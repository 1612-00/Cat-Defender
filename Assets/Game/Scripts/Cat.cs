using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteAnimator))]
    public sealed class Cat : MonoBehaviour
    {
        static readonly Vector3 Muzzle = new Vector3(0.35f, -0.1f, 0);

        CatData data;
        GameManager game;
        SpriteAnimator anim;
        float cooldown;

        public CatData Data => data;

        public void Init(CatData catData, GameManager manager)
        {
            data = catData;
            game = manager;
            anim = GetComponent<SpriteAnimator>();
            anim.Play(data.idle, true);
            cooldown = data.fireInterval * 0.5f;
        }

        void Update()
        {
            if (game.State != GameState.Playing) return;
            cooldown -= Time.deltaTime;
            if (cooldown > 0) return;

            Enemy target = game.FindTarget();
            if (target == null) return;

            cooldown = data.fireInterval;
            anim.Play(data.shoot, false, () => anim.Play(data.idle, true));
            game.SpawnBullet(transform.position + Muzzle, target, data);
        }
    }
}
