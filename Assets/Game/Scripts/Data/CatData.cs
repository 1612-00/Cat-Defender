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
        public Sprite bulletSprite;
        public Sprite[] idle;
        public Sprite[] shoot;
    }
}
