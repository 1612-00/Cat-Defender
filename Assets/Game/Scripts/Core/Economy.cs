using System;

namespace CatDefender
{
    public static class Economy
    {
        /// <summary>Giá mèo cấp 1 thứ n (n = số mèo đã mua trong level): baseCost * growth^n, làm tròn.</summary>
        public static int CatCost(int baseCost, float growth, int purchased) =>
            (int)Math.Round(baseCost * Math.Pow(growth, purchased));
    }
}
