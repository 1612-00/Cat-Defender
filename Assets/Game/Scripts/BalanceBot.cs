using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatDefender
{
    /// <summary>
    /// Công cụ gỡ lỗi (D.1 trong gameplay-depth-plan): bot chơi ẩu để đo độ khó. Chỉ chạy khi gọi <see cref="Run"/> trong Play mode.
    /// Tiến trình lưu thật được sao lưu rồi khôi phục khi xong hoặc khi bị ngắt.
    /// </summary>
    public sealed class BalanceBot : MonoBehaviour
    {
        const float TickSeconds = 0.25f, TimeoutSeconds = 600f;
        const string BackupPath = "Library/balance_bot_save_backup.json";
        const string ReportPath = "production/balance/bot-report.md";

        static BalanceBot instance;

        struct Result { public string outcome; public float wall, seconds; public int wave; }

        int[] levels;
        int runs;
        float speed;
        bool hadSave, restored, prevBackground;
        string originalSave;
        GameManager game;

        /// <summary>Chạy bot trên các level (chỉ số 0), mỗi level runsPerLevel lượt, ở tốc độ speed.</summary>
        public static void Run(int[] levelIndices, int runsPerLevel, float speed)
        {
            if (instance != null) { Debug.LogError("BalanceBot is already running."); return; }
            if (!Application.isPlaying) { Debug.LogError("BalanceBot needs Play mode."); return; }
            var go = new GameObject("BalanceBot");
            DontDestroyOnLoad(go);
            BalanceBot bot = go.AddComponent<BalanceBot>();
            bot.levels = levelIndices;
            bot.runs = runsPerLevel;
            bot.speed = speed;
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
                    Debug.Log($"BALANCE level={levels[l] + 1} run={r + 1} result={res.outcome} wall={res.wall:0}% wave={res.wave}/{game.WaveCount} seconds={res.seconds:0}");
                }
            }
            WriteReport(all);
            Finish();
            Debug.Log("BALANCE DONE");
        }

        void Act(BoardView board)
        {
            if (game.CanBuyCat) game.BuyCat();
            var lv = new int[board.SlotCount];
            for (int i = 0; i < lv.Length; i++) lv[i] = board.LevelAt(i);
            if (FindMergePair(lv, board.MaxLevel, out int a, out int b))
            {
                board.Press(board.SlotWorld(a));
                board.Release(board.SlotWorld(b));
            }
        }

        void WriteReport(List<Result>[] all)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Balance bot report");
            sb.AppendLine();
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm} | runsPerLevel: {runs} | speed: {speed}x");
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
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString());
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
