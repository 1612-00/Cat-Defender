using System;
using System.Collections.Generic;

namespace CatDefender
{
    public enum PerkType { FireRate, Damage, Splash, Coins, Slow, Wall, AddonRefill, FreeCat }

    /// <summary>
    /// Hiệu ứng cộng dồn của các thẻ nâng cấp đã chọn trong một level. Logic thuần.
    /// Thẻ tức thời (Wall, AddonRefill, FreeCat) do GameManager xử lý, không lưu ở đây.
    /// </summary>
    public sealed class Perks
    {
        /// <summary>Hệ số tốc độ bắn; 1,3 nghĩa là bắn nhanh hơn 30%.</summary>
        public float FireRateMul { get; private set; } = 1f;
        public float DamageMul { get; private set; } = 1f;
        public float CoinMul { get; private set; } = 1f;
        /// <summary>Phần sát thương lan sang quái đứng gần mục tiêu (0 = không lan).</summary>
        public float SplashFraction { get; private set; }
        /// <summary>Phần tốc độ quái bị giảm khi trúng đạn (0 = không làm chậm). Tối đa 0,7.</summary>
        public float SlowFraction { get; private set; }

        public void Apply(PerkType type, float value)
        {
            switch (type)
            {
                case PerkType.FireRate: FireRateMul += value; break;
                case PerkType.Damage: DamageMul += value; break;
                case PerkType.Coins: CoinMul += value; break;
                case PerkType.Splash: SplashFraction += value; break;
                case PerkType.Slow: SlowFraction = Math.Min(0.7f, SlowFraction + value); break;
            }
        }

        public int Damage(int baseDamage) => Math.Max(1, (int)Math.Round(baseDamage * DamageMul));
        public int Coins(int baseReward) => Math.Max(1, (int)Math.Round(baseReward * CoinMul));
        public float FireInterval(float baseInterval) => baseInterval / FireRateMul;
    }

    public static class Picker
    {
        /// <summary>Chọn ngẫu nhiên <paramref name="count"/> chỉ số khác nhau trong [0, total), tăng dần.</summary>
        public static List<int> Distinct(int total, int count, Random rng)
        {
            var pool = new List<int>(total);
            for (int i = 0; i < total; i++) pool.Add(i);
            var result = new List<int>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int k = rng.Next(pool.Count);
                result.Add(pool[k]);
                pool.RemoveAt(k);
            }
            result.Sort();
            return result;
        }

        /// <summary>
        /// Chọn hàng cho một con quái: hàng chính với xác suất <paramref name="share"/>, còn lại chia đều cho các hàng khác.
        /// Chỉ có một hàng thì luôn hàng đó.
        /// </summary>
        public static int WeightedLane(IReadOnlyList<int> activeLanes, int mainLane, float share, Random rng)
        {
            var others = new List<int>(activeLanes.Count);
            foreach (int l in activeLanes) if (l != mainLane) others.Add(l);
            if (others.Count == 0) return activeLanes[0];
            bool mainActive = others.Count < activeLanes.Count;
            if (mainActive && rng.NextDouble() < share) return mainLane;
            return others[rng.Next(others.Count)];
        }

        /// <summary>Số hàng có quái của wave thứ <paramref name="wave"/> (tính từ 1): 2 hàng lúc đầu, thêm 1 sau mỗi 2 wave.</summary>
        public static int ActiveLaneCount(int wave, int laneCount) => Math.Min(laneCount, 2 + (wave - 1) / 2);
    }
}
