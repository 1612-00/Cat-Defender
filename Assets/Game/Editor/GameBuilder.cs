using System;
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
    /// </summary>
    public static class GameBuilder
    {
        const string Kit = "Assets/cat-defense-kit/Png";
        const string Root = "Assets/Game";
        const int CatCount = 15;
        static readonly string[] EnemyFolders = { "Enemy Reg 1", "Enemy Reg 2" };

        // Toạ độ đo từ Area1.png (2143x1062, 100 px/unit, tâm ở gốc toạ độ).
        static readonly float[] SlotRows = { 2.30f, 1.14f, 0f, -1.15f, -2.30f };
        const float SlotColLeft = -7.99f, SlotColRight = -6.60f;
        const float BarX = -3.91f, BarBottom = -1.42f, BarHeight = 4.05f, BarWidth = 0.17f;

        [MenuItem("CatDefender/1. Fix Sprite Imports")]
        public static void FixImports()
        {
            string[] folders =
            {
                Kit + "/Characters", Kit + "/Bullets", Kit + "/Area", Kit + "/Ui",
            };
            var paths = folders.Concat(EnemyFolders.Select(e => Kit + "/Enemies/" + e))
                .SelectMany(f => Directory.GetFiles(f, "*.png", SearchOption.AllDirectories))
                .Select(p => p.Replace('\\', '/')).ToArray();

            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in paths)
                {
                    var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                    int maxSize = path.Contains("/Area/") ? 4096 : 2048;
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

        [MenuItem("CatDefender/2. Build Milestone 1")]
        public static void Build()
        {
            foreach (string dir in new[] { "Art", "Data", "Data/Cats", "Data/Enemies", "Data/Levels", "Prefabs", "Scenes" })
                Directory.CreateDirectory($"{Root}/{dir}");
            AssetDatabase.Refresh();

            // Mở scene mới trước tiên: việc đổi scene giải phóng asset chưa được tham chiếu,
            // làm mọi asset tải trước đó thành null.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material unlit = Asset<Material>($"{Root}/Art/SpriteUnlit.mat",
                () => new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")));
            Sprite white = WhiteSprite();

            // --- Dữ liệu ---
            Sprite bullet = LoadSprite($"{Kit}/Bullets/Artboard 1.png");
            var cats = new CatData[CatCount];
            for (int i = 0; i < CatCount; i++)
            {
                CatData c = Asset($"{Root}/Data/Cats/Cat{i + 1:00}.asset", ScriptableObject.CreateInstance<CatData>);
                c.level = i + 1;
                c.damage = Mathf.RoundToInt(4 * Mathf.Pow(2.1f, i));
                c.fireInterval = 0.8f;
                c.bulletSpeed = 22f;
                c.bulletSprite = bullet;
                c.idle = LoadSprites($"{Kit}/Characters/C{i + 1}/Idle");
                c.shoot = LoadSprites($"{Kit}/Characters/C{i + 1}/Shoot");
                EditorUtility.SetDirty(c);
                cats[i] = c;
            }

            EnemyData reg1 = EnemyAsset(0, hp: 20, speed: 0.8f, damage: 5, reward: 4);
            EnemyData reg2 = EnemyAsset(1, hp: 45, speed: 0.7f, damage: 8, reward: 8);

            LevelData level = Asset($"{Root}/Data/Levels/Level01.asset", ScriptableObject.CreateInstance<LevelData>);
            level.waves = new System.Collections.Generic.List<LevelData.Wave>
            {
                Wave((reg1, 6, 1.5f)),
                Wave((reg1, 10, 1.2f)),
                Wave((reg1, 8, 1.0f), (reg2, 4, 1.5f)),
                Wave((reg1, 12, 0.9f), (reg2, 6, 1.2f)),
                Wave((reg2, 10, 1.0f), (reg1, 10, 0.6f)),
            };
            EditorUtility.SetDirty(level);

            // --- Prefab ---
            BuildPrefab<Cat>("Cat", unlit, 10, typeof(SpriteAnimator));
            BuildPrefab<Enemy>("Enemy", unlit, 100, typeof(SpriteAnimator));
            BuildPrefab<Bullet>("Bullet", unlit, 800);
            // Tải lại sau khi lưu hết: tham chiếu trả về từ SaveAsPrefabAsset mất hiệu lực khi asset khác được import.
            var catPrefab = AssetDatabase.LoadAssetAtPath<Cat>($"{Root}/Prefabs/Cat.prefab");
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<Enemy>($"{Root}/Prefabs/Enemy.prefab");
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<Bullet>($"{Root}/Prefabs/Bullet.prefab");

            // --- Scene ---

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.31f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            MakeRenderer("Background", null, LoadSprite($"{Kit}/Area/Area1.png"), unlit, -100, Color.white);

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

            var game = new GameObject("Game").AddComponent<GameManager>();
            var board = new GameObject("Board").AddComponent<BoardView>();

            // 9 ô đặt mèo; ô dưới cùng cột trái là thùng rác.
            var slots = SlotRows.SelectMany((y, row) => row == SlotRows.Length - 1
                ? new[] { new Vector2(SlotColRight, y) }
                : new[] { new Vector2(SlotColLeft, y), new Vector2(SlotColRight, y) }).ToArray();
            Set(board, ("game", game), ("catPrefab", catPrefab), ("catsByLevel", cats), ("slots", slots),
                ("trash", new Vector2(SlotColLeft, SlotRows[SlotRows.Length - 1])));
            Set(game, ("level", level), ("board", board), ("wall", wall),
                ("enemyPrefab", enemyPrefab), ("bulletPrefab", bulletPrefab));

            BuildHud(game);

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
            Debug.Log("[GameBuilder] Milestone 1 built: " + scenePath);
        }

        // ---------- HUD ----------

        static void BuildHud(GameManager game)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2143, 1062);
            scaler.matchWidthOrHeight = 1;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Transform root = canvasGo.transform;

            Image coinBar = Img("CoinBar", root, "CoinBar", new Vector2(0, 1), new Vector2(140, -70));
            Text coinText = Label("Coins", coinBar.transform, "0", 40, new Vector2(20, 0));
            Img("CoinIcon", coinBar.transform, "CoinIcon", new Vector2(0, 0.5f), new Vector2(-20, 0)).rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image waveBar = Img("WaveBar", root, "WaveBar", new Vector2(1, 1), new Vector2(-260, -70));
            Text waveText = Label("Wave", waveBar.transform, "", 30, new Vector2(18, 0));

            Button buy = Btn("BuyButton", root, "BtnGreen", new Vector2(0, 0), new Vector2(200, 50),
                Strings.BuyCat, out Text buyCost);
            Button repair = Btn("RepairButton", root, "BtnOrange", new Vector2(0, 0), new Vector2(570, 50),
                Strings.RepairWall, out Text repairCost);

            GameObject win = ResultPanel("WinPanel", root, "WinPopUp", out Button winRestart);
            GameObject lose = ResultPanel("LosePanel", root, "LosePopUp", out Button loseRestart);

            var hud = canvasGo.AddComponent<Hud>();
            Set(hud, ("game", game), ("coinText", coinText), ("waveText", waveText),
                ("buyCostText", buyCost), ("repairCostText", repairCost),
                ("buyButton", buy), ("repairButton", repair), ("winPanel", win), ("losePanel", lose),
                ("restartButtons", new[] { winRestart, loseRestart }));
        }

        static Sprite Ui(string name) => LoadSprite($"{Kit}/Ui/{name}.png");

        /// <summary>Ảnh kích thước gốc; anchor và pivot trùng nhau tại <paramref name="corner"/>.</summary>
        static Image Img(string name, Transform parent, string sprite, Vector2 corner, Vector2 pos)
        {
            var img = new GameObject(name, typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = Ui(sprite);
            img.SetNativeSize();
            var rt = img.rectTransform;
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
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = offset;
            return t;
        }

        static Button Btn(string name, Transform parent, string sprite, Vector2 corner, Vector2 pos,
            string title, out Text cost)
        {
            Image img = Img(name, parent, sprite, corner, pos);
            var button = img.gameObject.AddComponent<Button>();
            Label("Title", img.transform, title, 38, new Vector2(0, 22));
            cost = Label("Cost", img.transform, "", 36, new Vector2(0, -24));
            return button;
        }

        static GameObject ResultPanel(string name, Transform parent, string popup, out Button restart)
        {
            var dim = new GameObject(name, typeof(Image)).GetComponent<Image>();
            dim.transform.SetParent(parent, false);
            dim.color = new Color(0, 0, 0, 0.6f);
            var rt = dim.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            Img("Popup", dim.transform, popup, new Vector2(0.5f, 0.5f), new Vector2(0, 80));
            Image b = Img("RestartButton", dim.transform, "BtnGreen", new Vector2(0.5f, 0.5f), new Vector2(0, -300));
            restart = b.gameObject.AddComponent<Button>();
            Label("Title", b.transform, Strings.PlayAgain, 44, Vector2.zero);
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        // ---------- Trợ giúp ----------

        static EnemyData EnemyAsset(int index, int hp, float speed, int damage, int reward)
        {
            string folder = $"{Kit}/Enemies/{EnemyFolders[index]}";
            EnemyData e = Asset($"{Root}/Data/Enemies/Reg{index + 1}.asset", ScriptableObject.CreateInstance<EnemyData>);
            e.hp = hp;
            e.speed = speed;
            e.damage = damage;
            e.coinReward = reward;
            e.walk = LoadSprites(folder + "/Walk");
            e.attack = LoadSprites(folder + "/Attack");
            e.dead = LoadSprites(folder + "/Dead");
            EditorUtility.SetDirty(e);
            return e;
        }

        static LevelData.Wave Wave(params (EnemyData enemy, int count, float interval)[] groups) => new LevelData.Wave
        {
            groups = groups.Select(g => new LevelData.Group { enemy = g.enemy, count = g.count, interval = g.interval }).ToList(),
        };

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

        static void BuildPrefab<T>(string name, Material material, int sortingOrder, params Type[] extra) where T : Component
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sharedMaterial = material;
            sr.sortingOrder = sortingOrder;
            foreach (Type t in extra) go.AddComponent(t);
            go.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(go);
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
