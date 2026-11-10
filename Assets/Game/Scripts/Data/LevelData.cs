using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatDefender
{
    [CreateAssetMenu(menuName = "CatDefender/Level")]
    public sealed class LevelData : ScriptableObject
    {
        [Serializable]
        public struct Group
        {
            public EnemyData enemy;
            public int count;
            public float interval;
        }

        [Serializable]
        public class Wave
        {
            public List<Group> groups = new List<Group>();
        }

        public Sprite background;
        /// <summary>Hệ số nhân máu quái và coin thưởng của riêng level này.</summary>
        public float enemyHpScale = 1f;
        public float rewardScale = 1f;
        public int startCoins = 30;
        public int wallHp = 100;
        public int catBaseCost = 10;
        public float catCostGrowth = 1.15f;
        public int repairCost = 25;
        public int repairAmount = 25;
        public float firstWaveDelay = 3f;
        public float waveDelay = 4f;
        public List<Wave> waves = new List<Wave>();
    }
}
