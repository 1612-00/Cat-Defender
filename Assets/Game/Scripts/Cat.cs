using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteAnimator))]
    public sealed class Cat : MonoBehaviour
    {
        static readonly Vector3 Muzzle = new Vector3(0.35f, -0.1f, 0);
        static readonly Color StunColor = new Color(0.45f, 0.45f, 0.55f);

        CatData data;
        GameManager game;
        SpriteAnimator anim;
        float cooldown, stun;

        public CatData Data => data;
        /// <summary>Hàng mèo đang đứng; mèo chỉ bắn quái trên hàng này.</summary>
        public int Lane { get; set; }

        public void Init(CatData catData, GameManager manager, int lane)
        {
            data = catData;
            game = manager;
            Lane = lane;
            anim = GetComponent<SpriteAnimator>();
            anim.Play(data.idle, true);
            cooldown = data.fireInterval * 0.5f;
        }

        public void Stun(float seconds)
        {
            stun = Mathf.Max(stun, seconds);
            anim.Renderer.color = StunColor;
        }

        void Update()
        {
            if (game.State != GameState.Playing) return;
            if (stun > 0)
            {
                stun -= Time.deltaTime;
                if (stun <= 0) anim.Renderer.color = Color.white;
                return;
            }

            cooldown -= Time.deltaTime;
            if (cooldown > 0) return;

            Enemy target = game.FindTarget(Lane);
            if (target == null) return;

            cooldown = game.Perks.FireInterval(data.fireInterval);
            anim.Play(data.shoot, false, () => anim.Play(data.idle, true));
            game.SpawnBullet(transform.position + Muzzle, target, data);
        }
    }
}
