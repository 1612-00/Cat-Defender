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
    public static class GameBuilder
    {
        public const string Kit = "Assets/cat-defense-kit/Png";
        public const string Root = "Assets/Game";
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

        [MenuItem("CatDefender/2. Build Game")]
        public static void Build()
        {
            foreach (string dir in new[] { "Art", "Data", "Data/Cats", "Data/Enemies", "Data/Levels", "Data/Addons", "Data/Perks", "Prefabs", "Scenes" })
                Directory.CreateDirectory($"{Root}/{dir}");
            AssetDatabase.Refresh();

            // Mở scene mới trước tiên: việc đổi scene giải phóng asset chưa được tham chiếu,
            // làm mọi asset tải trước đó thành null.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material unlit = Asset<Material>($"{Root}/Art/SpriteUnlit.mat",
                () => new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")));
            Sprite white = WhiteSprite();

            CatData[] cats = BuildCats();
            EnemyData[] regs = Enumerable.Range(0, RegCount).Select(BuildReg).ToArray();
            SetBehaviours(regs);
            EnemyData[] bosses = Enumerable.Range(0, BossCount).Select(i => BuildBoss(i, regs)).ToArray();
            AddonData[] addons = BuildAddons();
            PerkData[] perks = BuildPerks();
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

            SpriteRenderer background = MakeRenderer("Background", null, LoadSprite($"{Kit}/Area/Area1.png"), unlit, -100, Color.white);

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

            // 9 ô đặt mèo; ô dưới cùng cột trái là thùng rác.
            Vector2[] slots = SlotRows.SelectMany((y, row) => row == SlotRows.Length - 1
                ? new[] { new Vector2(SlotColRight, y) }
                : new[] { new Vector2(SlotColLeft, y), new Vector2(SlotColRight, y) }).ToArray();
            int[] slotLanes = slots.Select(s => Array.IndexOf(SlotRows, s.y)).ToArray();
            Set(board, ("game", game), ("catPrefab", catPrefab), ("catsByLevel", cats), ("slots", slots),
                ("slotLanes", slotLanes), ("trash", new Vector2(SlotColLeft, SlotRows[SlotRows.Length - 1])));

            // Dải đỏ mờ trên từng hàng, bật trong lúc nghỉ để báo hàng sắp có quái.
            const float warnLeft = -2.4f, warnRight = 10.72f;
            var warningsRoot = new GameObject("LaneWarnings").transform;
            SpriteRenderer[] warnings = SlotRows.Select((y, i) =>
            {
                SpriteRenderer w = MakeRenderer("Lane" + i, warningsRoot, white, unlit, -20, new Color(1f, 0.25f, 0.2f, 0.15f));
                w.transform.position = new Vector3((warnLeft + warnRight) * 0.5f, y, 0);
                w.transform.localScale = new Vector3(warnRight - warnLeft, 1.0f, 1);
                w.enabled = false;
                return w;
            }).ToArray();
            Set(runner, ("game", game), ("fxPrefab", fxPrefab));
            Set(game, ("levels", levels), ("addons", addons), ("perkPool", perks), ("board", board), ("wall", wall),
                ("laneYs", SlotRows), ("laneWarnings", warnings),
                ("addonRunner", runner), ("background", background),
                ("enemyPrefab", enemyPrefab), ("bulletPrefab", bulletPrefab), ("fxPrefab", fxPrefab),
                ("shootFx", LoadSprites($"{Kit}/ShootFx")), ("explosionFx", LoadSprites($"{Kit}/Explosion")));

            BuildHud(game, addons);

            string scenePath = $"{Root}/Scenes/Gameplay.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

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
            BuildPerk("FireRate", PerkType.FireRate, "AddonIcon1", "Rapid Fire", "Cats shoot 20% faster", 0.2f),
            BuildPerk("Damage", PerkType.Damage, "Up3", "Sharp Claws", "+25% bullet damage", 0.25f),
            BuildPerk("Splash", PerkType.Splash, "AddonIcon3", "Blast Rounds", "Bullets splash 30% damage nearby", 0.3f),
            BuildPerk("Coins", PerkType.Coins, "AddonIcon2", "Lucky Coins", "+30% coins from kills", 0.3f),
            BuildPerk("Slow", PerkType.Slow, "AddonIcon4", "Sticky Shots", "Bullets slow enemies by 20%", 0.2f),
            BuildPerk("Wall", PerkType.Wall, "WallIcon", "Thick Wall", "+40 max wall HP", 40f),
            BuildPerk("AddonRefill", PerkType.AddonRefill, "Icon_Addon", "Restock", "+1 use of every addon", 1f),
            BuildPerk("FreeCat", PerkType.FreeCat, "Icon_Cat", "Stray Cat", "A free level 2 cat joins", 2f),
        };

        static PerkData BuildPerk(string name, PerkType type, string icon, string title, string description, float value)
        {
            PerkData p = Asset($"{Root}/Data/Perks/{name}.asset", ScriptableObject.CreateInstance<PerkData>);
            p.type = type;
            p.icon = Ui(icon);
            p.title = title;
            p.description = description;
            p.value = value;
            EditorUtility.SetDirty(p);
            return p;
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
            level.background = LoadSprite($"{Kit}/Area/Area{chapter + 1}.png");
            level.enemyHpScale = Mathf.Pow(1.10f, number - 1);
            level.rewardScale = Mathf.Pow(1.08f, number - 1);
            level.startCoins = Mathf.RoundToInt(30 * Mathf.Pow(1.08f, number - 1));
            level.wallHp = 150;
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

        static void BuildHud(GameManager game, AddonData[] addons)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2143, 1062);
            scaler.matchWidthOrHeight = 1;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Transform root = canvasGo.transform;
            Vector2 topLeft = new Vector2(0, 1), topRight = new Vector2(1, 1), top = new Vector2(0.5f, 1),
                bottomLeft = Vector2.zero, bottomRight = new Vector2(1, 0), center = new Vector2(0.5f, 0.5f);

            Image coinBar = Img("CoinBar", root, "CoinBar", topLeft, new Vector2(140, -70));
            Text coinText = Label("Coins", coinBar.transform, "0", 40, new Vector2(20, 0));
            Img("CoinIcon", coinBar.transform, "CoinIcon", new Vector2(0, 0.5f), new Vector2(-20, 0))
                .rectTransform.pivot = center;

            Image waveBar = Img("WaveBar", root, "WaveBar", topRight, new Vector2(-260, -70));
            Text waveText = Label("Wave", waveBar.transform, "", 30, new Vector2(18, 0));

            // Chọn level tạm thời để chơi thử; mốc 3 thay bằng bản đồ level.
            Image levelBox = Img("LevelBox", root, "AddonBoxNumber", top, new Vector2(0, -70));
            levelBox.rectTransform.localScale = Vector3.one * 1.4f;
            Text levelText = Label("Level", levelBox.transform, "", 30, Vector2.zero);
            Button prev = SmallButton("PrevLevel", root, top, new Vector2(-170, -72), "<");
            Button skip = SmallButton("SkipLevel", root, top, new Vector2(170, -72), ">");

            Button buy = Btn("BuyButton", root, "BtnGreen", bottomLeft, new Vector2(200, 50),
                Strings.BuyCat, out Text buyCost);
            Button repair = Btn("RepairButton", root, "BtnOrange", bottomLeft, new Vector2(570, 50),
                Strings.RepairWall, out Text repairCost);

            var addonButtons = new Button[addons.Length];
            var addonCounts = new Text[addons.Length];
            for (int i = 0; i < addons.Length; i++)
            {
                Image box = Img("Addon" + addons[i].name, root, "YellowBox", bottomRight,
                    new Vector2(-90 - (addons.Length - 1 - i) * 175, 40));
                addonButtons[i] = box.gameObject.AddComponent<Button>();
                var icon = new GameObject("Icon", typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(box.transform, false);
                icon.sprite = addons[i].icon;
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(110, 110);
                icon.rectTransform.anchoredPosition = new Vector2(0, 8);
                addonCounts[i] = Label("Count", box.transform, "", 30, new Vector2(40, -50));
            }

            GameObject win = ResultPanel("WinPanel", root, "WinPopUp", out Transform winRoot);
            Button next = TextButton("NextLevelButton", winRoot, "BtnGreen", new Vector2(-190, -300), Strings.NextLevel);
            Button winRestart = TextButton("RestartButton", winRoot, "BtnOrange", new Vector2(190, -300), Strings.PlayAgain);
            GameObject lose = ResultPanel("LosePanel", root, "LosePopUp", out Transform loseRoot);
            Button loseRestart = TextButton("RestartButton", loseRoot, "BtnGreen", new Vector2(0, -300), Strings.PlayAgain);

            // Bảng chọn 1 trong 3 thẻ nâng cấp sau mỗi wave.
            GameObject perkPanel = ResultPanel("PerkPanel", root, "TitleRibbon", out Transform perkRoot);
            RectTransform ribbon = (RectTransform)perkRoot.Find("Popup");
            ribbon.anchoredPosition = new Vector2(0, 380);
            Label("Title", ribbon, Strings.ChoosePerk, 44, new Vector2(0, 14));
            const int cardCount = 3;
            var perkButtons = new Button[cardCount];
            var perkIcons = new Image[cardCount];
            var perkTitles = new Text[cardCount];
            var perkDescriptions = new Text[cardCount];
            for (int i = 0; i < cardCount; i++)
            {
                Image card = Img("Card" + i, perkRoot, i == 1 ? "OrangeShopBox" : "BlueShopBox", center,
                    new Vector2((i - 1) * 400, -40));
                card.rectTransform.localScale = Vector3.one * 1.5f;
                perkButtons[i] = card.gameObject.AddComponent<Button>();
                perkTitles[i] = Label("Title", card.transform, "", 26, new Vector2(0, 148));
                perkIcons[i] = new GameObject("Icon", typeof(Image)).GetComponent<Image>();
                perkIcons[i].transform.SetParent(card.transform, false);
                perkIcons[i].raycastTarget = false;
                perkIcons[i].preserveAspect = true;
                perkIcons[i].rectTransform.sizeDelta = new Vector2(110, 110);
                perkIcons[i].rectTransform.anchoredPosition = new Vector2(0, 40);
                Text d = Label("Description", card.transform, "", 22, Vector2.zero);
                d.horizontalOverflow = HorizontalWrapMode.Wrap;
                d.rectTransform.offsetMin = new Vector2(16, 20);
                d.rectTransform.offsetMax = new Vector2(-16, -220);
                perkDescriptions[i] = d;
            }

            var hud = canvasGo.AddComponent<Hud>();
            Set(hud, ("game", game), ("coinText", coinText), ("waveText", waveText), ("levelText", levelText),
                ("buyCostText", buyCost), ("repairCostText", repairCost),
                ("buyButton", buy), ("repairButton", repair), ("prevLevelButton", prev), ("skipLevelButton", skip),
                ("addonButtons", addonButtons), ("addonCountTexts", addonCounts),
                ("winPanel", win), ("losePanel", lose), ("nextLevelButton", next),
                ("restartButtons", new[] { winRestart, loseRestart }),
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

        /// <summary>Chữ phủ kín phần tử cha, căn giữa, có viền tối.</summary>
        static Text Label(string name, Transform parent, string text, int size, Vector2 offset)
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
            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = offset;
            return t;
        }

        /// <summary>Nút có tiêu đề ở trên và giá ở dưới.</summary>
        static Button Btn(string name, Transform parent, string sprite, Vector2 corner, Vector2 pos,
            string title, out Text cost)
        {
            Image img = Img(name, parent, sprite, corner, pos);
            Label("Title", img.transform, title, 38, new Vector2(0, 22));
            cost = Label("Cost", img.transform, "", 36, new Vector2(0, -24));
            return img.gameObject.AddComponent<Button>();
        }

        static Button TextButton(string name, Transform parent, string sprite, Vector2 pos, string title)
        {
            Image img = Img(name, parent, sprite, new Vector2(0.5f, 0.5f), pos);
            Label("Title", img.transform, title, 44, Vector2.zero);
            return img.gameObject.AddComponent<Button>();
        }

        static Button SmallButton(string name, Transform parent, Vector2 corner, Vector2 pos, string title)
        {
            Image img = Img(name, parent, "YellowBox", corner, pos);
            img.rectTransform.localScale = Vector3.one * 0.5f;
            Label("Title", img.transform, title, 80, Vector2.zero);
            return img.gameObject.AddComponent<Button>();
        }

        /// <summary>Lớp phủ tối toàn màn hình kèm ảnh popup; <paramref name="content"/> là nơi gắn nút.</summary>
        static GameObject ResultPanel(string name, Transform parent, string popup, out Transform content)
        {
            var dim = new GameObject(name, typeof(Image)).GetComponent<Image>();
            dim.transform.SetParent(parent, false);
            dim.color = new Color(0, 0, 0, 0.6f);
            RectTransform rt = dim.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Img("Popup", dim.transform, popup, new Vector2(0.5f, 0.5f), new Vector2(0, 80));
            content = dim.transform;
            dim.gameObject.SetActive(false);
            return dim.gameObject;
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
                    case float f: p.floatValue = f; break;
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
