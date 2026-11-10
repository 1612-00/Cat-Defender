using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatDefender.EditorTools
{
    /// <summary>Phần dựng scene Menu và các lệnh debug cho tiến trình lưu.</summary>
    public static partial class GameBuilder
    {
        // Mọi thứ trong menu nằm trên một "sân khấu" 2143x1062 đặt giữa màn hình, khớp với ảnh nền của bộ asset.
        // Màn hình hẹp hơn (16:9) bị cắt hai mép 127 px, nên nội dung giữ trong khoảng x 130..2010.
        const float StageW = 2143, StageH = 1062;

        static string BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var meta = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaPath);
            AddonData[] addons = new[] { "Spikes", "Tnt", "Boxing", "Guardian" }
                .Select(n => AssetDatabase.LoadAssetAtPath<AddonData>($"{Root}/Data/Addons/{n}.asset")).ToArray();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            Transform canvas = MakeCanvas();
            var stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
            stage.SetParent(canvas, false);
            stage.sizeDelta = new Vector2(StageW, StageH);

            // --- Màn chào ---
            Image landing = StageImage("Landing", stage, "LandingScreen");
            landing.raycastTarget = true;
            Button landingButton = landing.gameObject.AddComponent<Button>();
            landingButton.transition = Selectable.Transition.None;
            Img("Logo", landing.transform, "Logo", Center, new Vector2(0, 90)).raycastTarget = false;
            Label("TapToPlay", landing.transform, "", 60, new Vector2(0, -380), "tap_to_play");

            // --- Menu chính ---
            Image main = StageImage("Main", stage, "LevelMap_Bg_Screen");

            // Thanh trên: coin, gem, cài đặt.
            Image coinBar = At(Img("CoinBar", main.transform, "CoinBar", Center, Vector2.zero), 1500, 90);
            Text coinText = Label("Coins", coinBar.transform, "0", 38, new Vector2(18, 0));
            Icon("CoinIcon", coinBar.transform, Ui("CoinIcon"), 60, Center, new Vector2(-85, 0));
            Image gemBar = At(Img("GemBar", main.transform, "GemsBarBg", Center, Vector2.zero), 1750, 90);
            Text gemText = Label("Gems", gemBar.transform, "0", 38, new Vector2(18, 0));
            Icon("GemIcon", gemBar.transform, Ui("GemsIcon"), 60, Center, new Vector2(-115, 0));
            Button settingsButton = At(Img("SettingsButton", main.transform, "SettingBtn", Center, Vector2.zero), 1940, 90)
                .gameObject.AddComponent<Button>();

            // Cột tab bên trái.
            string[] tabIcons = { "Icon_home", "Icon_Cat", "Icon_Addon", "Icon_Shop" };
            string[] tabKeys = { "tab_map", "tab_upgrades", "tab_addons", "tab_shop" };
            var tabButtons = new Button[tabIcons.Length];
            for (int i = 0; i < tabIcons.Length; i++)
            {
                Image tab = At(Img("Tab" + i, main.transform, "BtnTab_on", Center, Vector2.zero), 290, 290 + i * 172);
                tab.rectTransform.sizeDelta = new Vector2(212, 152);
                tabButtons[i] = tab.gameObject.AddComponent<Button>();
                Icon("Icon", tab.transform, Ui(tabIcons[i]), 80, Center, new Vector2(0, 18));
                Label("Name", tab.transform, "", 26, new Vector2(0, -48), tabKeys[i]);
            }

            // Vùng nội dung: khung chìm trong ảnh nền, x 545..1875, y 277..929.
            var panels = new GameObject[tabIcons.Length];
            for (int i = 0; i < panels.Length; i++)
            {
                var panel = new GameObject("Panel" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                panel.SetParent(main.transform, false);
                panel.sizeDelta = new Vector2(1330, 652);
                At(panel, 1210, 603);
                panels[i] = panel.gameObject;
            }

            // Bản đồ: 5 chương x 6 level.
            var levelButtons = new Button[MetaConfig.LevelCount];
            for (int i = 0; i < levelButtons.Length; i++)
            {
                Image badge = Img("Level" + (i + 1), panels[0].transform, "LockedLevel", Center,
                    new Vector2(-520 + (i % 6) * 208, 252 - (i / 6) * 126));
                badge.rectTransform.localScale = Vector3.one * 0.74f;
                levelButtons[i] = badge.gameObject.AddComponent<Button>();
                Label("Number", badge.transform, (i + 1).ToString(), 56, new Vector2(0, 12));
            }

            // Nâng cấp vĩnh viễn và addon: mỗi dòng một mục.
            ShopRow[] upgradeRows = meta.upgrades.Select((u, i) =>
                LevelRow("Upgrade" + i, panels[1].transform, 3, i, u.icon, u.nameKey, u.descKey)).ToArray();
            ShopRow[] addonRows = addons.Select((a, i) =>
                LevelRow("Addon" + i, panels[2].transform, 4, i, a.icon, meta.addonNameKeys[i], "addon_desc")).ToArray();

            // Shop: gói coin mua bằng gem.
            ShopRow[] packRows = meta.packs.Select((pack, i) =>
            {
                Image card = Img("Pack" + i, panels[3].transform, "OrangeShopBox", Center, new Vector2((i - 1) * 400, 0));
                card.rectTransform.localScale = Vector3.one * 1.6f;
                var row = card.gameObject.AddComponent<ShopRow>();
                row.title = Label("Title", card.transform, "", 24, new Vector2(0, 148), "");
                row.icon = Icon("Icon", card.transform, pack.icon, 150, Center, new Vector2(0, 30));
                Image buy = Img("Buy", card.transform, "BtnShopYellow", Center, new Vector2(0, -115));
                row.button = buy.gameObject.AddComponent<Button>();
                row.costIcon = Icon("Gem", buy.transform, Ui("GemsIcon"), 44, Center, new Vector2(-34, 4));
                row.cost = Label("Cost", buy.transform, "", 34, new Vector2(22, 4));
                EditorUtility.SetDirty(row);
                return row;
            }).ToArray();

            // Cài đặt: bảng phủ trên menu chính.
            GameObject settings = ResultPanel("SettingsPanel", stage, "BgPaused", out Transform settingsRoot);
            RectTransform settingsTitle = (RectTransform)settingsRoot.Find("Popup");
            settingsTitle.anchoredPosition = new Vector2(0, 300);
            Label("Title", settingsTitle, "", 60, Vector2.zero, "settings");
            BuildSettings(settingsRoot, new Vector2(0, 60));
            Button settingsClose = TextButton("CloseButton", settingsRoot, "BtnOrange", new Vector2(0, -300), "close");

            var controller = canvas.gameObject.AddComponent<MenuController>();
            Set(controller, ("meta", meta), ("landing", landing.gameObject), ("main", main.gameObject),
                ("landingButton", landingButton), ("tabButtons", tabButtons), ("tabPanels", panels),
                ("tabOn", Ui("BtnTab_Active")), ("tabOff", Ui("BtnTab_on")),
                ("coinText", coinText), ("gemText", gemText), ("levelButtons", levelButtons),
                ("levelCleared", Ui("GreenLevel")), ("levelOpen", Ui("OrangeLvl")), ("levelLocked", Ui("LockedLevel")),
                ("upgradeRows", upgradeRows), ("addonRows", addonRows), ("packRows", packRows),
                ("pipSprites", Enumerable.Range(0, 6).Select(n => Ui(n + "Bar")).ToArray()),
                ("settingsButton", settingsButton), ("settingsClose", settingsClose), ("settingsPanel", settings));

            string path = $"{Root}/Scenes/Menu.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        /// <summary>Ảnh nền phủ kín sân khấu.</summary>
        static Image StageImage(string name, Transform stage, string sprite)
        {
            var img = new GameObject(name, typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(stage, false);
            img.sprite = Ui(sprite);
            img.raycastTarget = false;
            Stretch(img.rectTransform);
            return img;
        }

        /// <summary>Đặt phần tử theo toạ độ pixel của ảnh nền (gốc ở góc trên trái sân khấu).</summary>
        /// <remarks>Chỉ dùng cho con trực tiếp của ảnh nền phủ kín sân khấu.</remarks>
        static T At<T>(T graphic, float x, float y) where T : Component
        {
            var rt = (RectTransform)graphic.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Center;
            rt.anchoredPosition = new Vector2(x - StageW * 0.5f, StageH * 0.5f - y);
            return graphic;
        }

        /// <summary>Một dòng nâng cấp: icon, tên, mô tả, thanh cấp độ, nút mua bằng coin.</summary>
        static ShopRow LevelRow(string name, Transform panel, int rowCount, int index, Sprite icon, string nameKey, string descKey)
        {
            float rowHeight = 620f / rowCount;
            Image box = Img(name, panel, "AddonBoxGrey", Center, new Vector2(0, 310 - rowHeight * (index + 0.5f)));
            box.rectTransform.sizeDelta = new Vector2(1280, rowHeight - 14);
            box.raycastTarget = false;

            var row = box.gameObject.AddComponent<ShopRow>();
            row.icon = Icon("Icon", box.transform, icon, Mathf.Min(110, rowHeight - 40), Center, new Vector2(-560, 0));
            row.title = LeftLabel("Title", box.transform, 40, new Vector2(-470, 26), nameKey);
            row.description = LeftLabel("Description", box.transform, 26, new Vector2(-470, -28), descKey);
            row.pips = Img("Pips", box.transform, "0Bar", Center, new Vector2(230, 0));
            row.pips.rectTransform.localScale = Vector3.one * 1.5f;
            row.pips.raycastTarget = false;
            Image buy = Img("Buy", box.transform, "BtnShopYellow", Center, new Vector2(500, 0));
            buy.rectTransform.localScale = Vector3.one * 1.3f;
            row.button = buy.gameObject.AddComponent<Button>();
            row.costIcon = Icon("Coin", buy.transform, Ui("CoinIcon"), 40, Center, new Vector2(-42, 4));
            row.cost = Label("Cost", buy.transform, "", 30, new Vector2(16, 4), "");
            EditorUtility.SetDirty(row);
            return row;
        }

        /// <summary>Chữ căn trái, bắt đầu từ <paramref name="pos"/> (tính từ tâm phần tử cha).</summary>
        static Text LeftLabel(string name, Transform parent, int size, Vector2 pos, string key)
        {
            Text t = Label(name, parent, "", size, Vector2.zero, key);
            t.alignment = TextAnchor.MiddleLeft;
            RectTransform rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Center;
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(600, size + 10);
            rt.anchoredPosition = pos;
            return t;
        }

        // ---------- Lệnh debug cho tiến trình lưu ----------

        static void InitProfile() => Profile.Init(AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaPath));

        [MenuItem("CatDefender/Debug/Reset Save")]
        public static void ResetSave()
        {
            InitProfile();
            Profile.ResetAll();
            Debug.Log("[CatDefender] Đã xoá tiến trình lưu.");
        }

        [MenuItem("CatDefender/Debug/Unlock All Levels")]
        public static void UnlockAll()
        {
            InitProfile();
            Profile.Data.unlockedLevel = MetaConfig.LevelCount - 1;
            Profile.Save();
            Debug.Log("[CatDefender] Đã mở mọi level.");
        }

        [MenuItem("CatDefender/Debug/Add 1000 Coins And 50 Gems")]
        public static void AddCurrency()
        {
            InitProfile();
            Profile.Data.coins += 1000;
            Profile.Data.gems += 50;
            Profile.Save();
            Debug.Log("[CatDefender] Đã cộng 1000 coin và 50 gem.");
        }
    }
}
