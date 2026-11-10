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
        public Sprite[] walk;
        public Sprite[] attack;
        public Sprite[] dead;
    }
}
