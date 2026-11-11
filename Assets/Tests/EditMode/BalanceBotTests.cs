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
    }
}
