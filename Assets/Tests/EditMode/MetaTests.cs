using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CatDefender.Tests
{
    public class MetaTests
    {
        [Test]
        public void Cost_GrowsPerLevel_AndRoundsToFive()
        {
            Assert.AreEqual(100, MetaRules.Cost(100, 1.8f, 0));
            Assert.AreEqual(180, MetaRules.Cost(100, 1.8f, 1));
            Assert.AreEqual(325, MetaRules.Cost(100, 1.8f, 2)); // 324 làm tròn tới bội số 5
            Assert.Greater(MetaRules.Cost(100, 1.8f, 4), MetaRules.Cost(100, 1.8f, 3));
        }

        [Test]
        public void Rewards_WinGrowsWithLevel_LoseIsPartialByWavesCleared()
        {
            int win = MetaRules.WinCoins(40, 15, 10);
            Assert.AreEqual(190, win);
            Assert.AreEqual(0, MetaRules.LoseCoins(win, 0.3f, 0, 5));
            Assert.AreEqual(23, MetaRules.LoseCoins(win, 0.3f, 2, 5)); // 190 * 0,3 * 2/5 = 22,8
            Assert.AreEqual(0, MetaRules.LoseCoins(win, 0.3f, 3, 0));
        }

        [Test]
        public void SaveData_JsonRoundTrip_KeepsProgress()
        {
            var save = new SaveData { coins = 123, gems = 7, unlockedLevel = 4, language = "vi", music = false };
            save.upgradeLevels = new[] { 1, 2, 3 };
            save.addonLevels = new[] { 0, 5, 1, 2 };

            SaveData loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            loaded.Normalize(3, 4, 30, 5);

            Assert.AreEqual(123, loaded.coins);
            Assert.AreEqual(7, loaded.gems);
            Assert.AreEqual(4, loaded.unlockedLevel);
            Assert.AreEqual("vi", loaded.language);
            Assert.IsFalse(loaded.music);
            Assert.AreEqual(new[] { 1, 2, 3 }, loaded.upgradeLevels);
            Assert.AreEqual(new[] { 0, 5, 1, 2 }, loaded.addonLevels);
        }

        [Test]
        public void Normalize_RepairsOutOfRangeAndShortArrays()
        {
            var save = new SaveData { coins = -5, gems = -1, unlockedLevel = 99, language = "xx" };
            save.upgradeLevels = new[] { 9 };
            save.addonLevels = null;

            save.Normalize(3, 4, 30, 5);

            Assert.AreEqual(0, save.coins);
            Assert.AreEqual(0, save.gems);
            Assert.AreEqual(30, save.unlockedLevel);
            Assert.AreEqual("en", save.language);
            Assert.AreEqual(new[] { 5, 0, 0 }, save.upgradeLevels);
            Assert.AreEqual(new[] { 0, 0, 0, 0 }, save.addonLevels);
        }

        [Test]
        public void Loc_EveryKeyHasBothLanguages_WithMatchingPlaceholders()
        {
            foreach (string key in Loc.Keys)
            {
                string en = Loc.Raw(key, false), vi = Loc.Raw(key, true);
                Assert.IsNotEmpty(en, key);
                Assert.IsNotEmpty(vi, key);
                Assert.AreEqual(en.Count(c => c == '{'), vi.Count(c => c == '{'), "placeholders differ: " + key);
            }
        }

        [Test]
        public void PerkAssets_UseKeysThatExistInLoc()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PerkData", new[] { "Assets/Game/Data/Perks" });
            Assert.AreEqual(8, guids.Length);
            foreach (string guid in guids)
            {
                var perk = UnityEditor.AssetDatabase.LoadAssetAtPath<PerkData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                Assert.Contains(perk.titleKey, Loc.Keys.ToList(), perk.name);
                Assert.Contains(perk.descKey, Loc.Keys.ToList(), perk.name);
            }
        }
    }
}
