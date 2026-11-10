using System;
using UnityEngine;

namespace CatDefender
{
    /// <summary>Tiến trình đang dùng của người chơi: tải lúc cần, lưu sau mỗi thay đổi.</summary>
    public static class Profile
    {
        // ponytail: lưu một chuỗi JSON trong PlayerPrefs. Đủ cho dữ liệu vài trăm byte;
        // chuyển sang file + bản sao lưu nếu dữ liệu lớn lên hoặc cần đồng bộ đám mây.
        const string Key = "save_v1";

        static SaveData data;
        static MetaConfig config;

        public static event Action Changed;

        public static SaveData Data => data ?? (data = Load());

        /// <summary>Phải gọi một lần trước khi dùng <see cref="Data"/>; cần cấu hình để biết độ dài các mảng.</summary>
        public static void Init(MetaConfig metaConfig)
        {
            if (config == metaConfig && data != null) return;
            config = metaConfig;
            data = Load();
        }

        static SaveData Load()
        {
            SaveData loaded = null;
            string json = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    loaded = JsonUtility.FromJson<SaveData>(json);
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning("Dữ liệu lưu hỏng, bắt đầu lại từ đầu: " + e.Message);
                }
            }

            if (loaded == null)
            {
                loaded = new SaveData
                {
                    gems = config != null ? config.startGems : 0,
                    language = Application.systemLanguage == SystemLanguage.Vietnamese ? "vi" : "en",
                };
            }

            int upgrades = config != null ? config.upgrades.Length : 0;
            int addons = config != null ? config.addonNameKeys.Length : 0;
            int max = config != null ? config.maxLevel : 0;
            loaded.Normalize(upgrades, addons, MetaConfig.LevelCount, max);
            return loaded;
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(Key);
            data = Load();
            Changed?.Invoke();
        }

        public static bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Data.coins < amount) return false;
            Data.coins -= amount;
            Save();
            return true;
        }

        public static bool TrySpendGems(int amount)
        {
            if (amount < 0 || Data.gems < amount) return false;
            Data.gems -= amount;
            Save();
            return true;
        }
    }
}
