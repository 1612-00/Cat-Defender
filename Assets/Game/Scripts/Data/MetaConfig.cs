using System;
using UnityEngine;

namespace CatDefender
{
    public enum UpgradeId { CatDamage, WallHp, StartCoins }

    /// <summary>Chỉ số của phần ngoài màn chơi: nâng cấp vĩnh viễn, phần thưởng, gói trong shop.</summary>
    [CreateAssetMenu(menuName = "CatDefender/Meta Config")]
    public sealed class MetaConfig : ScriptableObject
    {
        [Serializable]
        public struct UpgradeDef
        {
            public UpgradeId id;
            public Sprite icon;
            public string nameKey;
            public string descKey;
            public int baseCost;
            public float costGrowth;
            /// <summary>Phần cộng thêm mỗi cấp: tỉ lệ với CatDamage, số tuyệt đối với WallHp và StartCoins.</summary>
            public float valuePerLevel;
        }

        [Serializable]
        public struct CoinPack
        {
            public Sprite icon;
            public int gems;
            public int coins;
        }

        public const int LevelCount = 30;

        /// <summary>Cấp tối đa của mỗi nâng cấp; khớp với ảnh 0Bar..5Bar.</summary>
        public int maxLevel = 5;
        public UpgradeDef[] upgrades;
        public string[] addonNameKeys;
        public int addonBaseCost = 80;
        public float addonCostGrowth = 1.8f;
        /// <summary>Mỗi cấp addon tăng sát thương và thời gian thêm ngần này (0,2 = 20%).</summary>
        public float addonBonusPerLevel = 0.2f;
        public CoinPack[] packs;
        public int winBaseCoins = 40;
        public int winCoinsPerLevel = 15;
        public float loseFraction = 0.3f;
        public int firstClearGems = 5;
        public int bossFirstClearGems = 15;
        public int startGems = 10;

        public float UpgradeValue(UpgradeId id, SaveData save)
        {
            for (int i = 0; i < upgrades.Length; i++)
                if (upgrades[i].id == id) return upgrades[i].valuePerLevel * save.upgradeLevels[i];
            return 0;
        }

        public int UpgradeCost(int index, int level) =>
            MetaRules.Cost(upgrades[index].baseCost, upgrades[index].costGrowth, level);

        public int AddonCost(int level) => MetaRules.Cost(addonBaseCost, addonCostGrowth, level);
        public float AddonScale(int level) => 1f + addonBonusPerLevel * level;
    }
}
