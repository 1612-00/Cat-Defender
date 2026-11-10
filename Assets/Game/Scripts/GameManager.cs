using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatDefender
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>Điều phối một level: ví tiền, wave, quái, tường, điều kiện thắng/thua.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] LevelData level;
        [SerializeField] BoardView board;
        [SerializeField] Wall wall;
        [SerializeField] Enemy enemyPrefab;
        [SerializeField] Bullet bulletPrefab;
        [SerializeField] float spawnX = 11.6f;
        [SerializeField] float spawnYMin = -2.4f;
        [SerializeField] float spawnYMax = 2.6f;
        [SerializeField] float wallX = -2.6f;
        [SerializeField] float targetMaxX = 10.4f;

        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<int> spawnBuffer = new List<int>();
        readonly List<EnemyData> enemyTypes = new List<EnemyData>();
        WaveRunner waves;
        int catsBought;

        public GameState State { get; private set; }
        public Wallet Wallet { get; private set; }
        public Wall Wall => wall;
        public float WallX => wallX;
        public int CurrentWave => waves.CurrentWave;
        public int WaveCount => waves.WaveCount;
        public int CatCost => Economy.CatCost(level.catBaseCost, level.catCostGrowth, catsBought);
        public int RepairCost => level.repairCost;
        public bool CanBuyCat => State == GameState.Playing && board.HasEmptySlot && Wallet.Coins >= CatCost;
        public bool CanRepair => State == GameState.Playing && wall.Hp < wall.MaxHp && Wallet.Coins >= RepairCost;

        /// <summary>Phát khi wave, trạng thái thắng/thua, hoặc giá mèo thay đổi.</summary>
        public event Action Changed;

        void Awake()
        {
            Wallet = new Wallet(level.startCoins);
            wall.Init(level.wallHp);
            waves = new WaveRunner(BuildWaves(), level.waveDelay, level.firstWaveDelay);
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
            e.Init(data, this);
            enemies.Add(e);
        }

        /// <summary>
        /// Quái gần tường nhất đã vào màn hình mà đạn đang bay chưa đủ hạ.
        /// Nếu mọi quái đều sắp chết thì không bắn (trả về null) để khỏi lãng phí đạn.
        /// </summary>
        public Enemy FindTarget()
        {
            Enemy best = null;
            float bestX = targetMaxX;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                float x = e.transform.position.x;
                if (x >= bestX || e.Doomed) continue;
                bestX = x;
                best = e;
            }
            return best;
        }

        public void SpawnBullet(Vector3 from, Enemy target, CatData cat)
        {
            Bullet b = Instantiate(bulletPrefab, from, Quaternion.identity);
            b.Init(target, cat.damage, cat.bulletSpeed, cat.bulletSprite);
        }

        public void OnEnemyKilled(Enemy enemy, int reward)
        {
            enemies.Remove(enemy);
            Wallet.Add(reward);
        }

        public void DamageWall(int amount)
        {
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

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        void End(GameState result)
        {
            if (State != GameState.Playing) return;
            State = result;
            Changed?.Invoke();
        }
    }
}
