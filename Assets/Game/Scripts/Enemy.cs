using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteAnimator))]
    public sealed class Enemy : MonoBehaviour
    {
        /// <summary>Điểm đạn nhắm tới, tính từ tâm sprite (thân quái nằm thấp hơn tâm ảnh).</summary>
        public static readonly Vector3 AimOffset = new Vector3(0, -0.4f, 0);
        /// <summary>Tâm sprite cao hơn chân quái chừng này, nên quái đứng lệch lên so với tâm hàng.</summary>
        public const float FootOffset = 0.45f;
        const float FlashTime = 0.12f, LaneSwitchTime = 0.8f;
        static readonly Color HitColor = new Color(1f, 0.45f, 0.45f);
        static readonly Color SlowColor = new Color(0.55f, 0.75f, 1f);

        [SerializeField] GameObject hpBar;
        [SerializeField] Transform hpFill;

        EnemyData data;
        GameManager game;
        SpriteAnimator anim;
        int hp, maxHp, reward, armor;
        bool attacking;
        float attackTimer, flash, slowTimer, slowFraction;
        float switchAtX = float.MinValue, switchFromY, switchT = 1f;
        float summonTimer, stunTimer;

        /// <summary>Quái còn máu và chưa bị hạ.</summary>
        public bool Alive => hp > 0;
        /// <summary>Quái có giáp, dùng để chọn hiệu ứng âm thanh trúng đòn.</summary>
        public bool IsArmored => armor > 0;
        /// <summary>Cho biết đây là boss.</summary>
        public bool IsBoss => data != null && data.isBoss;
        public int Lane { get; private set; }

        /// <summary>Tổng sát thương (đã trừ giáp) của các viên đạn đang bay tới quái này.</summary>
        public int PendingDamage { get; set; }

        /// <summary>Đạn đang bay đã đủ để hạ quái; mèo khác nên chọn mục tiêu khác.</summary>
        public bool Doomed => PendingDamage >= hp;

        public void Init(EnemyData enemyData, GameManager manager, int lane, float hpScale, float rewardScale)
        {
            data = enemyData;
            game = manager;
            Lane = lane;
            hp = maxHp = Mathf.Max(1, Mathf.RoundToInt(data.hp * hpScale));
            reward = Mathf.Max(1, Mathf.RoundToInt(data.coinReward * rewardScale));
            armor = Mathf.RoundToInt(data.armor * hpScale);
            transform.localScale = Vector3.one * data.scale;
            anim = GetComponent<SpriteAnimator>();
            anim.Renderer.color = data.tint;
            anim.Play(data.walk, true);
            hpBar.SetActive(data.isBoss);
            if (data.switchesLane) switchAtX = Random.Range(2f, 7f);
            summonTimer = data.summonInterval;
            stunTimer = data.stunInterval;
            Sort();
        }

        /// <summary>Sát thương thực nhận sau khi trừ giáp.</summary>
        public int Mitigate(int damage) => Mathf.Max(1, damage - armor);

        /// <summary>
        /// Thứ tự vẽ theo y: quái đứng thấp hơn trên màn hình được vẽ đè lên quái đứng cao hơn.
        /// Luôn dương với mọi y trong sân, để không bao giờ chìm dưới ảnh nền (thứ tự -100).
        /// </summary>
        public static int SortOrder(float y) => 450 - Mathf.RoundToInt(y * 100);

        void Sort() => anim.Renderer.sortingOrder = SortOrder(transform.position.y);

        void Update()
        {
            if (flash > 0 || slowTimer > 0)
            {
                flash -= Time.deltaTime;
                Color baseColor = slowTimer > 0 ? data.tint * SlowColor : data.tint;
                anim.Renderer.color = flash > 0 ? Color.Lerp(baseColor, HitColor, flash / FlashTime) : baseColor;
            }
            if (!Alive || game.State != GameState.Playing) return;

            float dt = Time.deltaTime;
            float speed = data.speed;
            if (slowTimer > 0)
            {
                slowTimer -= dt;
                speed *= 1f - slowFraction;
                if (slowTimer <= 0) anim.Renderer.color = data.tint;
            }

            BossAbilities(dt);

            if (switchT < 1f)
            {
                switchT = Mathf.Min(1f, switchT + dt / LaneSwitchTime);
                Vector3 p = transform.position;
                transform.position = new Vector3(p.x, Mathf.Lerp(switchFromY, game.LaneY(Lane) + FootOffset, switchT), 0);
                Sort();
            }

            if (!attacking)
            {
                transform.position += Vector3.left * (speed * dt);
                float x = transform.position.x;
                if (x <= switchAtX && switchT >= 1f)
                {
                    switchAtX = float.MinValue;
                    switchFromY = transform.position.y;
                    Lane = game.AdjacentLane(Lane);
                    switchT = 0;
                }
                if (x > game.WallX) return;
                attacking = true;
                attackTimer = data.attackInterval;
                anim.Play(data.attack, true);
                return;
            }

            attackTimer -= dt;
            if (attackTimer > 0) return;
            attackTimer += data.attackInterval;
            game.DamageWall(data.damage, Lane);
        }

        void BossAbilities(float dt)
        {
            if (data.summon != null && data.summonInterval > 0)
            {
                summonTimer -= dt;
                if (summonTimer <= 0)
                {
                    summonTimer = data.summonInterval;
                    for (int i = 0; i < data.summonCount; i++)
                        game.SpawnEnemyAt(data.summon, transform.position.x + 0.8f, game.RandomLane());
                }
            }
            if (data.stunInterval > 0)
            {
                stunTimer -= dt;
                if (stunTimer <= 0)
                {
                    stunTimer = data.stunInterval;
                    if (game.StunLane(Lane, data.stunDuration)) AudioManager.Play(AudioCue.BossStun);
                }
            }
        }

        public void ApplySlow(float fraction, float seconds)
        {
            if (data.isBoss) fraction *= 0.5f;
            slowFraction = fraction;
            slowTimer = seconds;
        }

        /// <summary>Gây sát thương; giáp được trừ ở đây.</summary>
        public void TakeDamage(int amount)
        {
            if (!Alive) return;
            hp -= Mitigate(amount);
            flash = FlashTime;
            if (hp > 0)
            {
                if (data.isBoss) hpFill.localScale = new Vector3((float)hp / maxHp, 1, 1);
                return;
            }
            hp = 0;
            hpBar.SetActive(false);
            game.OnEnemyKilled(this, reward);
            if (data.splitInto != null)
            {
                AudioManager.Play(AudioCue.EnemySplit);
                for (int i = 0; i < data.splitCount; i++)
                    game.SpawnEnemyAt(data.splitInto, transform.position.x + 0.5f * i, i == 0 ? Lane : game.AdjacentLane(Lane));
            }
            anim.Play(data.dead, false, () => Destroy(gameObject));
        }
    }
}
