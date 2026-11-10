using System;

namespace CatDefender
{
    /// <summary>Toàn bộ tiến trình của người chơi, lưu thành một chuỗi JSON.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int coins;
        public int gems;
        /// <summary>Chỉ số level cao nhất đã mở (tính từ 0). Mọi level có chỉ số nhỏ hơn đã được thắng; bằng số level nghĩa là đã thắng hết.</summary>
        public int unlockedLevel;
        public int[] upgradeLevels = new int[0];
        public int[] addonLevels = new int[0];
        public bool music = true;
        public bool sound = true;
        public bool vibration = true;
        public string language = "en";

        /// <summary>Sửa dữ liệu lưu cũ hoặc hỏng về trạng thái hợp lệ: đúng độ dài mảng, không có số âm.</summary>
        public void Normalize(int upgradeCount, int addonCount, int levelCount, int maxUpgradeLevel)
        {
            coins = Math.Max(0, coins);
            gems = Math.Max(0, gems);
            unlockedLevel = Math.Min(Math.Max(0, unlockedLevel), levelCount);
            upgradeLevels = Fit(upgradeLevels, upgradeCount, maxUpgradeLevel);
            addonLevels = Fit(addonLevels, addonCount, maxUpgradeLevel);
            if (language != "vi") language = "en";
        }

        static int[] Fit(int[] source, int length, int max)
        {
            var result = new int[length];
            if (source == null) return result;
            for (int i = 0; i < length && i < source.Length; i++)
                result[i] = Math.Min(Math.Max(0, source[i]), max);
            return result;
        }
    }

    /// <summary>Công thức của phần ngoài màn chơi: giá nâng cấp và phần thưởng. Logic thuần.</summary>
    public static class MetaRules
    {
        /// <summary>Giá nâng từ cấp <paramref name="level"/> lên cấp kế: baseCost * growth^level, làm tròn tới bội số 5.</summary>
        public static int Cost(int baseCost, float growth, int level) =>
            (int)(Math.Round(baseCost * Math.Pow(growth, level) / 5.0) * 5);

        public static int WinCoins(int baseCoins, int perLevel, int levelNumber) => baseCoins + perLevel * levelNumber;

        /// <summary>Thua vẫn nhận một phần thưởng thắng, tỉ lệ với số wave đã qua.</summary>
        public static int LoseCoins(int winCoins, float fraction, int wavesCleared, int waveCount) =>
            waveCount <= 0 ? 0 : (int)Math.Round(winCoins * fraction * wavesCleared / waveCount);
    }
}
