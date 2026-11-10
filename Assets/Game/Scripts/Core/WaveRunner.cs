using System.Collections.Generic;

namespace CatDefender
{
    /// <summary>Một nhóm quái trong wave: loại quái, số lượng, khoảng cách giữa hai lần sinh.</summary>
    public struct SpawnGroup
    {
        public int enemyId;
        public int count;
        public float interval;
    }

    /// <summary>
    /// Điều khiển việc sinh quái theo wave. Wave kế tiếp bắt đầu sau khi wave hiện tại
    /// sinh hết, không còn quái sống, và hết thời gian nghỉ. Logic thuần.
    /// </summary>
    public sealed class WaveRunner
    {
        readonly IReadOnlyList<IReadOnlyList<SpawnGroup>> waves;
        readonly float waveDelay;
        int wave, group, spawnedInGroup;
        float timer;
        bool resting = true;

        public WaveRunner(IReadOnlyList<IReadOnlyList<SpawnGroup>> waves, float waveDelay, float firstDelay)
        {
            this.waves = waves;
            this.waveDelay = waveDelay;
            timer = firstDelay;
        }

        public int WaveCount => waves.Count;
        /// <summary>Số thứ tự wave đang chạy, tính từ 1.</summary>
        public int CurrentWave => System.Math.Min(wave + 1, waves.Count);
        public bool AllSpawned => wave >= waves.Count;
        /// <summary>Đang nghỉ trước một wave (kể cả trước wave đầu tiên).</summary>
        public bool Resting => resting && !AllSpawned;
        /// <summary>Số giây còn lại trước khi wave kế tiếp bắt đầu, hoặc 0 nếu không nghỉ.</summary>
        public float TimeUntilNextWave => Resting && timer > 0 ? timer : 0;

        /// <summary>Thêm vào <paramref name="spawns"/> id các quái cần sinh trong khung hình này.</summary>
        public void Tick(float dt, int aliveEnemies, List<int> spawns)
        {
            if (AllSpawned) return;

            if (resting)
            {
                // Trước wave đầu tiên chỉ chờ hết giờ; giữa các wave phải hạ hết quái trước.
                if (aliveEnemies > 0 && wave > 0) return;
                timer -= dt;
                if (timer > 0) return;
                resting = false;
                timer = 0;
                dt = 0;
            }

            timer -= dt;
            while (timer <= 0 && !AllSpawned && !resting)
            {
                var g = waves[wave][group];
                spawns.Add(g.enemyId);
                timer += g.interval > 0 ? g.interval : 0.01f;
                if (++spawnedInGroup < g.count) continue;
                spawnedInGroup = 0;
                if (++group < waves[wave].Count) continue;
                group = 0;
                wave++;
                resting = true;
                timer = waveDelay;
            }
        }
    }
}
