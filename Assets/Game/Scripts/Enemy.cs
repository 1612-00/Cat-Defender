using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteAnimator))]
    public sealed class Enemy : MonoBehaviour
    {
        /// <summary>Điểm đạn nhắm tới, tính từ tâm sprite (thân quái nằm thấp hơn tâm ảnh).</summary>
        public static readonly Vector3 AimOffset = new Vector3(0, -0.4f, 0);

        EnemyData data;
        GameManager game;
        SpriteAnimator anim;
        int hp;
        bool attacking;
        float attackTimer;

        public bool Alive => hp > 0;

        /// <summary>Tổng sát thương của các viên đạn đang bay tới quái này.</summary>
        public int PendingDamage { get; set; }

        /// <summary>Đạn đang bay đã đủ để hạ quái; mèo khác nên chọn mục tiêu khác.</summary>
        public bool Doomed => PendingDamage >= hp;

        public void Init(EnemyData enemyData, GameManager manager)
        {
            data = enemyData;
            game = manager;
            hp = data.hp;
            anim = GetComponent<SpriteAnimator>();
            anim.Renderer.sortingOrder = 100 + (int)(-transform.position.y * 100);
            anim.Play(data.walk, true);
        }

        void Update()
        {
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
            if (hp > 0) return;
            hp = 0;
            game.OnEnemyKilled(this, data.coinReward);
            anim.Play(data.dead, false, () => Destroy(gameObject));
        }
    }
}
