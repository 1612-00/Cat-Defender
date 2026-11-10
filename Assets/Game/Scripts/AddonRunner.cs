using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatDefender
{
    /// <summary>Thực thi hiệu ứng của 4 addon: chông, TNT, Cat Boxing, Cat Guardian.</summary>
    public sealed class AddonRunner : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] SpriteAnimator fxPrefab;
        [SerializeField] float roadYMin = -2.6f;
        [SerializeField] float roadYMax = 2.6f;

        readonly List<Enemy> hitBuffer = new List<Enemy>();

        public void Run(AddonData addon)
        {
            switch (addon.type)
            {
                case AddonType.Tnt: Tnt(addon); break;
                case AddonType.Spikes: StartCoroutine(Spikes(addon)); break;
                case AddonType.Boxing: StartCoroutine(Helper(addon, false)); break;
                case AddonType.Guardian: StartCoroutine(Helper(addon, true)); break;
            }
        }

        void Tnt(AddonData addon)
        {
            Enemy front = game.FrontEnemy();
            Vector3 center = front != null ? front.transform.position : new Vector3(game.WallX + 3f, 0, 0);
            game.SpawnExplosion(center, addon.range);
            DamageWhere(addon.damage, e => (e.transform.position - center).sqrMagnitude <= addon.range * addon.range);
        }

        IEnumerator Spikes(AddonData addon)
        {
            float x0 = game.WallX + 0.4f, x1 = x0 + addon.range;
            var root = new GameObject("Spikes").transform;
            for (float x = x0 + 0.5f; x < x1; x += 1f)
            for (float y = roadYMin + 0.5f; y < roadYMax; y += 1f)
            {
                SpriteAnimator tile = Instantiate(fxPrefab, new Vector3(x, y, 0), Quaternion.identity, root);
                tile.enabled = false;
                tile.Renderer.sprite = addon.icon;
                tile.Renderer.sortingOrder = -10;
                tile.transform.localScale = Vector3.one * 0.8f;
            }

            for (float t = 0; t < addon.duration && game.State == GameState.Playing; t += addon.tickInterval)
            {
                DamageWhere(addon.damage, e => e.transform.position.x >= x0 - 0.5f && e.transform.position.x <= x1);
                yield return new WaitForSeconds(addon.tickInterval);
            }
            Destroy(root.gameObject);
        }

        /// <summary>Cat Boxing đánh quái gần tường; Cat Guardian che tường khỏi sát thương.</summary>
        IEnumerator Helper(AddonData addon, bool shield)
        {
            SpriteAnimator helper = Instantiate(fxPrefab, new Vector3(game.WallX + 0.7f, 0, 0), Quaternion.identity);
            helper.Renderer.sortingOrder = 700;
            helper.Play(addon.frames, true);
            if (shield) game.WallShields++;

            float x1 = game.WallX + addon.range;
            for (float t = 0; t < addon.duration && game.State == GameState.Playing; t += addon.tickInterval)
            {
                if (!shield)
                {
                    Enemy front = game.FrontEnemy();
                    if (front != null && front.transform.position.x <= x1)
                        helper.transform.position = new Vector3(game.WallX + 0.7f, front.transform.position.y, 0);
                    DamageWhere(addon.damage, e => e.transform.position.x <= x1);
                }
                yield return new WaitForSeconds(addon.tickInterval);
            }

            if (shield) game.WallShields--;
            Destroy(helper.gameObject);
        }

        void DamageWhere(int damage, System.Predicate<Enemy> match)
        {
            // Sao chép trước: TakeDamage có thể xoá quái khỏi danh sách đang duyệt.
            hitBuffer.Clear();
            IReadOnlyList<Enemy> enemies = game.Enemies;
            for (int i = 0; i < enemies.Count; i++)
                if (match(enemies[i])) hitBuffer.Add(enemies[i]);
            for (int i = 0; i < hitBuffer.Count; i++) hitBuffer[i].TakeDamage(damage);
        }
    }
}
