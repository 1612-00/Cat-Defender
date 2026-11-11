using UnityEngine;

namespace CatDefender
{
    [CreateAssetMenu(menuName = "CatDefender/Cat")]
    public sealed class CatData : ScriptableObject
    {
        public int level = 1;
        public int damage = 4;
        public float fireInterval = 0.8f;
        public float bulletSpeed = 22f;
        /// <summary>Phóng mèo: sát thương nổ = damage × hệ số này.</summary>
        public float launchMultiplier = 12f;
        /// <summary>Phóng mèo: bán kính nổ.</summary>
        public float launchRadius = 1.8f;
        public Sprite bulletSprite;
        public Sprite[] idle;
        public Sprite[] shoot;
    }
}
