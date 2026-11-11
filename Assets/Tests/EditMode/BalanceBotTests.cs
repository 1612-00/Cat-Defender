using NUnit.Framework;

namespace CatDefender.Tests
{
    public class BalanceBotTests
    {
        [Test]
        public void FindMergePair_TwoSameLevelCats_ReturnsTheirSlots()
        {
            bool found = BalanceBot.FindMergePair(new[] { 1, 2, 0, 2 }, 5, out int a, out int b);

            Assert.IsTrue(found);
            Assert.AreEqual((1, 3), (a, b));
        }

        [Test]
        public void FindMergePair_EmptySlots_AreIgnored()
        {
            bool found = BalanceBot.FindMergePair(new[] { 0, 0, 1 }, 5, out _, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void FindMergePair_MaxLevelPair_IsIgnored()
        {
            bool found = BalanceBot.FindMergePair(new[] { 5, 5 }, 5, out _, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void FindMergePair_NoMatchingLevels_ReturnsNone()
        {
            bool found = BalanceBot.FindMergePair(new[] { 1, 2, 3 }, 5, out int a, out int b);

            Assert.IsFalse(found);
            Assert.AreEqual((-1, -1), (a, b));
        }

        static readonly int[] Lanes10 = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 };

        [Test]
        public void FindRearrangeMove_NoActiveLanes_ReturnsFalse()
        {
            bool found = BalanceBot.FindRearrangeMove(new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, Lanes10, new int[0], out _, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void FindRearrangeMove_CatOnInactiveLane_MovesToEmptySlotOnActiveLane()
        {
            var lv = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 0 };

            bool found = BalanceBot.FindRearrangeMove(lv, Lanes10, new[] { 1 }, out int from, out int to);

            Assert.IsTrue(found);
            Assert.AreEqual(8, from);
            Assert.AreEqual(1, Lanes10[to]);
        }

        [Test]
        public void FindRearrangeMove_PrefersStrongestCat()
        {
            var lv = new[] { 1, 0, 0, 0, 0, 0, 5, 0, 3, 0 };

            bool found = BalanceBot.FindRearrangeMove(lv, Lanes10, new[] { 2 }, out int from, out _);

            Assert.IsTrue(found);
            Assert.AreEqual(6, from);
        }

        [Test]
        public void FindRearrangeMove_StrongestAlreadyEvenlyOnActiveLanes_ReturnsFalse()
        {
            var lv = new[] { 0, 0, 5, 0, 4, 0, 0, 0, 0, 0 };

            bool found = BalanceBot.FindRearrangeMove(lv, Lanes10, new[] { 1, 2 }, out _, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void FindRearrangeMove_RepeatedlyApplied_Converges()
        {
            var boards = new[]
            {
                new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                new[] { 1, 1, 0, 2, 0, 3, 0, 4, 0, 5 },
                new[] { 9, 8, 7, 0, 0, 0, 0, 0, 0, 1 },
                new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            };
            var activeSets = new[] { new[] { 0, 3 }, new[] { 1, 2, 4 }, new[] { 4 } };
            foreach (int[] start in boards)
                foreach (int[] active in activeSets)
                {
                    var lv = (int[])start.Clone();
                    int iterations = 0;
                    while (BalanceBot.FindRearrangeMove(lv, Lanes10, active, out int from, out int to))
                    {
                        Assert.AreNotEqual(from, to);
                        Assert.AreNotEqual(lv[from], lv[to], "move would merge or is empty-from");
                        (lv[from], lv[to]) = (lv[to], lv[from]);
                        Assert.Less(++iterations, 30);
                    }
                }
        }
    }
}
