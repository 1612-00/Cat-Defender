using System.Collections.Generic;
using NUnit.Framework;

namespace CatDefender.Tests
{
    public class CoreLogicTests
    {
        [Test]
        public void Merge_TwoSameLevel_GivesOneNextLevel()
        {
            var b = new MergeBoard(9, 15);
            b.Place(0, 3);
            b.Place(1, 3);
            Assert.AreEqual(MoveResult.Merged, b.Move(0, 1));
            Assert.AreEqual(0, b.LevelAt(0));
            Assert.AreEqual(4, b.LevelAt(1));
        }

        [Test]
        public void Move_DifferentLevels_Swaps()
        {
            var b = new MergeBoard(9, 15);
            b.Place(0, 2);
            b.Place(1, 5);
            Assert.AreEqual(MoveResult.Swapped, b.Move(0, 1));
            Assert.AreEqual(5, b.LevelAt(0));
            Assert.AreEqual(2, b.LevelAt(1));
        }

        [Test]
        public void Move_MaxLevelPair_SwapsInsteadOfMerging()
        {
            var b = new MergeBoard(9, 3);
            b.Place(0, 3);
            b.Place(1, 3);
            Assert.AreEqual(MoveResult.Swapped, b.Move(0, 1));
            Assert.AreEqual(3, b.LevelAt(0));
            Assert.AreEqual(3, b.LevelAt(1));
        }

        [Test]
        public void Move_ToEmpty_MovesAndEmptySourceDoesNothing()
        {
            var b = new MergeBoard(9, 15);
            b.Place(0, 1);
            Assert.AreEqual(MoveResult.Moved, b.Move(0, 4));
            Assert.AreEqual(1, b.LevelAt(4));
            Assert.AreEqual(MoveResult.None, b.Move(0, 4));
            Assert.AreEqual(MoveResult.None, b.Move(4, 4));
        }

        [Test]
        public void FirstEmpty_FullBoard_ReturnsMinusOne()
        {
            var b = new MergeBoard(2, 15);
            Assert.AreEqual(0, b.FirstEmpty());
            b.Place(0, 1);
            b.Place(1, 1);
            Assert.AreEqual(-1, b.FirstEmpty());
            Assert.IsFalse(b.Place(1, 1));
        }

        [Test]
        public void Wallet_CannotOverspend()
        {
            var w = new Wallet(10);
            Assert.IsFalse(w.TrySpend(11));
            Assert.IsTrue(w.TrySpend(10));
            Assert.AreEqual(0, w.Coins);
        }

        [Test]
        public void CatCost_GrowsWithPurchases()
        {
            Assert.AreEqual(10, Economy.CatCost(10, 1.15f, 0));
            Assert.AreEqual(12, Economy.CatCost(10, 1.2f, 1));
            Assert.Greater(Economy.CatCost(10, 1.15f, 10), Economy.CatCost(10, 1.15f, 9));
        }

        [Test]
        public void WaveRunner_RestsBeforeFirstWave_EvenWithEnemiesAlive()
        {
            var r = TwoWaves();
            var spawns = new List<int>();
            Assert.IsTrue(r.Resting);
            r.Tick(0.5f, 5, spawns);
            Assert.IsEmpty(spawns);
            r.Tick(0.6f, 5, spawns);
            Assert.AreEqual(1, spawns.Count);
            Assert.IsFalse(r.Resting);
        }

        [Test]
        public void Perks_StackAndScaleValues()
        {
            var p = new Perks();
            p.Apply(PerkType.Damage, 0.25f);
            p.Apply(PerkType.Damage, 0.25f);
            p.Apply(PerkType.FireRate, 0.25f);
            p.Apply(PerkType.Coins, 0.5f);
            Assert.AreEqual(150, p.Damage(100));
            Assert.AreEqual(0.8f, p.FireInterval(1f), 0.001f);
            Assert.AreEqual(15, p.Coins(10));
            Assert.AreEqual(1, p.Damage(0));
        }

        [Test]
        public void Perks_SlowIsCapped()
        {
            var p = new Perks();
            for (int i = 0; i < 10; i++) p.Apply(PerkType.Slow, 0.2f);
            Assert.AreEqual(0.7f, p.SlowFraction, 0.001f);
        }

        [Test]
        public void Picker_ReturnsDistinctSortedIndicesInRange()
        {
            var rng = new System.Random(1);
            for (int n = 0; n < 50; n++)
            {
                List<int> picks = Picker.Distinct(5, 3, rng);
                Assert.AreEqual(3, picks.Count);
                Assert.AreEqual(3, new HashSet<int>(picks).Count);
                Assert.That(picks, Is.Ordered);
                Assert.That(picks, Is.All.InRange(0, 4));
            }
            Assert.AreEqual(5, Picker.Distinct(5, 9, rng).Count);
        }

        [Test]
        public void ActiveLaneCount_GrowsWithWaveAndIsCapped()
        {
            Assert.AreEqual(2, Picker.ActiveLaneCount(1, 5));
            Assert.AreEqual(2, Picker.ActiveLaneCount(2, 5));
            Assert.AreEqual(3, Picker.ActiveLaneCount(3, 5));
            Assert.AreEqual(5, Picker.ActiveLaneCount(9, 5));
        }

        static WaveRunner TwoWaves() => new WaveRunner(new List<IReadOnlyList<SpawnGroup>>
        {
            new List<SpawnGroup> { new SpawnGroup { enemyId = 0, count = 3, interval = 1f } },
            new List<SpawnGroup>
            {
                new SpawnGroup { enemyId = 0, count = 2, interval = 1f },
                new SpawnGroup { enemyId = 1, count = 1, interval = 1f },
            },
        }, waveDelay: 2f, firstDelay: 1f);

        [Test]
        public void WaveRunner_SpawnsEveryEnemyInOrder_AndWaitsForClearBetweenWaves()
        {
            var r = TwoWaves();
            var spawns = new List<int>();

            // Wave 1: 3 quái, quái vẫn còn sống nên wave 2 chưa bắt đầu.
            for (int i = 0; i < 100; i++) r.Tick(0.1f, 1, spawns);
            Assert.AreEqual(new[] { 0, 0, 0 }, spawns.ToArray());
            Assert.AreEqual(2, r.CurrentWave);
            Assert.IsFalse(r.AllSpawned);

            // Hết quái: nghỉ 2 giây rồi sinh wave 2.
            for (int i = 0; i < 100; i++) r.Tick(0.1f, 0, spawns);
            Assert.AreEqual(new[] { 0, 0, 0, 0, 0, 1 }, spawns.ToArray());
            Assert.IsTrue(r.AllSpawned);
            Assert.AreEqual(2, r.WaveCount);
        }

        [Test]
        public void WaveRunner_LargeDeltaTime_SpawnsWholeWaveButNotTheNext()
        {
            var r = TwoWaves();
            var spawns = new List<int>();
            r.Tick(60f, 0, spawns); // hết giờ nghỉ đầu, sinh con thứ nhất
            r.Tick(60f, 0, spawns); // sinh nốt wave 1 rồi dừng ở giờ nghỉ
            r.Tick(0.1f, 3, spawns); // còn quái sống: chưa sang wave 2
            Assert.AreEqual(3, spawns.Count);
            Assert.IsTrue(r.Resting);
        }
    }
}
