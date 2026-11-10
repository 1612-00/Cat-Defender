using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Bullet : MonoBehaviour
    {
        Enemy target;
        int damage;
        float speed;

        public void Init(Enemy enemy, int dmg, float bulletSpeed, Sprite sprite)
        {
            target = enemy;
            target.PendingDamage += dmg;
            damage = dmg;
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
                target.PendingDamage -= damage;
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
            transform.position += to.normalized * step;
            transform.right = to;
        }
    }
}
