using System;

namespace CatDefender
{
    public sealed class Wallet
    {
        public int Coins { get; private set; }
        public event Action Changed;

        public Wallet(int startCoins) => Coins = startCoins;

        public void Add(int amount)
        {
            Coins += amount;
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (amount > Coins) return false;
            Coins -= amount;
            Changed?.Invoke();
            return true;
        }
    }
}
