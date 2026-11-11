using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatDefender
{
    /// <summary>Bảng chuỗi tiếng Anh / tiếng Việt. Mọi chữ hiển thị đều đi qua <see cref="T"/>.</summary>
    public static class Loc
    {
        // Mỗi khoá: { tiếng Anh, tiếng Việt }.
        static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "wave", new[] { "Wave {0} / {1}", "Đợt {0} / {1}" } },
            { "lose_reason", new[] { "Lane {0} took the most hits", "Hàng {0} bị thủng nhiều nhất" } },
            { "level", new[] { "Level {0}", "Màn {0}" } },
            { "buy_cat", new[] { "Buy Cat", "Mua mèo" } },
            { "repair", new[] { "Repair Wall", "Sửa tường" } },
            { "retry", new[] { "Retry", "Thử lại" } },
            { "next_level", new[] { "Next Level", "Màn tiếp" } },
            { "menu", new[] { "Menu", "Menu" } },
            { "resume", new[] { "Resume", "Tiếp tục" } },
            { "paused", new[] { "Paused", "Tạm dừng" } },
            { "choose_perk", new[] { "Choose an upgrade", "Chọn một nâng cấp" } },
            { "tap_to_play", new[] { "Tap To Play", "Chạm để chơi" } },
            { "tab_map", new[] { "Levels", "Màn chơi" } },
            { "tab_upgrades", new[] { "Upgrades", "Nâng cấp" } },
            { "tab_addons", new[] { "Addons", "Hỗ trợ" } },
            { "tab_shop", new[] { "Shop", "Cửa hàng" } },
            { "settings", new[] { "Settings", "Cài đặt" } },
            { "language", new[] { "Language: English", "Ngôn ngữ: Tiếng Việt" } },
            { "close", new[] { "Close", "Đóng" } },
            { "max", new[] { "MAX", "TỐI ĐA" } },
            { "up_damage_name", new[] { "Cat Damage", "Sát thương mèo" } },
            { "up_damage_desc", new[] { "+10% bullet damage per level", "+10% sát thương đạn mỗi cấp" } },
            { "up_wall_name", new[] { "Wall HP", "Máu tường" } },
            { "up_wall_desc", new[] { "+20 wall HP per level", "+20 máu tường mỗi cấp" } },
            { "up_coins_name", new[] { "Starting Coins", "Coin khởi đầu" } },
            { "up_coins_desc", new[] { "+8 coins at level start per level", "+8 coin đầu màn mỗi cấp" } },
            { "addon_spikes", new[] { "Spikes", "Bẫy chông" } },
            { "addon_tnt", new[] { "TNT", "TNT" } },
            { "addon_boxing", new[] { "Boxing Cat", "Mèo đấm bốc" } },
            { "addon_guardian", new[] { "Guardian Cat", "Mèo hộ vệ" } },
            { "addon_desc", new[] { "+20% power per level", "+20% sức mạnh mỗi cấp" } },
            { "pack_coins", new[] { "{0} Coins", "{0} coin" } },
            { "perk_firerate_title", new[] { "Rapid Fire", "Bắn nhanh" } },
            { "perk_firerate_desc", new[] { "Cats shoot 20% faster", "Mèo bắn nhanh hơn 20%" } },
            { "perk_damage_title", new[] { "Sharp Claws", "Vuốt sắc" } },
            { "perk_damage_desc", new[] { "+25% bullet damage", "+25% sát thương đạn" } },
            { "perk_splash_title", new[] { "Blast Rounds", "Đạn nổ" } },
            { "perk_splash_desc", new[] { "Bullets splash 30% damage nearby", "Đạn lan 30% sát thương ra xung quanh" } },
            { "perk_coins_title", new[] { "Lucky Coins", "Coin may mắn" } },
            { "perk_coins_desc", new[] { "+30% coins from kills", "+30% coin khi hạ quái" } },
            { "perk_slow_title", new[] { "Sticky Shots", "Đạn dính" } },
            { "perk_slow_desc", new[] { "Bullets slow enemies by 20%", "Đạn làm chậm quái 20%" } },
            { "perk_wall_title", new[] { "Thick Wall", "Tường dày" } },
            { "perk_wall_desc", new[] { "+40 max wall HP", "+40 máu tường tối đa" } },
            { "perk_addonrefill_title", new[] { "Restock", "Tiếp tế" } },
            { "perk_addonrefill_desc", new[] { "+1 use of every addon", "+1 lượt cho mọi hỗ trợ" } },
            { "perk_freecat_title", new[] { "Stray Cat", "Mèo hoang" } },
            { "perk_freecat_desc", new[] { "A free level 2 cat joins", "Tặng một mèo cấp 2" } },
        };

        static Font viFont;

        /// <summary>Phát khi đổi ngôn ngữ; mọi nơi hiển thị chữ cần vẽ lại.</summary>
        public static event Action Changed;

        public static IEnumerable<string> Keys => Table.Keys;
        public static bool IsVietnamese => Profile.Data.language == "vi";

        public static string T(string key, params object[] args)
        {
            if (!Table.TryGetValue(key, out string[] row)) return key;
            string text = row[IsVietnamese ? 1 : 0];
            return args.Length == 0 ? text : string.Format(text, args);
        }

        /// <summary>Chuỗi của một ngôn ngữ cụ thể; dùng cho test.</summary>
        public static string Raw(string key, bool vietnamese) => Table[key][vietnamese ? 1 : 0];

        public static void ToggleLanguage()
        {
            Profile.Data.language = IsVietnamese ? "en" : "vi";
            Profile.Save();
            Changed?.Invoke();
        }

        /// <summary>
        /// Font cho chữ tiếng Việt. Passion One thiếu phần lớn chữ có dấu, nên tiếng Việt tạm dùng font hệ thống.
        /// ponytail: thay bằng một font trang trí có đủ chữ Việt khi chủ dự án cung cấp.
        /// </summary>
        public static Font VietnameseFont =>
            viFont != null ? viFont : viFont = Font.CreateDynamicFontFromOSFont(new[] { "Roboto", "Arial", "Helvetica" }, 32);
    }
}
