using UnityEngine;

namespace CatDefender
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MergeStrikeVisual : MonoBehaviour
    {
        SpriteRenderer beam;
        Vector3 start;
        float duration;
        float elapsed;
        float length;
        float thickness;

        public void Play(Sprite sprite, Vector3 start, float length, float beamThickness, float seconds, int sortingOrder)
        {
            beam = GetComponent<SpriteRenderer>();
            beam.sprite = sprite;
            beam.color = new Color(0.25f, 0.9f, 1f, 0.9f);
            beam.sortingOrder = sortingOrder;
            this.start = start;
            this.length = Mathf.Max(0.1f, length);
            duration = Mathf.Max(0.01f, seconds);
            thickness = Mathf.Max(0.01f, beamThickness);
            transform.position = start + Vector3.right * (this.length * 0.5f);
            transform.localScale = new Vector3(this.length, thickness, 1f);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 3f) * 0.45f;
            transform.position = start + Vector3.right * (length * 0.5f);
            transform.localScale = new Vector3(length, thickness * pulse, 1f);

            Color color = beam.color;
            color.a = 0.9f * (1f - progress);
            beam.color = color;

            if (progress >= 1f) Destroy(gameObject);
        }
    }
}
