using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatDefender
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>Điều phối một level: ví tiền, wave, hàng, quái, tường, addon, thẻ nâng cấp, thắng/thua.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] LevelData[] levels;
        [SerializeField] AddonData[] addons;
        [SerializeField] PerkData[] perkPool;
        [SerializeField] MetaConfig meta;
        [SerializeField] BoardView board;
        [SerializeField] Wall wall;
        [SerializeField] AddonRunner addonRunner;
        [SerializeField] SpriteRenderer background;
        [SerializeField] Enemy enemyPrefab;
        [SerializeField] Bullet bulletPrefab;
        [SerializeField] SpriteAnimator fxPrefab;
        [SerializeField] Sprite[] shootFx;
        [SerializeField] Sprite[] explosionFx;
        /// <summary>Toạ độ y của tâm từng hàng, từ trên xuống.</summary>
        [SerializeField] float[] laneYs;
        /// <summary>Dải cảnh báo của từng hàng, hiện trong lúc nghỉ để báo hàng sắp có quái.</summary>
        [SerializeField] SpriteRenderer[] laneWarnings;
        [SerializeField] float spawnX = 11.6f;
        [SerializeField] float wallX = -2.6f;
        [SerializeField] float targetMaxX = 10.4f;
        [SerializeField] int perkChoiceCount = 3;
        [SerializeField] float splashRadius = 1.5f;
        [SerializeField] float slowSeconds = 2f;

        // Ảnh nổ rộng 13 unit ở tỉ lệ 1; vòng lửa chiếm khoảng một nửa bề rộng ảnh.
        const float ExplosionSpriteRadius = 3.2f;

        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Enemy> splashBuffer = new List<Enemy>();
        readonly List<int> spawnBuffer = new List<int>();
        readonly List<EnemyData> enemyTypes = new List<EnemyData>();
        readonly System.Random rng = new System.Random();
        List<int> activeLanes = new List<int>();
        LevelData level;
        WaveRunner waves;
        int[] addonUsesLeft;
        int catsBought, preparedWave;

        public GameState State { get; private set; }
        public Wallet Wallet { get; private set; }
        public Perks Perks { get; private set; }
        public Wall Wall => wall;
        public float WallX => wallX;
        public IReadOnlyList<Enemy> Enemies => enemies;
        /// <summary>Số hiệu ứng đang che tường; lớn hơn 0 thì tường không mất máu.</summary>
        public int WallShields { get; set; }

        /// <summary>Các thẻ đang chờ người chơi chọn; null khi không có. Game tạm dừng trong lúc chọn.</summary>
        public PerkData[] PerkChoices { get; private set; }
        /// <summary>Người chơi bấm tạm dừng.</summary>
        public bool UserPaused { get; private set; }
        public bool Paused => PerkChoices != null || UserPaused;

        /// <summary>Coin và gem nhận được khi level kết thúc; chỉ có nghĩa sau khi thắng hoặc thua.</summary>
        public int RewardCoins { get; private set; }
        public int RewardGems { get; private set; }

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
        public bool CanUseAddon(int i) => State == GameState.Playing && !Paused && addonUsesLeft[i] > 0;

        public int LaneCount => laneYs.Length;
        public float LaneY(int lane) => laneYs[lane];

        /// <summary>Phát khi wave, trạng thái thắng/thua, giá mèo, lượt addon hoặc thẻ chờ chọn thay đổi.</summary>
        public event Action Changed;

        void Awake()
        {
            Time.timeScale = 1f;
            Session.LevelIndex = Mathf.Clamp(Session.LevelIndex, 0, levels.Length - 1);
            level = levels[Session.LevelIndex];
            if (level.background != null) background.sprite = level.background;

            // Nâng cấp vĩnh viễn từ menu cộng vào chỉ số khởi đầu của level.
            Profile.Init(meta);
            SaveData save = Profile.Data;
            Wallet = new Wallet(level.startCoins + Mathf.RoundToInt(meta.UpgradeValue(UpgradeId.StartCoins, save)));
            Perks = new Perks();
            Perks.Apply(PerkType.Damage, meta.UpgradeValue(UpgradeId.CatDamage, save));
            wall.Init(level.wallHp + Mathf.RoundToInt(meta.UpgradeValue(UpgradeId.WallHp, save)));
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

            // Sân đã sạch và đang nghỉ: chọn hàng cho wave kế tiếp, và mời chọn thẻ (trừ trước wave 1).
            if (waves.Resting && enemies.Count == 0 && preparedWave != waves.CurrentWave)
            {
                preparedWave = waves.CurrentWave;
                activeLanes = Picker.Distinct(laneYs.Length, Picker.ActiveLaneCount(preparedWave, laneYs.Length), rng);
                if (preparedWave > 1) OfferPerks();
            }
            // Chỉ báo khi đã chọn xong hàng cho wave kế tiếp (sân sạch), không báo hàng của wave cũ.
            ShowLaneWarnings(waves.Resting && preparedWave == waves.CurrentWave);

            int waveBefore = waves.CurrentWave;
            spawnBuffer.Clear();
            waves.Tick(Time.deltaTime, enemies.Count, spawnBuffer);
            for (int i = 0; i < spawnBuffer.Count; i++)
                SpawnEnemyAt(enemyTypes[spawnBuffer[i]], spawnX, RandomLane());
            if (waves.CurrentWave != waveBefore) Changed?.Invoke();

            if (waves.AllSpawned && enemies.Count == 0) End(GameState.Won);
        }

        void ShowLaneWarnings(bool resting)
        {
            float alpha = 0.10f + 0.12f * Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
            for (int i = 0; i < laneWarnings.Length; i++)
            {
                bool on = resting && activeLanes.Contains(i);
                if (laneWarnings[i].enabled != on) laneWarnings[i].enabled = on;
                if (on) laneWarnings[i].color = new Color(1f, 0.25f, 0.2f, alpha);
            }
        }

        // ---------- Hàng ----------

        /// <summary>Một hàng ngẫu nhiên trong các hàng đang có quái của wave hiện tại.</summary>
        public int RandomLane() =>
            activeLanes.Count > 0 ? activeLanes[rng.Next(activeLanes.Count)] : rng.Next(laneYs.Length);

        /// <summary>Hàng ngay trên hoặc ngay dưới, chọn ngẫu nhiên, không ra ngoài sân.</summary>
        public int AdjacentLane(int lane)
        {
            if (lane <= 0) return 1;
            if (lane >= laneYs.Length - 1) return laneYs.Length - 2;
            return lane + (rng.Next(2) == 0 ? -1 : 1);
        }

        public void StunLane(int lane, float seconds) => board.StunLane(lane, seconds);

        // ---------- Quái và đạn ----------

        public void SpawnEnemyAt(EnemyData data, float x, int lane)
        {
            var pos = new Vector3(x, laneYs[lane] + Enemy.FootOffset + UnityEngine.Random.Range(-0.12f, 0.12f), 0);
            Enemy e = Instantiate(enemyPrefab, pos, Quaternion.identity);
            e.Init(data, this, lane, level.enemyHpScale, level.rewardScale);
            enemies.Add(e);
        }

        /// <summary>
        /// Quái gần tường nhất trên hàng này, đã vào màn hình, mà đạn đang bay chưa đủ hạ.
        /// Trả về null nếu hàng trống hoặc mọi quái trên hàng đều sắp chết.
        /// </summary>
        public Enemy FindTarget(int lane) => Front(lane, true);

        /// <summary>Quái gần tường nhất trên mọi hàng, kể cả con sắp chết; null nếu không có.</summary>
        public Enemy FrontEnemy() => Front(-1, false);

        Enemy Front(int lane, bool skipDoomed)
        {
            Enemy best = null;
            float bestX = targetMaxX;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (lane >= 0 && e.Lane != lane) continue;
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
            b.Init(this, target, Perks.Damage(cat.damage), cat.bulletSpeed, cat.bulletSprite);
            SpawnFx(shootFx, from + new Vector3(0.35f, 0, 0), 0.45f, 850);
        }

        /// <summary>Đạn trúng đích: gây sát thương, rồi áp hiệu ứng lan và làm chậm từ thẻ nâng cấp.</summary>
        public void OnBulletHit(Enemy target, int damage)
        {
            Vector3 at = target.transform.position;
            if (Perks.SlowFraction > 0) target.ApplySlow(Perks.SlowFraction, slowSeconds);
            target.TakeDamage(damage);

            if (Perks.SplashFraction <= 0) return;
            int splash = Mathf.Max(1, Mathf.RoundToInt(damage * Perks.SplashFraction));
            // Sao chép trước: TakeDamage có thể xoá quái khỏi danh sách đang duyệt.
            splashBuffer.Clear();
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != target && (enemies[i].transform.position - at).sqrMagnitude <= splashRadius * splashRadius)
                    splashBuffer.Add(enemies[i]);
            for (int i = 0; i < splashBuffer.Count; i++) splashBuffer[i].TakeDamage(splash);
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
            Wallet.Add(Perks.Coins(reward));
        }

        public void DamageWall(int amount)
        {
            if (WallShields > 0) return;
            wall.Damage(amount);
            if (wall.Hp <= 0) End(GameState.Lost);
        }

        // ---------- Hành động của người chơi ----------

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
            addonRunner.Run(addons[i], meta.AddonScale(Profile.Data.addonLevels[i]));
            Changed?.Invoke();
        }

        // ---------- Thẻ nâng cấp ----------

        void OfferPerks()
        {
            var pool = new List<PerkData>();
            foreach (PerkData p in perkPool)
                if (p.type != PerkType.FreeCat || board.HasEmptySlot) pool.Add(p);
            if (pool.Count == 0) return;

            List<int> picks = Picker.Distinct(pool.Count, Mathf.Min(perkChoiceCount, pool.Count), rng);
            PerkChoices = new PerkData[picks.Count];
            for (int i = 0; i < picks.Count; i++) PerkChoices[i] = pool[picks[i]];
            UpdateTimeScale();
            Changed?.Invoke();
        }

        public void ChoosePerk(int index)
        {
            if (PerkChoices == null || index < 0 || index >= PerkChoices.Length) return;
            PerkData perk = PerkChoices[index];
            PerkChoices = null;
            UpdateTimeScale();

            switch (perk.type)
            {
                case PerkType.Wall:
                    wall.AddMax(Mathf.RoundToInt(perk.value));
                    break;
                case PerkType.AddonRefill:
                    for (int i = 0; i < addonUsesLeft.Length; i++) addonUsesLeft[i] += Mathf.RoundToInt(perk.value);
                    break;
                case PerkType.FreeCat:
                    board.AddCat(Mathf.RoundToInt(perk.value));
                    break;
                default:
                    Perks.Apply(perk.type, perk.value);
                    break;
            }
            Changed?.Invoke();
        }

        // ---------- Level ----------

        public void SetPaused(bool paused)
        {
            if (State != GameState.Playing) return;
            UserPaused = paused;
            UpdateTimeScale();
            Changed?.Invoke();
        }

        void UpdateTimeScale() => Time.timeScale = Paused ? 0f : 1f;

        public void Restart() => LoadLevel(Session.LevelIndex);

        public void LoadLevel(int index)
        {
            Session.LevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
            Time.timeScale = 1f;
            SceneManager.LoadScene("Gameplay");
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }

        void End(GameState result)
        {
            if (State != GameState.Playing) return;
            State = result;
            UserPaused = false;
            Time.timeScale = 1f;
            ShowLaneWarnings(false);
            GrantRewards(result == GameState.Won);
            Changed?.Invoke();
        }

        /// <summary>Cộng thưởng vào tiến trình và lưu. Thắng lần đầu mở level kế tiếp và tặng gem.</summary>
        void GrantRewards(bool won)
        {
            SaveData save = Profile.Data;
            int winCoins = MetaRules.WinCoins(meta.winBaseCoins, meta.winCoinsPerLevel, LevelNumber);
            RewardCoins = won ? winCoins : MetaRules.LoseCoins(winCoins, meta.loseFraction, waves.CurrentWave - 1, waves.WaveCount);
            RewardGems = 0;

            if (won && Session.LevelIndex == save.unlockedLevel)
            {
                save.unlockedLevel = Session.LevelIndex + 1;
                RewardGems = HasBoss(level) ? meta.bossFirstClearGems : meta.firstClearGems;
            }

            save.coins += RewardCoins;
            save.gems += RewardGems;
            Profile.Save();
        }

        static bool HasBoss(LevelData data)
        {
            foreach (LevelData.Wave wave in data.waves)
                foreach (LevelData.Group g in wave.groups)
                    if (g.enemy.isBoss) return true;
            return false;
        }
    }
}
