using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteAnimator))]
    public sealed class Enemy : MonoBehaviour
    {
        /// <summary>Điểm đạn nhắm tới, tính từ tâm sprite (thân quái nằm thấp hơn tâm ảnh).</summary>
        public static readonly Vector3 AimOffset = new Vector3(0, -0.4f, 0);
        const float FlashTime = 0.12f;

        [SerializeField] GameObject hpBar;
        [SerializeField] Transform hpFill;

        EnemyData data;
        GameManager game;
        SpriteAnimator anim;
        int hp, maxHp, reward;
        bool attacking;
        float attackTimer, flash;

        public bool Alive => hp > 0;

        /// <summary>Tổng sát thương của các viên đạn đang bay tới quái này.</summary>
        public int PendingDamage { get; set; }

        /// <summary>Đạn đang bay đã đủ để hạ quái; mèo khác nên chọn mục tiêu khác.</summary>
        public bool Doomed => PendingDamage >= hp;

        public void Init(EnemyData enemyData, GameManager manager, float hpScale, float rewardScale)
        {
            data = enemyData;
            game = manager;
            hp = maxHp = Mathf.Max(1, Mathf.RoundToInt(data.hp * hpScale));
            reward = Mathf.Max(1, Mathf.RoundToInt(data.coinReward * rewardScale));
            transform.localScale = Vector3.one * data.scale;
            anim = GetComponent<SpriteAnimator>();
            anim.Renderer.sortingOrder = 100 + (int)(-transform.position.y * 100);
            anim.Play(data.walk, true);
            hpBar.SetActive(data.isBoss);
        }

        void Update()
        {
            if (flash > 0)
            {
                flash -= Time.deltaTime;
                anim.Renderer.color = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), flash / FlashTime);
            }
            if (!Alive || game.State != GameState.Playing) return;

            if (!attacking)
            {
                transform.position += Vector3.left * (data.speed * Time.deltaTime);
                if (transform.position.x > game.WallX) return;
                attacking = true;
                attackTimer = data.attackInterval;
                anim.Play(data.attack, true);
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0) return;
            attackTimer += data.attackInterval;
            game.DamageWall(data.damage);
        }

        public void TakeDamage(int amount)
        {
            if (!Alive) return;
            hp -= amount;
            flash = FlashTime;
            if (hp > 0)
            {
                if (data.isBoss) hpFill.localScale = new Vector3((float)hp / maxHp, 1, 1);
                return;
            }
            hp = 0;
            hpBar.SetActive(false);
            game.OnEnemyKilled(this, reward);
            anim.Play(data.dead, false, () => Destroy(gameObject));
        }
    }
}
