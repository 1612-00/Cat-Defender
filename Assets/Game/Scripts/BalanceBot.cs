using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatDefender
{
    /// <summary>Cách bot đặt mèo: Careless rải ngẫu nhiên như người chơi ẩu; Sensible dồn mèo mạnh vào hàng có cảnh báo.</summary>
    public enum BotPolicy { Careless, Sensible }

    /// <summary>
    /// Công cụ gỡ lỗi (D.1 trong gameplay-depth-plan): bot chơi ẩu để đo độ khó. Chỉ chạy khi gọi <see cref="Run"/> trong Play mode.
    /// Tiến trình lưu thật được sao lưu rồi khôi phục khi xong hoặc khi bị ngắt.
    /// </summary>
    public sealed class BalanceBot : MonoBehaviour
    {
        const float TickSeconds = 0.25f, TimeoutSeconds = 600f;
        const string BackupPath = "Library/balance_bot_save_backup.json";

        static BalanceBot instance;

        struct Result { public string outcome; public float wall, seconds; public int wave; }

        int[] levels;
        int runs;
        float speed;
        bool hadSave, restored, prevBackground;
        string originalSave;
        GameManager game;
        BotPolicy policy;
        readonly System.Random rng = new System.Random();

        /// <summary>Chạy bot kiểu Careless (xem bản có policy).</summary>
        public static void Run(int[] levelIndices, int runsPerLevel, float speed) =>
            Run(levelIndices, runsPerLevel, speed, BotPolicy.Careless);

        /// <summary>Chạy bot trên các level (chỉ số 0), mỗi level runsPerLevel lượt, ở tốc độ speed, theo chính sách chơi policy.</summary>
        public static void Run(int[] levelIndices, int runsPerLevel, float speed, BotPolicy policy)
        {
            if (instance != null) { Debug.LogError("BalanceBot is already running."); return; }
            if (!Application.isPlaying) { Debug.LogError("BalanceBot needs Play mode."); return; }
            var go = new GameObject("BalanceBot");
            DontDestroyOnLoad(go);
            BalanceBot bot = go.AddComponent<BalanceBot>();
            bot.levels = levelIndices;
            bot.runs = runsPerLevel;
            bot.speed = speed;
            bot.policy = policy;
            instance = bot;
            bot.StartCoroutine(bot.Main());
        }

        /// <summary>
        /// Tìm hai ô khác nhau cùng cấp, cấp &gt; 0 và &lt; maxLevel. Trả về false nếu không có.
        /// </summary>
        public static bool FindMergePair(int[] slotLevels, int maxLevel, out int a, out int b)
        {
            for (int i = 0; i < slotLevels.Length; i++)
            {
                if (slotLevels[i] <= 0 || slotLevels[i] >= maxLevel) continue;
                for (int j = i + 1; j < slotLevels.Length; j++)
                    if (slotLevels[j] == slotLevels[i]) { a = i; b = j; return true; }
            }
            a = b = -1;
            return false;
        }

        void Update()
        {
            if (game != null && !game.Paused) Time.timeScale = speed;
        }

        IEnumerator Main()
        {
            prevBackground = Application.runInBackground;
            Application.runInBackground = true;
            originalSave = PlayerPrefs.GetString(Profile.Key, "");
            hadSave = PlayerPrefs.HasKey(Profile.Key);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
                File.WriteAllText(BackupPath, originalSave);
            }
            catch (Exception e) { Debug.LogError("BalanceBot backup failed, aborting: " + e.Message); Finish(); yield break; }

            var all = new List<Result>[levels.Length];
            for (int l = 0; l < levels.Length; l++)
            {
                all[l] = new List<Result>();
                for (int r = 0; r < runs; r++)
                {
                    Profile.ResetAll();
                    Session.LevelIndex = levels[l];
                    game = null;
                    SceneManager.LoadScene("Gameplay");
                    yield return null;
                    yield return null;
                    game = FindFirstObjectByType<GameManager>();
                    BoardView board = FindFirstObjectByType<BoardView>();
                    if (game == null || board == null) { Debug.LogError("BalanceBot: Gameplay scene has no GameManager/BoardView."); Finish(); yield break; }

                    float seconds = 0, tick = 0;
                    while (game.State == GameState.Playing && seconds < TimeoutSeconds)
                    {
                        if (game.PerkChoices != null) game.ChoosePerk(0);
                        seconds += Time.deltaTime;
                        tick += Time.deltaTime;
                        if (tick >= TickSeconds) { tick = 0; Act(board); }
                        yield return null;
                    }
                    var res = new Result
                    {
                        outcome = game.State == GameState.Playing ? "Timeout" : game.State.ToString(),
                        wall = Mathf.Max(0, game.Wall.Hp) * 100f / game.Wall.MaxHp,
                        wave = game.CurrentWave,
                        seconds = seconds,
                    };
                    all[l].Add(res);
                    Debug.Log($"BALANCE policy={policy} level={levels[l] + 1} run={r + 1} result={res.outcome} wall={res.wall:0}% wave={res.wave}/{game.WaveCount} seconds={res.seconds:0}");
                }
            }
            WriteReport(all);
            Finish();
            Debug.Log("BALANCE DONE");
        }

        int[] Levels(BoardView board)
        {
            var lv = new int[board.SlotCount];
            for (int i = 0; i < lv.Length; i++) lv[i] = board.LevelAt(i);
            return lv;
        }

        void MoveCat(BoardView board, int from, int to)
        {
            board.Press(board.SlotWorld(from));
            board.Release(board.SlotWorld(to));
        }

        /// <summary>Mỗi nhịp: ghép nếu có cặp; không thì mua. Careless rải mèo mới ngẫu nhiên; Sensible sắp xếp dần (tối đa một nước/nhịp).</summary>
        void Act(BoardView board)
        {
            int[] lv = Levels(board);
            // Ghép làm mất một con mèo, tức mất một hàng được che. Bot hợp lý chỉ ghép khi sau đó vẫn đủ mèo cho
            // mọi hàng có quái; bot ẩu giữ ít nhất 3 con.
            int catCount = 0;
            foreach (int level in lv) if (level > 0) catCount++;
            int keep = policy == BotPolicy.Sensible ? game.ActiveLanes.Count : 3;
            if (catCount > keep && FindMergePair(lv, board.MaxLevel, out int a, out int b)) { MoveCat(board, a, b); return; }
            if (game.CanBuyCat)
            {
                game.BuyCat();
                if (policy == BotPolicy.Careless)
                {
                    int[] after = Levels(board);
                    int from = -1;
                    var empties = new List<int>();
                    for (int i = 0; i < after.Length; i++)
                    {
                        if (lv[i] == 0 && after[i] > 0) from = i;
                        if (after[i] == 0) empties.Add(i);
                    }
                    if (from >= 0 && empties.Count > 0) MoveCat(board, from, empties[rng.Next(empties.Count)]);
                    return;
                }
                lv = Levels(board);
            }
            if (policy != BotPolicy.Sensible) return;
            var lanes = new int[lv.Length];
            for (int i = 0; i < lanes.Length; i++) lanes[i] = board.SlotLane(i);
            if (FindRearrangeMove(lv, lanes, game.ActiveLanes, out int f, out int t)) MoveCat(board, f, t);
        }

        /// <summary>
        /// Một nước sắp xếp hướng tới: mèo mạnh nhất đứng trên hàng có quái, dàn đều giữa các hàng đó.
        /// Mèo mạnh nhất ở hàng không có quái (hoặc chồng lên hàng đã đông hơn hàng khác từ 2 mèo) được chuyển tới
        /// hàng đang ít mèo nhất: vào ô trống, hoặc đổi chỗ với mèo yếu hơn (không bao giờ đổi với mèo cùng cấp để khỏi ghép).
        /// Mỗi nước tăng (tổng cấp trên hàng có quái, rồi mức đều), nên lặp lại chắc chắn dừng.
        /// </summary>
        public static bool FindRearrangeMove(int[] slotLevels, int[] slotLanes, IReadOnlyList<int> activeLanes, out int from, out int to)
        {
            from = to = -1;
            if (activeLanes == null || activeLanes.Count == 0) return false;
            var counts = new Dictionary<int, int>();
            foreach (int l in activeLanes) counts[l] = 0;
            var cats = new List<int>();
            for (int i = 0; i < slotLevels.Length; i++)
            {
                if (slotLevels[i] <= 0) continue;
                cats.Add(i);
                if (counts.ContainsKey(slotLanes[i])) counts[slotLanes[i]]++;
            }
            cats.Sort((x, y) => slotLevels[x] != slotLevels[y] ? slotLevels[y].CompareTo(slotLevels[x]) : x.CompareTo(y));
            var order = new List<int>(counts.Keys);
            order.Sort((x, y) => counts[x] != counts[y] ? counts[x].CompareTo(counts[y]) : x.CompareTo(y));

            foreach (int c in cats)
            {
                bool inactive = !counts.ContainsKey(slotLanes[c]);
                foreach (int lane in order)
                {
                    if (!inactive && counts[lane] > counts[slotLanes[c]] - 2) break; // order tăng dần: các hàng sau chỉ đông hơn
                    int dest = -1;
                    for (int s = 0; s < slotLevels.Length; s++)
                    {
                        if (slotLanes[s] != lane) continue;
                        if (slotLevels[s] == 0) { dest = s; break; }
                        // Chỉ đổi chỗ khi mèo kia yếu hơn (và chỉ khi nguồn nằm ngoài hàng có quái).
                        if (inactive && slotLevels[s] < slotLevels[c] && (dest < 0 || slotLevels[s] < slotLevels[dest])) dest = s;
                    }
                    if (dest < 0 || (slotLevels[dest] > 0 && !inactive)) continue;
                    from = c; to = dest;
                    return true;
                }
            }
            return false;
        }

        void WriteReport(List<Result>[] all)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Balance bot report (policy={policy})");
            sb.AppendLine();
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm} | policy: {policy} | runsPerLevel: {runs} | speed: {speed}x");
            sb.AppendLine();
            sb.AppendLine("| Level | Wins/Runs | Avg wall % at end | Avg wave reached | Avg seconds |");
            sb.AppendLine("|---|---|---|---|---|");
            for (int l = 0; l < levels.Length; l++)
            {
                int wins = 0; float wall = 0, wave = 0, sec = 0;
                foreach (Result r in all[l]) { if (r.outcome == "Won") wins++; wall += r.wall; wave += r.wave; sec += r.seconds; }
                int n = Math.Max(1, all[l].Count);
                sb.AppendLine($"| {levels[l] + 1} | {wins}/{all[l].Count} | {wall / n:0} | {wave / n:0.0} | {sec / n:0} |");
            }
            string path = $"production/balance/bot-report-{policy.ToString().ToLowerInvariant()}.md";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>Khôi phục tiến trình lưu và các thiết lập tạm; an toàn khi gọi nhiều lần.</summary>
        void Finish()
        {
            if (!restored)
            {
                restored = true;
                if (hadSave) PlayerPrefs.SetString(Profile.Key, originalSave);
                else PlayerPrefs.DeleteKey(Profile.Key);
                PlayerPrefs.Save();
                Profile.Reload();
                Time.timeScale = 1f;
                Application.runInBackground = prevBackground;
            }
            if (instance == this) instance = null;
            Destroy(gameObject);
        }

        void OnDestroy() { if (!restored && originalSave != null) Finish(); else if (instance == this) instance = null; }
        void OnApplicationQuit() { if (!restored && originalSave != null) Finish(); }
    }
}
