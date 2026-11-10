using UnityEngine;

namespace CatDefender
{
    [CreateAssetMenu(menuName = "CatDefender/Enemy")]
    public sealed class EnemyData : ScriptableObject
    {
        public int hp = 20;
        public float speed = 0.8f;
        public int damage = 5;
        public float attackInterval = 1f;
        public int coinReward = 4;
        public bool isBoss;
        public float scale = 1f;

        [Header("Hành vi riêng")]
        /// <summary>Giáp: mỗi phát đạn bị trừ ngần này sát thương (tối thiểu còn 1). Nhân theo hệ số máu của level.</summary>
        public int armor;
        /// <summary>Giữa đường nhảy sang hàng bên cạnh một lần.</summary>
        public bool switchesLane;
        /// <summary>Khi chết tách thành các con này.</summary>
        public EnemyData splitInto;
        public int splitCount;
        /// <summary>Boss: gọi đàn em theo chu kỳ.</summary>
        public EnemyData summon;
        public int summonCount;
        public float summonInterval;
        /// <summary>Boss: làm choáng mèo trên hàng của nó theo chu kỳ.</summary>
        public float stunInterval;
        public float stunDuration;
        public Color tint = Color.white;
        public Sprite[] walk;
        public Sprite[] attack;
        public Sprite[] dead;
    }
}
