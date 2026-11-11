using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CatDefender.EditorTools
{
    /// <summary>
    /// Dựng lại toàn bộ dữ liệu, prefab và scene Gameplay từ bộ asset.
    /// Chạy lại được nhiều lần: asset đã có thì ghi đè nội dung, giữ nguyên GUID.
    /// Mọi chỉ số bản nháp của mèo, quái, addon và 30 level nằm ở đây; sửa rồi chạy lại Build.
    /// </summary>
    public static partial class GameBuilder
    {
        public const string Kit = "Assets/cat-defense-kit/Png";
        public const string Root = "Assets/Game";
        public const string MetaPath = Root + "/Data/Meta/MetaConfig.asset";
        public const int CatCount = 15, RegCount = 8, BossCount = 7, LevelCount = 30;

        // Toạ độ đo từ Area1.png (2143x1062, 100 px/unit, tâm ở gốc toạ độ).
        static readonly float[] SlotRows = { 2.30f, 1.14f, 0f, -1.15f, -2.30f };
        const float SlotColLeft = -7.99f, SlotColRight = -6.60f;
        const float BarX = -3.91f, BarBottom = -1.42f, BarHeight = 4.05f, BarWidth = 0.17f;

        // Quái thường 1..8: máu, tốc độ, sát thương, coin. Loại 3, 5, 7 chạy nhanh, máu thấp.
        static readonly (int hp, float speed, int damage, int reward)[] RegStats =
        {
            (20, 0.8f, 5, 4), (45, 0.7f, 8, 8), (30, 1.3f, 6, 6), (90, 0.65f, 12, 14),
            (60, 1.4f, 10, 10), (180, 0.6f, 18, 24), (120, 1.5f, 14, 18), (360, 0.55f, 25, 40),
        };

        // Level (tính từ 1) có boss, và boss nào (tính từ 1).
        static readonly Dictionary<int, int> BossLevels = new Dictionary<int, int>
        {
            { 6, 1 }, { 12, 2 }, { 18, 3 }, { 24, 4 }, { 27, 5 }, { 29, 6 }, { 30, 7 },
        };

        [MenuItem("CatDefender/1. Fix Sprite Imports")]
        public static void FixImports()
        {
            string[] folders = { "Characters", "Bullets", "Area", "Ui", "Enemies", "CatBoxing", "Cat Guardian", "Explosion", "ShootFx" };
            string[] paths = folders
                .SelectMany(f => Directory.GetFiles($"{Kit}/{f}", "*.png", SearchOption.AllDirectories))
                .Select(p => p.Replace('\\', '/')).ToArray();

            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in paths)
                {
                    var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                    // Ảnh nổ 1305 px x 20 khung hình: thu nhỏ để tiết kiệm bộ nhớ, kích thước trong game không đổi.
                    int maxSize = path.Contains("/Area/") ? 4096 : path.Contains("/Explosion/") ? 512 : 2048;
                    if (imp.textureType == TextureImporterType.Sprite &&
                        imp.spriteImportMode == SpriteImportMode.Single &&
                        !imp.mipmapEnabled && imp.maxTextureSize == maxSize) continue;
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.mipmapEnabled = false;
                    imp.maxTextureSize = maxSize;
                    imp.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"[GameBuilder] Sprite imports fixed: {changed}/{paths.Length}");
        }

        [MenuItem("CatDefender/1. Fix Audio Imports")]
        public static void FixAudioImports()
        {
            string[] paths = Directory.GetFiles($"{Root}/Audio", "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                .Select(path => path.Replace('\\', '/')).ToArray();

            int changed = 0;
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                bool isMusic = path.EndsWith("/main_menu_music_mastered.mp3", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith("/DST-TowerDefenseTheme.mp3", StringComparison.OrdinalIgnoreCase);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                AudioClipLoadType loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                if (settings.loadType == loadType && importer.forceToMono == !isMusic &&
                    settings.preloadAudioData == !isMusic && importer.loadInBackground == isMusic)
                    continue;

                settings.loadType = loadType;
                settings.preloadAudioData = !isMusic;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = !isMusic;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
                changed++;
            }

            Debug.Log($"[GameBuilder] Audio imports fixed: {changed}/{paths.Length}");
        }

        [MenuItem("CatDefender/2. Build Game")]
        public static void Build()
        {
            foreach (string dir in new[] { "Art", "Data", "Data/Cats", "Data/Enemies", "Data/Levels", "Data/Addons", "Data/Perks", "Data/Meta", "Prefabs", "Scenes" })
                Directory.CreateDirectory($"{Root}/{dir}");
            AssetDatabase.Refresh();

            // Mở scene mới trước tiên: việc đổi scene giải phóng asset chưa được tham chiếu,
            // làm mọi asset tải trước đó thành null.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildAudioManager();

            Material unlit = Asset<Material>($"{Root}/Art/SpriteUnlit.mat",
                () => new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")));
            Sprite white = WhiteSprite();

            CatData[] cats = BuildCats();
            EnemyData[] regs = Enumerable.Range(0, RegCount).Select(BuildReg).ToArray();
            SetBehaviours(regs);
            EnemyData[] bosses = Enumerable.Range(0, BossCount).Select(i => BuildBoss(i, regs)).ToArray();
            AddonData[] addons = BuildAddons();
            PerkData[] perks = BuildPerks();
            MetaConfig meta = BuildMeta();
            LevelData[] levels = Enumerable.Range(1, LevelCount).Select(n => BuildLevel(n, regs, bosses)).ToArray();

            BuildPrefab<Cat>("Cat", unlit, 10, typeof(SpriteAnimator));
            BuildEnemyPrefab(unlit, white);
            BuildPrefab<Bullet>("Bullet", unlit, 800);
            BuildPrefab<SpriteAnimator>("Fx", unlit, 850);
            // Tải lại sau khi lưu hết: tham chiếu trả về từ SaveAsPrefabAsset mất hiệu lực khi asset khác được import.
            var catPrefab = AssetDatabase.LoadAssetAtPath<Cat>($"{Root}/Prefabs/Cat.prefab");
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<Enemy>($"{Root}/Prefabs/Enemy.prefab");
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<Bullet>($"{Root}/Prefabs/Bullet.prefab");
            var fxPrefab = AssetDatabase.LoadAssetAtPath<SpriteAnimator>($"{Root}/Prefabs/Fx.prefab");

            // --- Scene ---
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.31f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            SpriteRenderer background = MakeRenderer("Background", null, AreaSprite(1), unlit, -100, Color.white);

            var wallGo = new GameObject("Wall");
            var wall = wallGo.AddComponent<Wall>();
            var barCenter = new Vector3(BarX, BarBottom + BarHeight * 0.5f, 0);
            var back = MakeRenderer("BarBack", wallGo.transform, white, unlit, -50, new Color(0.10f, 0.16f, 0.18f));
            back.transform.position = barCenter;
            back.transform.localScale = new Vector3(BarWidth, BarHeight, 1);
            var fill = MakeRenderer("BarFill", wallGo.transform, white, unlit, -49, new Color(0.56f, 0.88f, 0.12f));
            fill.transform.position = barCenter;
            fill.transform.localScale = new Vector3(BarWidth, BarHeight, 1);
            Set(wall, ("barFill", fill.transform), ("barBottomY", BarBottom), ("barHeight", BarHeight));

            var gameGo = new GameObject("Game");
            var game = gameGo.AddComponent<GameManager>();
            var runner = gameGo.AddComponent<AddonRunner>();
            var board = new GameObject("Board").AddComponent<BoardView>();

            // 10 ô đặt mèo: 5 hàng x 2 cột. Thùng rác nằm trên HUD, cạnh nút mua.
            Vector2[] slots = SlotRows.SelectMany(y => new[] { new Vector2(SlotColLeft, y), new Vector2(SlotColRight, y) }).ToArray();
            int[] slotLanes = slots.Select(s => Array.IndexOf(SlotRows, s.y)).ToArray();
            const float warnLeft = -2.4f, warnRight = 10.72f;
            Set(board, ("game", game), ("catPrefab", catPrefab), ("catsByLevel", cats), ("slots", slots),
                ("slotLanes", slotLanes));

            // Dải đỏ mờ trên từng hàng, bật trong lúc nghỉ để báo hàng sắp có quái.
            var warningsRoot = new GameObject("LaneWarnings").transform;
            SpriteRenderer[] warnings = SlotRows.Select((y, i) =>
            {
                SpriteRenderer w = MakeRenderer("Lane" + i, warningsRoot, white, unlit, -20, new Color(1f, 0.25f, 0.2f, 0.15f));
                w.transform.position = new Vector3((warnLeft + warnRight) * 0.5f, y, 0);
                w.transform.localScale = new Vector3(warnRight - warnLeft, 1.0f, 1);
                w.enabled = false;
                return w;
            }).ToArray();
            var mergePreviewRoot = new GameObject("MergeStrikePreview").transform;
            SpriteRenderer[] mergePreview = SlotRows.Select((y, i) =>
            {
                SpriteRenderer preview = MakeRenderer("Lane" + i, mergePreviewRoot, white, unlit, -19,
                    new Color(0.15f, 0.8f, 1f, 0.24f));
                preview.transform.position = new Vector3((warnLeft + warnRight) * 0.5f, y, 0);
                preview.transform.localScale = new Vector3(warnRight - warnLeft, 1.0f, 1);
                preview.enabled = false;
                return preview;
            }).ToArray();
            SpriteRenderer[] mergeSlotPreview = slots.Select((position, i) =>
            {
                SpriteRenderer preview = MakeRenderer("Slot" + i, mergePreviewRoot, white, unlit, 9,
                    new Color(0.15f, 0.8f, 1f, 0.45f));
                preview.transform.position = position;
                preview.transform.localScale = Vector3.one * 0.95f;
                preview.enabled = false;
                return preview;
            }).ToArray();
            Set(board, ("mergePreviewSlots", mergeSlotPreview), ("mergePreviewLanes", mergePreview));
            Set(runner, ("game", game), ("fxPrefab", fxPrefab));
            Set(game, ("levels", levels), ("addons", addons), ("perkPool", perks), ("meta", meta), ("board", board), ("wall", wall),
                ("laneYs", SlotRows), ("laneWarnings", warnings),
                ("addonRunner", runner), ("background", background),
                ("enemyPrefab", enemyPrefab), ("bulletPrefab", bulletPrefab), ("fxPrefab", fxPrefab), ("mergeStrikeSprite", white),
                ("shootFx", LoadSprites($"{Kit}/ShootFx")), ("explosionFx", LoadSprites($"{Kit}/Explosion")));

            BuildHud(game, board, addons);

            string scenePath = $"{Root}/Scenes/Gameplay.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Scene Menu dựng sau cùng và là scene mở đầu game. Mở scene mới làm mọi asset ở trên thành null,
            // nên BuildMenuScene tự tải lại những gì nó cần.
            string menuPath = BuildMenuScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(menuPath, true),
                new EditorBuildSettingsScene(scenePath, true),
            };

            PlayerSettings.productName = "Cats Defender";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[GameBuilder] Game built: " + scenePath);
        }

        // ---------- Dữ liệu ----------

        static CatData[] BuildCats()
        {
            // Màu đạn theo bậc: cấp 1–5 vàng, 6–10 xanh, 11–15 đỏ.
            Sprite[] bullets = new[] { "Artboard 1", "Artboard 1 copy", "Artboard 1 copy 2" }
                .Select(n => LoadSprite($"{Kit}/Bullets/{n}.png")).ToArray();
            var cats = new CatData[CatCount];
            for (int i = 0; i < CatCount; i++)
            {
                CatData c = Asset($"{Root}/Data/Cats/Cat{i + 1:00}.asset", ScriptableObject.CreateInstance<CatData>);
                c.level = i + 1;
                // Ghép 2 con cho ra sát thương x2,1: ghép luôn có lợi hơn để hai con cấp thấp.
                c.damage = Mathf.RoundToInt(4 * Mathf.Pow(2.1f, i));
                c.fireInterval = 0.8f;
                c.bulletSpeed = 22f;
                c.bulletSprite = bullets[i / 5];
                c.idle = LoadSprites($"{Kit}/Characters/C{i + 1}/Idle");
                c.shoot = LoadSprites($"{Kit}/Characters/C{i + 1}/Shoot");
                EditorUtility.SetDirty(c);
                cats[i] = c;
            }
            return cats;
        }

        static EnemyData BuildReg(int i)
        {
            (int hp, float speed, int damage, int reward) = RegStats[i];
            return BuildEnemy($"Reg{i + 1}", $"Enemy Reg {i + 1}", hp, speed, damage, 1f, reward, false, 1f);
        }

        /// <summary>Hành vi riêng của quái thường: giáp, đổi hàng, tách đôi khi chết.</summary>
        static void SetBehaviours(EnemyData[] regs)
        {
            var steel = new Color(0.72f, 0.82f, 1f);
            regs[1].armor = 3; regs[1].tint = steel;                    // Reg 2: giáp nhẹ
            regs[2].switchesLane = true;                                // Reg 3: nhanh, đổi hàng
            regs[3].splitInto = regs[0]; regs[3].splitCount = 2;        // Reg 4: chết tách thành 2 Reg 1
            regs[4].switchesLane = true;                                // Reg 5: nhanh, đổi hàng
            regs[5].armor = 10; regs[5].tint = steel;                   // Reg 6: giáp nặng
            regs[6].switchesLane = true; regs[6].armor = 4;             // Reg 7: nhanh, đổi hàng, giáp nhẹ
            regs[6].tint = steel;
            regs[7].splitInto = regs[5]; regs[7].splitCount = 2;        // Reg 8: chết tách thành 2 Reg 6
            foreach (EnemyData e in regs) EditorUtility.SetDirty(e);
        }

        /// <summary>Boss gọi 2 đàn em mỗi 7 giây và làm choáng mèo trên hàng của nó mỗi 9 giây.</summary>
        static EnemyData BuildBoss(int i, EnemyData[] regs)
        {
            EnemyData e = BuildEnemy($"Boss{i + 1}", $"Enemy Boss {i + 1}",
                hp: Mathf.RoundToInt(600 * Mathf.Pow(1.9f, i)), speed: 0.4f, damage: 30 + 10 * i,
                attackInterval: 1.5f, reward: 100 * (i + 1), isBoss: true, scale: 1.25f);
            e.summon = regs[Mathf.Min(i, 4)];
            e.summonCount = 2;
            e.summonInterval = 7f;
            e.stunInterval = 9f;
            e.stunDuration = 2.5f;
            EditorUtility.SetDirty(e);
            return e;
        }

        static EnemyData BuildEnemy(string asset, string folder, int hp, float speed, int damage,
            float attackInterval, int reward, bool isBoss, float scale)
        {
            EnemyData e = Asset($"{Root}/Data/Enemies/{asset}.asset", ScriptableObject.CreateInstance<EnemyData>);
            e.hp = hp;
            e.speed = speed;
            e.damage = damage;
            e.attackInterval = attackInterval;
            e.coinReward = reward;
            e.isBoss = isBoss;
            e.scale = scale;
            // Đặt lại hành vi riêng về mặc định; SetBehaviours và BuildBoss gán lại sau.
            e.armor = 0;
            e.switchesLane = false;
            e.splitInto = null;
            e.splitCount = 0;
            e.summon = null;
            e.summonCount = 0;
            e.summonInterval = 0;
            e.stunInterval = 0;
            e.stunDuration = 0;
            e.tint = Color.white;
            e.walk = LoadSprites($"{Kit}/Enemies/{folder}/Walk");
            e.attack = LoadSprites($"{Kit}/Enemies/{folder}/Attack");
            e.dead = LoadSprites($"{Kit}/Enemies/{folder}/Dead");
            EditorUtility.SetDirty(e);
            return e;
        }

        static AddonData[] BuildAddons() => new[]
        {
            BuildAddon("Spikes", AddonType.Spikes, 8, 2, duration: 8f, damage: 12, tick: 0.5f, range: 2f, null),
            BuildAddon("Tnt", AddonType.Tnt, 6, 2, duration: 0f, damage: 150, tick: 0f, range: 3f, null),
            BuildAddon("Boxing", AddonType.Boxing, 7, 2, duration: 10f, damage: 25, tick: 0.5f, range: 2.5f, "CatBoxing/Attack"),
            BuildAddon("Guardian", AddonType.Guardian, 5, 2, duration: 8f, damage: 0, tick: 0.5f, range: 0f, "Cat Guardian/Attack"),
        };

        static PerkData[] BuildPerks() => new[]
        {
            BuildPerk("FireRate", PerkType.FireRate, "AddonIcon1", 0.2f),
            BuildPerk("Damage", PerkType.Damage, "Up3", 0.25f),
            BuildPerk("Splash", PerkType.Splash, "AddonIcon3", 0.3f),
            BuildPerk("Coins", PerkType.Coins, "AddonIcon2", 0.3f),
            BuildPerk("Slow", PerkType.Slow, "AddonIcon4", 0.2f),
            BuildPerk("Wall", PerkType.Wall, "WallIcon", 40f),
            BuildPerk("AddonRefill", PerkType.AddonRefill, "Icon_Addon", 1f),
            BuildPerk("FreeCat", PerkType.FreeCat, "Icon_Cat", 2f),
        };

        /// <summary>Chữ của thẻ nằm trong Loc với khoá perk_{tên thường}_title và perk_{tên thường}_desc.</summary>
        static PerkData BuildPerk(string name, PerkType type, string icon, float value)
        {
            PerkData p = Asset($"{Root}/Data/Perks/{name}.asset", ScriptableObject.CreateInstance<PerkData>);
            p.type = type;
            p.icon = Ui(icon);
            p.titleKey = $"perk_{name.ToLowerInvariant()}_title";
            p.descKey = $"perk_{name.ToLowerInvariant()}_desc";
            p.value = value;
            EditorUtility.SetDirty(p);
            return p;
        }

        /// <summary>Chỉ số bản nháp của phần ngoài màn chơi: nâng cấp vĩnh viễn, thưởng, gói coin.</summary>
        static MetaConfig BuildMeta()
        {
            MetaConfig m = Asset(MetaPath, ScriptableObject.CreateInstance<MetaConfig>);
            m.maxLevel = 5;
            m.upgrades = new[]
            {
                new MetaConfig.UpgradeDef { id = UpgradeId.CatDamage, icon = Ui("Up3"), nameKey = "up_damage_name", descKey = "up_damage_desc", baseCost = 100, costGrowth = 1.8f, valuePerLevel = 0.1f },
                new MetaConfig.UpgradeDef { id = UpgradeId.WallHp, icon = Ui("WallIcon"), nameKey = "up_wall_name", descKey = "up_wall_desc", baseCost = 80, costGrowth = 1.8f, valuePerLevel = 20f },
                new MetaConfig.UpgradeDef { id = UpgradeId.StartCoins, icon = Ui("CoinIcon"), nameKey = "up_coins_name", descKey = "up_coins_desc", baseCost = 80, costGrowth = 1.8f, valuePerLevel = 8f },
            };
            // Cùng thứ tự với BuildAddons.
            m.addonNameKeys = new[] { "addon_spikes", "addon_tnt", "addon_boxing", "addon_guardian" };
            m.addonBaseCost = 80;
            m.addonCostGrowth = 1.8f;
            m.addonBonusPerLevel = 0.2f;
            m.packs = new[]
            {
                new MetaConfig.CoinPack { icon = Ui("ShopIcon2"), gems = 10, coins = 300 },
                new MetaConfig.CoinPack { icon = Ui("ShopIcon3"), gems = 25, coins = 900 },
                new MetaConfig.CoinPack { icon = Ui("ShopIcon1"), gems = 50, coins = 2200 },
            };
            m.winBaseCoins = 40;
            m.winCoinsPerLevel = 15;
            m.loseFraction = 0.3f;
            m.firstClearGems = 5;
            m.bossFirstClearGems = 15;
            m.startGems = 10;
            EditorUtility.SetDirty(m);
            return m;
        }

        static AddonData BuildAddon(string name, AddonType type, int icon, int uses, float duration, int damage,
            float tick, float range, string framesFolder)
        {
            AddonData a = Asset($"{Root}/Data/Addons/{name}.asset", ScriptableObject.CreateInstance<AddonData>);
            a.type = type;
            a.icon = Ui("AddonIcon" + icon);
            a.uses = uses;
            a.duration = duration;
            a.damage = damage;
            a.tickInterval = tick;
            a.range = range;
            a.frames = framesFolder == null ? new Sprite[0] : LoadSprites($"{Kit}/{framesFolder}");
            EditorUtility.SetDirty(a);
            return a;
        }

        /// <summary>
        /// Sinh level bản nháp theo công thức (cân bằng thật ở mốc 5):
        /// - 5 chương x 6 level, mỗi chương một nền; số wave 5..9.
        /// - Loại quái mở dần: cứ 3 level thêm một loại.
        /// - Mỗi wave có "ngân sách máu" tăng theo wave và level, chia cho loại luân phiên và loại mới nhất.
        /// - Máu quái x1,10 mỗi level; coin thưởng và coin khởi đầu x1,08 mỗi level.
        /// </summary>
        static LevelData BuildLevel(int number, EnemyData[] regs, EnemyData[] bosses)
        {
            LevelData level = Asset($"{Root}/Data/Levels/Level{number:00}.asset", ScriptableObject.CreateInstance<LevelData>);
            int chapter = (number - 1) / 6;
            level.background = AreaSprite(chapter + 1);
            level.enemyHpScale = Mathf.Pow(1.10f, number - 1);
            level.rewardScale = Mathf.Pow(1.08f, number - 1);
            level.startCoins = Mathf.RoundToInt(30 * Mathf.Pow(1.08f, number - 1));
            level.wallHp = 150;
            level.mergeStrikeMultiplier = 4f;
            // Nghỉ 5 giây trước mỗi wave để người chơi kịp xếp lại mèo theo hàng được báo.
            level.firstWaveDelay = 5f;
            level.waveDelay = 5f;

            int unlocked = Mathf.Min(RegCount, 1 + number / 3);
            int waveCount = 5 + chapter;
            level.waves = new List<LevelData.Wave>();
            for (int w = 0; w < waveCount; w++)
            {
                float budget = 100f * (1 + 0.35f * w) * (1 + 0.04f * (number - 1));
                int a = w % unlocked, b = unlocked - 1;
                var wave = new LevelData.Wave();
                if (a == b)
                    wave.groups.Add(Group(regs[a], budget));
                else
                {
                    wave.groups.Add(Group(regs[a], budget * 0.6f));
                    wave.groups.Add(Group(regs[b], budget * 0.4f));
                }
                level.waves.Add(wave);
            }

            if (BossLevels.TryGetValue(number, out int boss))
                level.waves[waveCount - 1].groups.Add(new LevelData.Group { enemy = bosses[boss - 1], count = 1, interval = 1f });

            EditorUtility.SetDirty(level);
            return level;
        }

        static LevelData.Group Group(EnemyData enemy, float hpBudget)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(hpBudget / enemy.hp), 2, 25);
            return new LevelData.Group { enemy = enemy, count = count, interval = Mathf.Clamp(10f / count, 0.4f, 1.5f) };
        }

        // ---------- Prefab ----------

        static void BuildPrefab<T>(string name, Material material, int sortingOrder, params Type[] extra) where T : Component
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sharedMaterial = material;
            sr.sortingOrder = sortingOrder;
            foreach (Type t in extra) go.AddComponent(t);
            if (go.GetComponent<T>() == null) go.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        /// <summary>Quái kèm thanh máu trên đầu (chỉ bật cho boss).</summary>
        static void BuildEnemyPrefab(Material material, Sprite white)
        {
            var go = new GameObject("Enemy", typeof(SpriteRenderer), typeof(SpriteAnimator));
            go.GetComponent<SpriteRenderer>().sharedMaterial = material;
            var enemy = go.AddComponent<Enemy>();

            var bar = new GameObject("HpBar").transform;
            bar.SetParent(go.transform, false);
            bar.localPosition = new Vector3(0, 1.35f, 0);
            MakeRenderer("Back", bar, white, material, 950, new Color(0.1f, 0.1f, 0.1f, 0.85f))
                .transform.localScale = new Vector3(1.6f, 0.2f, 1);
            // Điểm neo ở mép trái để thanh máu co về bên trái khi scale.x giảm.
            var pivot = new GameObject("FillPivot").transform;
            pivot.SetParent(bar, false);
            pivot.localPosition = new Vector3(-0.75f, 0, 0);
            Transform fill = MakeRenderer("Fill", pivot, white, material, 951, new Color(0.9f, 0.2f, 0.2f)).transform;
            fill.localPosition = new Vector3(0.75f, 0, 0);
            fill.localScale = new Vector3(1.5f, 0.12f, 1);

            Set(enemy, ("hpBar", bar.gameObject), ("hpFill", pivot));
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/Enemy.prefab");
            Object.DestroyImmediate(go);
        }

        // ---------- HUD ----------

        static void BuildHud(GameManager game, BoardView board, AddonData[] addons)
        {
            Transform canvas = MakeCanvas();
            Transform root = SafeRoot(canvas);

            Image coinBar = Img("CoinBar", root, "CoinBar", TopLeft, new Vector2(140, -70));
            Text coinText = Label("Coins", coinBar.transform, "0", 40, new Vector2(20, 0));
            Img("CoinIcon", coinBar.transform, "CoinIcon", new Vector2(0, 0.5f), new Vector2(-20, 0))
                .rectTransform.pivot = Center;

            Button pause = SoundButton(Img("PauseButton", root, "SettingBtn", TopRight, new Vector2(-50, -50)).gameObject.AddComponent<Button>(), AudioCue.Open);
            Image waveBar = Img("WaveBar", root, "WaveBar", TopRight, new Vector2(-190, -62));
            Text waveText = Label("Wave", waveBar.transform, "", 30, new Vector2(18, 0), "");

            Image levelBox = Img("LevelBox", root, "AddonBoxNumber", Top, new Vector2(0, -70));
            levelBox.rectTransform.localScale = Vector3.one * 1.4f;
            Text levelText = Label("Level", levelBox.transform, "", 30, Vector2.zero, "");

            // Thùng rác: kéo mèo thả vào đây để bỏ. Đặt bên trái nút mua.
            Image trash = Icon("Trash", root, EnsureSprite($"{Root}/Art/Trash.png", 256), 120, BottomLeft, new Vector2(110, 111));
            Set(board, ("trashIcon", trash.rectTransform));

            Button buy = Btn("BuyButton", root, "BtnGreen", BottomLeft, new Vector2(200, 50), "buy_cat", out Text buyCost, AudioCue.None);
            Button repair = Btn("RepairButton", root, "BtnOrange", BottomLeft, new Vector2(570, 50), "repair", out Text repairCost, AudioCue.None);

            var addonButtons = new Button[addons.Length];
            var addonCounts = new Text[addons.Length];
            for (int i = 0; i < addons.Length; i++)
            {
                Image box = Img("Addon" + addons[i].name, root, "YellowBox", BottomRight,
                    new Vector2(-90 - (addons.Length - 1 - i) * 175, 40));
                addonButtons[i] = box.gameObject.AddComponent<Button>();
                Icon("Icon", box.transform, addons[i].icon, 110, Center, new Vector2(0, 8));
                addonCounts[i] = Label("Count", box.transform, "", 30, new Vector2(40, -50));
            }

            // Các bảng phủ kín màn hình nằm ngoài vùng an toàn để lớp tối che hết.
            GameObject win = ResultPanel("WinPanel", canvas, "WinPopUp", out Transform winRoot);
            Text winCoins = RewardRow("RewardCoins", winRoot, Ui("CoinIcon"), new Vector2(0, -10)).GetComponentInChildren<Text>();
            GameObject gemRow = RewardRow("RewardGems", winRoot, Ui("GemsIcon"), new Vector2(0, -90));
            Button next = TextButton("NextLevelButton", winRoot, "BtnGreen", new Vector2(-190, -300), "next_level", cue: AudioCue.Confirm);
            Button winMenu = TextButton("MenuButton", winRoot, "BtnOrange", new Vector2(190, -300), "menu");

            GameObject lose = ResultPanel("LosePanel", canvas, "LosePopUp", out Transform loseRoot);
            Text loseCoins = RewardRow("RewardCoins", loseRoot, Ui("CoinIcon"), new Vector2(0, -170)).GetComponentInChildren<Text>();
            Button loseRetry = TextButton("RetryButton", loseRoot, "BtnGreen", new Vector2(-190, -300), "retry");
            Button loseMenu = TextButton("MenuButton", loseRoot, "BtnOrange", new Vector2(190, -300), "menu");

            GameObject pausePanel = ResultPanel("PausePanel", canvas, "BgPaused", out Transform pauseRoot);
            RectTransform pauseTitle = (RectTransform)pauseRoot.Find("Popup");
            pauseTitle.anchoredPosition = new Vector2(0, 300);
            Label("Title", pauseTitle, "", 60, Vector2.zero, "paused");
            BuildSettings(pauseRoot, new Vector2(0, 60));
            Button resume = TextButton("ResumeButton", pauseRoot, "BtnGreen", new Vector2(-380, -300), "resume", cue: AudioCue.Close);
            Button pauseRetry = TextButton("RetryButton", pauseRoot, "BtnOrange", new Vector2(0, -300), "retry");
            Button pauseMenu = TextButton("MenuButton", pauseRoot, "BtnOrange", new Vector2(380, -300), "menu");

            // Bảng chọn 1 trong 3 thẻ nâng cấp sau mỗi wave.
            GameObject perkPanel = ResultPanel("PerkPanel", canvas, "TitleRibbon", out Transform perkRoot);
            RectTransform ribbon = (RectTransform)perkRoot.Find("Popup");
            ribbon.anchoredPosition = new Vector2(0, 380);
            Label("Title", ribbon, "", 44, new Vector2(0, 14), "choose_perk");
            const int cardCount = 3;
            var perkButtons = new Button[cardCount];
            var perkIcons = new Image[cardCount];
            var perkTitles = new Text[cardCount];
            var perkDescriptions = new Text[cardCount];
            for (int i = 0; i < cardCount; i++)
            {
                Image card = Img("Card" + i, perkRoot, i == 1 ? "OrangeShopBox" : "BlueShopBox", Center,
                    new Vector2((i - 1) * 400, -40));
                card.rectTransform.localScale = Vector3.one * 1.5f;
                perkButtons[i] = SoundButton(card.gameObject.AddComponent<Button>(), AudioCue.UpgradeConfirm);
                perkTitles[i] = Label("Title", card.transform, "", 26, new Vector2(0, 148), "");
                perkIcons[i] = Icon("Icon", card.transform, null, 110, Center, new Vector2(0, 40));
                Text d = Label("Description", card.transform, "", 22, Vector2.zero, "");
                d.horizontalOverflow = HorizontalWrapMode.Wrap;
                d.rectTransform.offsetMin = new Vector2(16, 20);
                d.rectTransform.offsetMax = new Vector2(-16, -220);
                perkDescriptions[i] = d;
            }

            var hud = canvas.gameObject.AddComponent<Hud>();
            Set(hud, ("game", game), ("coinText", coinText), ("waveText", waveText), ("levelText", levelText),
                ("buyCostText", buyCost), ("repairCostText", repairCost),
                ("buyButton", buy), ("repairButton", repair),
                ("addonButtons", addonButtons), ("addonCountTexts", addonCounts),
                ("winPanel", win), ("losePanel", lose), ("nextLevelButton", next),
                ("restartButtons", new[] { loseRetry, pauseRetry }),
                ("menuButtons", new[] { winMenu, loseMenu, pauseMenu }),
                ("rewardCoinTexts", new[] { winCoins, loseCoins }),
                ("rewardGemText", gemRow.GetComponentInChildren<Text>()), ("rewardGemRow", gemRow),
                ("pauseButton", pause), ("resumeButton", resume), ("pausePanel", pausePanel),
                ("perkPanel", perkPanel), ("perkButtons", perkButtons), ("perkIcons", perkIcons),
                ("perkTitles", perkTitles), ("perkDescriptions", perkDescriptions));
        }

        static Sprite Ui(string name) => LoadSprite($"{Kit}/Ui/{name}.png");

        /// <summary>Ảnh kích thước gốc; anchor và pivot trùng nhau tại <paramref name="corner"/>.</summary>
        static Image Img(string name, Transform parent, string sprite, Vector2 corner, Vector2 pos)
        {
            var img = new GameObject(name, typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = Ui(sprite);
            img.SetNativeSize();
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = pos;
            return img;
        }

        /// <summary>
        /// Chữ phủ kín phần tử cha, căn giữa, có viền tối.
        /// <paramref name="key"/>: null = chữ không dịch (con số); "" = chữ do code gán nhưng cần đổi font theo ngôn ngữ;
        /// khác rỗng = khoá trong Loc, tự điền chữ.
        /// </summary>
        static Text Label(string name, Transform parent, string text, int size, Vector2 offset, string key = null)
        {
            var t = new GameObject(name, typeof(Text), typeof(Outline)).GetComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/Passion_One/PassionOne-Regular.ttf");
            t.fontSize = size;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = t.GetComponent<Outline>();
            o.effectColor = new Color(0.2f, 0.1f, 0.05f, 0.9f);
            o.effectDistance = new Vector2(2, -2);
            Stretch(t.rectTransform);
            t.rectTransform.anchoredPosition = offset;
            if (key != null) Set(t.gameObject.AddComponent<LocText>(), ("key", key));
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Ảnh vuông kích thước cho trước, giữ tỉ lệ, không chặn chạm.</summary>
        static Image Icon(string name, Transform parent, Sprite sprite, float size, Vector2 corner, Vector2 pos)
        {
            var img = new GameObject(name, typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            RectTransform rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = Center;
            rt.anchoredPosition = pos;
            return img;
        }

        /// <summary>Nút có tiêu đề ở trên và giá ở dưới.</summary>
        static Button Btn(string name, Transform parent, string sprite, Vector2 corner, Vector2 pos,
            string titleKey, out Text cost, AudioCue cue = AudioCue.UiClick)
        {
            Image img = Img(name, parent, sprite, corner, pos);
            Label("Title", img.transform, "", 38, new Vector2(0, 22), titleKey);
            cost = Label("Cost", img.transform, "", 36, new Vector2(0, -24));
            return SoundButton(img.gameObject.AddComponent<Button>(), cue);
        }

        static Button TextButton(string name, Transform parent, string sprite, Vector2 pos, string key, int size = 44, AudioCue cue = AudioCue.UiClick)
        {
            Image img = Img(name, parent, sprite, Center, pos);
            Label("Title", img.transform, "", size, Vector2.zero, key);
            return SoundButton(img.gameObject.AddComponent<Button>(), cue);
        }

        static Button SoundButton(Button button, AudioCue cue)
        {
            AudioButton audioButton = button.gameObject.AddComponent<AudioButton>();
            Set(audioButton, ("cue", cue));
            return button;
        }

        static void BuildAudioManager()
        {
            FixAudioImports();
            AudioConfig config = BuildAudioConfig();
            AudioManager manager = new GameObject("AudioManager").AddComponent<AudioManager>();
            Set(manager, ("config", config));
        }

        static AudioConfig BuildAudioConfig()
        {
            const string configPath = Root + "/Data/AudioConfig.asset";
            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AudioConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }

            (AudioCue cue, string path)[] mappings =
            {
                (AudioCue.UiClick, "UI_Interaction/click_001.ogg"),
                (AudioCue.SelectCat, "UI_Interaction/select_001.ogg"),
                (AudioCue.DropCat, "UI_Interaction/drop_001.ogg"),
                (AudioCue.Open, "UI_Interaction/open_001.ogg"),
                (AudioCue.Close, "UI_Interaction/close_001.ogg"),
                (AudioCue.Error, "UI_Interaction/error_004.ogg"),
                (AudioCue.Confirm, "UI_Interaction/confirmation_001.ogg"),
                (AudioCue.Switch, "UI_Interaction/switch_002.ogg"),
                (AudioCue.Tick, "UI_Interaction/tick_002.ogg"),
                (AudioCue.CatShoot, "Combat/laserSmall_000.ogg"),
                (AudioCue.CatShootAlternate, "Combat/laserSmall_001.ogg"),
                (AudioCue.SpecialShot, "Combat/laserRetro_000.ogg"),
                (AudioCue.HeavyShot, "Combat/laserLarge_000.ogg"),
                (AudioCue.BulletHit, "Combat/impactSoft_medium_002.ogg"),
                (AudioCue.BoxingHit, "Combat/impactSoft_heavy_000.ogg"),
                (AudioCue.Explosion, "Combat/explosionCrunch_000.ogg"),
                (AudioCue.BossExplosion, "Combat/lowFrequency_explosion_000.ogg"),
                (AudioCue.EnemyHit, "Combat/impactSoft_heavy_000.ogg"),
                (AudioCue.ArmorHit, "Combat/impactMetal_000.ogg"),
                (AudioCue.Coin, "Economy_Upgrade/glass_001.ogg"),
                (AudioCue.CoinAlternate, "Economy_Upgrade/pluck_001.ogg"),
                (AudioCue.CatPurchase, "Economy_Upgrade/confirmation_001.ogg"),
                (AudioCue.Upgrade, "Economy_Upgrade/maximize_005.ogg"),
                (AudioCue.UpgradeConfirm, "Economy_Upgrade/confirmation_003.ogg"),
                (AudioCue.Shield, "Economy_Upgrade/forceField_000.ogg"),
                (AudioCue.Skill, "Economy_Upgrade/laserRetro_001.ogg"),
                (AudioCue.WideExplosion, "Economy_Upgrade/explosionCrunch_002.ogg"),
                (AudioCue.Merge, "UI_Interaction/merge-cat.wav"),
                (AudioCue.LevelComplete, "UI_Interaction/game-level-completed.wav"),
                (AudioCue.GameOver, "UI_Interaction/musical-game-over.wav"),
                (AudioCue.WaveStart, "UI_Interaction/classic-alarm.wav"),
                (AudioCue.BossAppear, "UI_Interaction/giant-monster-footstep.wav"),
                (AudioCue.PerkOffer, "UI_Interaction/happy-bell-alert.wav"),
                (AudioCue.CatTrash, "UI_Interaction/money-bag-drop.wav"),
                (AudioCue.WallHit, "Combat/impactWood_heavy_004.ogg"),
                (AudioCue.WallCritical, "Combat/facility-alarm.wav"),
                (AudioCue.BossStun, "Combat/impactPlate_heavy_004.ogg"),
                (AudioCue.EnemySplit, "Combat/long-pop.wav"),
                (AudioCue.BoxingActivate, "Economy_Upgrade/confirmation_001.ogg")
            };

            var existing = new Dictionary<AudioCue, AudioCueClip>();
            if (config.cues != null)
                foreach (AudioCueClip entry in config.cues)
                    if (entry != null && !existing.ContainsKey(entry.cue)) existing.Add(entry.cue, entry);

            AudioCueClip[] entries = new AudioCueClip[mappings.Length];
            for (int i = 0; i < mappings.Length; i++)
            {
                string path = $"{Root}/Audio/{mappings[i].path}";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) throw new FileNotFoundException("Missing audio clip: " + path);
                if (existing.TryGetValue(mappings[i].cue, out AudioCueClip entry))
                {
                    entry.clip = clip;
                    entries[i] = entry;
                }
                else
                {
                    entries[i] = new AudioCueClip
                    {
                        cue = mappings[i].cue,
                        clip = clip,
                        volume = AudioVolume(mappings[i].cue),
                        pitchVariation = AudioPitchVariation(mappings[i].cue),
                        volumeVariation = AudioVolumeVariation(mappings[i].cue),
                        minInterval = AudioMinInterval(mappings[i].cue),
                        priority = AudioPriority(mappings[i].cue),
                        bus = AudioBus(mappings[i].cue)
                    };
                }
            }
            config.cues = entries;
            config.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/UI_Interaction/main_menu_music_mastered.mp3");
            config.gameplayMusic = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/UI_Interaction/DST-TowerDefenseTheme.mp3");
            if (config.menuMusic == null || config.gameplayMusic == null)
                throw new FileNotFoundException("Both menu and gameplay music clips must be present in Assets/Game/Audio/UI_Interaction.");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        static float AudioVolume(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.UiClick:
                case AudioCue.Switch:
                case AudioCue.SelectCat:
                case AudioCue.DropCat:
                case AudioCue.Tick: return 0.18f;
                case AudioCue.Open:
                case AudioCue.Close:
                case AudioCue.Error:
                case AudioCue.Confirm: return 0.30f;
                case AudioCue.CatShoot:
                case AudioCue.CatShootAlternate:
                case AudioCue.SpecialShot:
                case AudioCue.HeavyShot: return 0.34f;
                case AudioCue.BulletHit:
                case AudioCue.EnemyHit:
                case AudioCue.ArmorHit: return 0.34f;
                case AudioCue.Coin:
                case AudioCue.CoinAlternate: return 0.22f;
                case AudioCue.CatPurchase:
                case AudioCue.Upgrade:
                case AudioCue.UpgradeConfirm:
                case AudioCue.BoxingActivate: return 0.32f;
                case AudioCue.BoxingHit:
                    return 0.34f;
                case AudioCue.WallHit: return 0.65f;
                case AudioCue.Explosion:
                case AudioCue.WideExplosion: return 0.40f;
                case AudioCue.BossExplosion:
                case AudioCue.BossAppear:
                case AudioCue.LevelComplete:
                case AudioCue.GameOver:
                case AudioCue.WaveStart:
                case AudioCue.WallCritical: return 0.70f;
                case AudioCue.Shield:
                case AudioCue.Skill:
                case AudioCue.Merge:
                case AudioCue.PerkOffer:
                case AudioCue.CatTrash:
                case AudioCue.BossStun:
                case AudioCue.EnemySplit: return 0.26f;
                default: return 0.25f;
            }
        }

        static float AudioPitchVariation(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.CatShoot:
                case AudioCue.CatShootAlternate:
                case AudioCue.SpecialShot:
                case AudioCue.HeavyShot:
                case AudioCue.BulletHit:
                case AudioCue.EnemyHit:
                case AudioCue.ArmorHit:
                case AudioCue.Coin:
                case AudioCue.CoinAlternate: return 0.06f;
                default: return 0f;
            }
        }

        static float AudioVolumeVariation(AudioCue cue) => AudioPitchVariation(cue) > 0f ? 0.10f : 0f;

        static AudioCueBus AudioBus(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.UiClick:
                case AudioCue.SelectCat:
                case AudioCue.DropCat:
                case AudioCue.Open:
                case AudioCue.Close:
                case AudioCue.Error:
                case AudioCue.Confirm:
                case AudioCue.Switch:
                case AudioCue.UpgradeConfirm:
                case AudioCue.PerkOffer: return AudioCueBus.UI;
                default: return AudioCueBus.Gameplay;
            }
        }

        static int AudioPriority(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.CatShoot:
                case AudioCue.CatShootAlternate:
                case AudioCue.SpecialShot:
                case AudioCue.HeavyShot:
                case AudioCue.Coin:
                case AudioCue.CoinAlternate: return 5;
                case AudioCue.WallHit:
                case AudioCue.WallCritical:
                case AudioCue.BossExplosion:
                case AudioCue.BossAppear:
                case AudioCue.LevelComplete:
                case AudioCue.GameOver: return 100;
                case AudioCue.BossStun:
                case AudioCue.WaveStart: return 80;
                default: return 25;
            }
        }

        static float AudioMinInterval(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.CatShoot:
                case AudioCue.CatShootAlternate:
                case AudioCue.SpecialShot:
                case AudioCue.HeavyShot: return 0.10f;
                case AudioCue.BulletHit:
                case AudioCue.EnemyHit:
                case AudioCue.ArmorHit:
                case AudioCue.Coin:
                case AudioCue.CoinAlternate:
                case AudioCue.Explosion:
                case AudioCue.BoxingHit: return 0.08f;
                case AudioCue.WallHit: return 0.25f;
                case AudioCue.WallCritical:
                case AudioCue.BossStun: return 1f;
                case AudioCue.Tick: return 0.5f;
                default: return 0.06f;
            }
        }

        /// <summary>Lớp phủ tối toàn màn hình kèm một ảnh ở giữa (tên "Popup"); <paramref name="content"/> là nơi gắn nút.</summary>
        static GameObject ResultPanel(string name, Transform parent, string popup, out Transform content)
        {
            var dim = new GameObject(name, typeof(Image)).GetComponent<Image>();
            dim.transform.SetParent(parent, false);
            dim.color = new Color(0, 0, 0, 0.6f);
            Stretch(dim.rectTransform);
            Img("Popup", dim.transform, popup, Center, new Vector2(0, 80));
            content = dim.transform;
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        /// <summary>Dòng "icon +số" hiển thị phần thưởng; trả về GameObject chứa cả dòng.</summary>
        static GameObject RewardRow(string name, Transform parent, Sprite icon, Vector2 pos)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = (RectTransform)row.transform;
            rt.sizeDelta = new Vector2(300, 70);
            rt.anchoredPosition = pos;
            Icon("Icon", row.transform, icon, 64, Center, new Vector2(-70, 0));
            Label("Amount", row.transform, "+0", 52, new Vector2(30, 0));
            return row;
        }

        /// <summary>Ba nút tròn nhạc/âm thanh/rung và nút đổi ngôn ngữ.</summary>
        static SettingsPanel BuildSettings(Transform parent, Vector2 pos)
        {
            var go = new GameObject("Settings", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).anchoredPosition = pos;

            Button Round(string name, string sprite, float x)
            {
                Image img = Img(name, go.transform, sprite, Center, new Vector2(x, 60));
                img.rectTransform.localScale = Vector3.one * 0.75f;
                return SoundButton(img.gameObject.AddComponent<Button>(), AudioCue.Switch);
            }

            Button music = Round("Music", "BtnMusic", -220);
            Button sound = Round("Sound", "BtnSound", 0);
            Button vibration = Round("Vibration", "BtnVibra", 220);
            Button language = TextButton("Language", go.transform, "BtnGreen", new Vector2(0, -120), "language", 30, AudioCue.Switch);
            language.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 110);

            var panel = go.AddComponent<SettingsPanel>();
            Set(panel, ("music", music), ("sound", sound), ("vibration", vibration), ("language", language),
                ("musicOn", Ui("BtnMusic")), ("musicOff", Ui("BtnMusicOff")),
                ("soundOn", Ui("BtnSound")), ("soundOff", Ui("BtnSound Off")),
                ("vibrationOn", Ui("BtnVibra")), ("vibrationOff", Ui("BtnVibra Off")));
            return panel;
        }

        static readonly Vector2 TopLeft = new Vector2(0, 1), TopRight = new Vector2(1, 1), Top = new Vector2(0.5f, 1),
            BottomLeft = Vector2.zero, BottomRight = new Vector2(1, 0), Center = new Vector2(0.5f, 0.5f);

        /// <summary>Canvas phủ màn hình, co giãn theo chiều cao 1062, kèm EventSystem dùng Input System.</summary>
        static Transform MakeCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2143, 1062);
            scaler.matchWidthOrHeight = 1;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return canvasGo.transform;
        }

        /// <summary>Vùng an toàn (tránh tai thỏ); mọi UI neo vào mép màn hình đặt trong này.</summary>
        static Transform SafeRoot(Transform canvas)
        {
            var go = new GameObject("Safe", typeof(RectTransform), typeof(SafeArea));
            go.transform.SetParent(canvas, false);
            Stretch((RectTransform)go.transform);
            return go.transform;
        }

        // ---------- Trợ giúp ----------

        static T Asset<T>(string path, Func<T> create) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            T created = create();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) throw new InvalidOperationException("Không tải được sprite (chạy Fix Sprite Imports trước): " + path);
            return s;
        }

        /// <summary>Nền đã vá (ô thùng rác cũ thành ô trống), sinh từ ảnh gốc của bộ asset vào Assets/Game/Art.</summary>
        static Sprite AreaSprite(int number) => EnsureSprite($"{Root}/Art/Area{number}.png", 4096);

        /// <summary>Đặt kiểu import sprite đơn cho một ảnh trong Assets/Game/Art rồi tải nó.</summary>
        static Sprite EnsureSprite(string path, int maxSize)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) throw new InvalidOperationException("Thiếu ảnh: " + path);
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single ||
                imp.mipmapEnabled || imp.maxTextureSize != maxSize)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = maxSize;
                imp.SaveAndReimport();
            }
            return LoadSprite(path);
        }

        static Sprite[] LoadSprites(string folder)
        {
            Sprite[] result = Directory.GetFiles(folder, "*.png").OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => LoadSprite(p.Replace('\\', '/'))).ToArray();
            if (result.Length == 0) throw new InvalidOperationException("Thư mục không có sprite: " + folder);
            return result;
        }

        static Sprite WhiteSprite()
        {
            string path = $"{Root}/Art/White.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(4, 4);
                tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp.spriteImportMode != SpriteImportMode.Single || imp.spritePixelsPerUnit != 4)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 4;
                imp.SaveAndReimport();
            }
            return LoadSprite(path);
        }

        static SpriteRenderer MakeRenderer(string name, Transform parent, Sprite sprite, Material material, int order, Color color)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        /// <summary>Gán các trường [SerializeField] private theo tên.</summary>
        static void Set(Object target, params (string name, object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach ((string name, object value) in fields)
            {
                SerializedProperty p = so.FindProperty(name);
                if (p == null) throw new InvalidOperationException($"{target.GetType().Name} không có trường '{name}'");
                switch (value)
                {
                    case AudioCue cue: p.intValue = (int)cue; break;
                    case float f: p.floatValue = f; break;
                    case string str: p.stringValue = str; break;
                    case Vector2 v: p.vector2Value = v; break;
                    case int[] ints:
                        p.arraySize = ints.Length;
                        for (int i = 0; i < ints.Length; i++) p.GetArrayElementAtIndex(i).intValue = ints[i];
                        break;
                    case float[] floats:
                        p.arraySize = floats.Length;
                        for (int i = 0; i < floats.Length; i++) p.GetArrayElementAtIndex(i).floatValue = floats[i];
                        break;
                    case Vector2[] vs:
                        p.arraySize = vs.Length;
                        for (int i = 0; i < vs.Length; i++) p.GetArrayElementAtIndex(i).vector2Value = vs[i];
                        break;
                    case Object o:
                        if (o == null) throw new InvalidOperationException($"Tham chiếu rỗng cho '{name}'");
                        p.objectReferenceValue = o;
                        break;
                    case Object[] os:
                        p.arraySize = os.Length;
                        for (int i = 0; i < os.Length; i++)
                        {
                            if (os[i] == null) throw new InvalidOperationException($"Tham chiếu rỗng trong '{name}[{i}]'");
                            p.GetArrayElementAtIndex(i).objectReferenceValue = os[i];
                        }
                        break;
                    default: throw new InvalidOperationException($"Kiểu chưa hỗ trợ cho '{name}': {value?.GetType()}");
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
