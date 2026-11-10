using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace CatDefender.Tests
{
    /// <summary>Kiểm tra 30 level do GameBuilder sinh ra đều chơi được (không thiếu dữ liệu).</summary>
    public class LevelAssetsTests
    {
        static LevelData Load(int number) =>
            AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Game/Data/Levels/Level{number:00}.asset");

        [Test]
        public void AllThirtyLevels_HaveBackgroundAndValidWaves()
        {
            for (int n = 1; n <= 30; n++)
            {
                LevelData level = Load(n);
                Assert.IsNotNull(level, $"Level {n} missing");
                Assert.IsNotNull(level.background, $"Level {n} background");
                Assert.GreaterOrEqual(level.waves.Count, 5, $"Level {n} wave count");
                foreach (LevelData.Wave wave in level.waves)
                {
                    Assert.IsNotEmpty(wave.groups, $"Level {n} has an empty wave");
                    foreach (LevelData.Group g in wave.groups)
                    {
                        Assert.IsNotNull(g.enemy, $"Level {n} group enemy");
                        Assert.Greater(g.count, 0, $"Level {n} group count");
                        Assert.IsNotEmpty(g.enemy.walk, $"Level {n} enemy sprites");
                    }
                }
            }
        }

        [Test]
        public void ChapterEndLevels_EndWithBoss()
        {
            foreach (int n in new[] { 6, 12, 18, 24, 30 })
            {
                LevelData level = Load(n);
                Assert.IsTrue(level.waves.Last().groups.Any(g => g.enemy.isBoss), $"Level {n} has no boss in last wave");
            }
        }

        [Test]
        public void Level1_HasNoBossAndOnlyFirstEnemyType()
        {
            LevelData level = Load(1);
            var enemies = level.waves.SelectMany(w => w.groups).Select(g => g.enemy).Distinct().ToList();
            Assert.AreEqual(1, enemies.Count);
            Assert.IsFalse(enemies[0].isBoss);
        }
    }
}
