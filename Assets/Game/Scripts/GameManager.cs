using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatDefender
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>Điều phối một level: ví tiền, wave, quái, tường, addon, điều kiện thắng/thua.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] LevelData[] levels;
        [SerializeField] AddonData[] addons;
        [SerializeField] BoardView board;
        [SerializeField] Wall wall;
        [SerializeField] AddonRunner addonRunner;
        [SerializeField] SpriteRenderer background;
        [SerializeField] Enemy enemyPrefab;
        [SerializeField] Bullet bulletPrefab;
        [SerializeField] SpriteAnimator fxPrefab;
        [SerializeField] Sprite[] shootFx;
        [SerializeField] Sprite[] explosionFx;
        [SerializeField] float spawnX = 11.6f;
        [SerializeField] float spawnYMin = -2.4f;
        [SerializeField] float spawnYMax = 2.6f;
        [SerializeField] float wallX = -2.6f;
        [SerializeField] float targetMaxX = 10.4f;

        // Ảnh nổ rộng 13 unit ở tỉ lệ 1; vòng lửa chiếm khoảng một nửa bề rộng ảnh.
        const float ExplosionSpriteRadius = 3.2f;

        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<int> spawnBuffer = new List<int>();
        readonly List<EnemyData> enemyTypes = new List<EnemyData>();
        LevelData level;
        WaveRunner waves;
        int[] addonUsesLeft;
        int catsBought;

        public GameState State { get; private set; }
        public Wallet Wallet { get; private set; }
        public Wall Wall => wall;
        public float WallX => wallX;
        public IReadOnlyList<Enemy> Enemies => enemies;
        /// <summary>Số hiệu ứng đang che tường; lớn hơn 0 thì tường không mất máu.</summary>
        public int WallShields { get; set; }

        public int LevelNumber => Session.LevelIndex + 1;
        public int LevelCount => levels.Length;
        public bool HasNextLevel => Session.LevelIndex + 1 < levels.Length;
        public int CurrentWave => waves.CurrentWave;
        public int WaveCount => waves.WaveCount;
        public int CatCost => Economy.CatCost(level.catBaseCost, level.catCostGrowth, catsBought);
        public int RepairCost => level.repairCost;
        public bool CanBuyCat => State == GameState.Playing && board.HasEmptySlot && Wallet.Coins >= CatCost;
        public bool CanRepair => State == GameState.Playing && wall.Hp < wall.MaxHp && Wallet.Coins >= RepairCost;

        public int AddonCount => addons.Length;
        public AddonData Addon(int i) => addons[i];
        public int AddonUsesLeft(int i) => addonUsesLeft[i];
        public bool CanUseAddon(int i) => State == GameState.Playing && addonUsesLeft[i] > 0;

        /// <summary>Phát khi wave, trạng thái thắng/thua, giá mèo hoặc lượt addon thay đổi.</summary>
        public event Action Changed;

        void Awake()
        {
            Session.LevelIndex = Mathf.Clamp(Session.LevelIndex, 0, levels.Length - 1);
            level = levels[Session.LevelIndex];
            if (level.background != null) background.sprite = level.background;

            Wallet = new Wallet(level.startCoins);
            wall.Init(level.wallHp);
            waves = new WaveRunner(BuildWaves(), level.waveDelay, level.firstWaveDelay);
            addonUsesLeft = new int[addons.Length];
            for (int i = 0; i < addons.Length; i++) addonUsesLeft[i] = addons[i].uses;
        }

        List<IReadOnlyList<SpawnGroup>> BuildWaves()
        {
            var result = new List<IReadOnlyList<SpawnGroup>>();
            foreach (LevelData.Wave wave in level.waves)
            {
                var groups = new List<SpawnGroup>();
                foreach (LevelData.Group g in wave.groups)
                {
                    int id = enemyTypes.IndexOf(g.enemy);
                    if (id < 0)
                    {
                        id = enemyTypes.Count;
                        enemyTypes.Add(g.enemy);
                    }
                    groups.Add(new SpawnGroup { enemyId = id, count = g.count, interval = g.interval });
                }
                result.Add(groups);
            }
            return result;
        }

        void Update()
        {
            if (State != GameState.Playing) return;

            int waveBefore = waves.CurrentWave;
            spawnBuffer.Clear();
            waves.Tick(Time.deltaTime, enemies.Count, spawnBuffer);
            for (int i = 0; i < spawnBuffer.Count; i++) SpawnEnemy(enemyTypes[spawnBuffer[i]]);
            if (waves.CurrentWave != waveBefore) Changed?.Invoke();

            if (waves.AllSpawned && enemies.Count == 0) End(GameState.Won);
        }

        void SpawnEnemy(EnemyData data)
        {
            var pos = new Vector3(spawnX, UnityEngine.Random.Range(spawnYMin, spawnYMax), 0);
            Enemy e = Instantiate(enemyPrefab, pos, Quaternion.identity);
            e.Init(data, this, level.enemyHpScale, level.rewardScale);
            enemies.Add(e);
        }

        /// <summary>
        /// Quái gần tường nhất đã vào màn hình mà đạn đang bay chưa đủ hạ.
        /// Nếu mọi quái đều sắp chết thì không bắn (trả về null) để khỏi lãng phí đạn.
        /// </summary>
        public Enemy FindTarget() => Front(true);

        /// <summary>Quái gần tường nhất đã vào màn hình, kể cả con sắp chết; null nếu không có.</summary>
        public Enemy FrontEnemy() => Front(false);

        Enemy Front(bool skipDoomed)
        {
            Enemy best = null;
            float bestX = targetMaxX;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                float x = e.transform.position.x;
                if (x >= bestX || (skipDoomed && e.Doomed)) continue;
                bestX = x;
                best = e;
            }
            return best;
        }

        public void SpawnBullet(Vector3 from, Enemy target, CatData cat)
        {
            Bullet b = Instantiate(bulletPrefab, from, Quaternion.identity);
            b.Init(target, cat.damage, cat.bulletSpeed, cat.bulletSprite);
            SpawnFx(shootFx, from + new Vector3(0.35f, 0, 0), 0.45f, 850);
        }

        public void SpawnExplosion(Vector3 at, float radius) =>
            SpawnFx(explosionFx, at, radius / ExplosionSpriteRadius, 860);

        void SpawnFx(Sprite[] frames, Vector3 at, float scale, int sortingOrder)
        {
            SpriteAnimator fx = Instantiate(fxPrefab, at, Quaternion.identity);
            fx.transform.localScale = Vector3.one * scale;
            fx.Renderer.sortingOrder = sortingOrder;
            fx.Play(frames, false, () => Destroy(fx.gameObject));
        }

        public void OnEnemyKilled(Enemy enemy, int reward)
        {
            enemies.Remove(enemy);
            Wallet.Add(reward);
        }

        public void DamageWall(int amount)
        {
            if (WallShields > 0) return;
            wall.Damage(amount);
            if (wall.Hp <= 0) End(GameState.Lost);
        }

        public void BuyCat()
        {
            if (!CanBuyCat || !Wallet.TrySpend(CatCost)) return;
            board.AddCat(1);
            catsBought++;
            Changed?.Invoke();
        }

        public void RepairWall()
        {
            if (!CanRepair || !Wallet.TrySpend(RepairCost)) return;
            wall.Repair(level.repairAmount);
        }

        public void UseAddon(int i)
        {
            if (!CanUseAddon(i)) return;
            addonUsesLeft[i]--;
            addonRunner.Run(addons[i]);
            Changed?.Invoke();
        }

        public void Restart() => LoadLevel(Session.LevelIndex);

        public void LoadLevel(int index)
        {
            Session.LevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void End(GameState result)
        {
            if (State != GameState.Playing) return;
            State = result;
            Changed?.Invoke();
        }
    }
}
