using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Bullet : MonoBehaviour
    {
        GameManager game;
        Enemy target;
        int damage, pending;
        float speed;

        public void Init(GameManager manager, Enemy enemy, int dmg, float bulletSpeed, Sprite sprite)
        {
            game = manager;
            target = enemy;
            damage = dmg;
            pending = enemy.Mitigate(dmg);
            target.PendingDamage += pending;
            speed = bulletSpeed;
            GetComponent<SpriteRenderer>().sprite = sprite;
        }

        void Update()
        {
            if (target == null || !target.Alive)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 goal = target.transform.position + Enemy.AimOffset;
            Vector3 to = goal - transform.position;
            float step = speed * Time.deltaTime;
            if (to.sqrMagnitude <= step * step)
            {
                target.PendingDamage -= pending;
                game.OnBulletHit(target, damage);
                Destroy(gameObject);
                return;
            }
            transform.position += to.normalized * step;
            transform.right = to;
        }
    }
}
