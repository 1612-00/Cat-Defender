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
            r.Tick(60f, 0, spawns);
            Assert.AreEqual(3, spawns.Count);
        }
    }
}
