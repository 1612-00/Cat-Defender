using System.Collections.Generic;
using UnityEngine;

namespace CatDefender
{
    /// <summary>Các hiệu ứng âm thanh được gắn với thao tác trong game.</summary>
    public enum AudioCue
    {
        None = -1,
        UiClick,
        SelectCat,
        DropCat,
        Open,
        Close,
        Error,
        Confirm,
        Switch,
        Tick,
        CatShoot,
        CatShootAlternate,
        SpecialShot,
        HeavyShot,
        BulletHit,
        BoxingHit,
        Explosion,
        BossExplosion,
        EnemyHit,
        ArmorHit,
        Coin,
        CoinAlternate,
        CatPurchase,
        Upgrade,
        UpgradeConfirm,
        Shield,
        Skill,
        WideExplosion
    }

    /// <summary>Clip and playback settings explicitly associated with one audio cue.</summary>
    [System.Serializable]
    public sealed class AudioCueClip
    {
        /// <summary>Cue handled by this configuration.</summary>
        public AudioCue cue;
        /// <summary>Audio clip played for this cue.</summary>
        public AudioClip clip;
        /// <summary>Playback volume multiplier.</summary>
        [Range(0f, 1f)] public float volume = 0.5f;
        /// <summary>Minimum unscaled seconds between plays of this cue.</summary>
        [Min(0f)] public float minInterval = 0.06f;
    }

    /// <summary>Điểm phát âm thanh chung cho hai scene; clip chưa gán hoặc bị tắt tiếng sẽ được bỏ qua.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioCueClip[] entries;

        static AudioManager instance;
        static int nextShot;
        static int nextCoin;
        readonly Dictionary<AudioCue, AudioCueClip> cueEntries = new Dictionary<AudioCue, AudioCueClip>();
        readonly Dictionary<AudioCue, float> lastPlayed = new Dictionary<AudioCue, float>();
        AudioSource source;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            cueEntries.Clear();
            if (entries == null)
            {
                Debug.LogError("AudioManager has no cue configuration.", this);
                return;
            }
            foreach (AudioCueClip entry in entries)
            {
                if (entry == null || entry.cue == AudioCue.None || entry.clip == null) continue;
                if (cueEntries.ContainsKey(entry.cue))
                    Debug.LogError($"Duplicate audio cue configuration: {entry.cue}", this);
                else
                    cueEntries.Add(entry.cue, entry);
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Phát một cue nếu AudioManager, clip và tuỳ chọn âm thanh đều khả dụng.</summary>
        public static void Play(AudioCue cue)
        {
            if (instance == null || cue == AudioCue.None || !Profile.Data.sound) return;
            if (!instance.cueEntries.TryGetValue(cue, out AudioCueClip entry)) return;
            float now = Time.unscaledTime;
            if (instance.lastPlayed.TryGetValue(cue, out float last) && now - last < entry.minInterval) return;
            instance.lastPlayed[cue] = now;
            instance.source.PlayOneShot(entry.clip, entry.volume);
        }

        /// <summary>Luân phiên hai biến thể tiếng bắn để giảm lặp âm.</summary>
        public static void PlayCatShot(int catLevel)
        {
            if (catLevel >= 11)
            {
                Play(AudioCue.HeavyShot);
                return;
            }
            if (catLevel >= 6)
            {
                Play(AudioCue.SpecialShot);
                return;
            }
            Play((nextShot++ & 1) == 0 ? AudioCue.CatShoot : AudioCue.CatShootAlternate);
        }

        /// <summary>Luân phiên hai biến thể tiếng coin khi hạ quái.</summary>
        public static void PlayCoin()
        {
            Play((nextCoin++ & 1) == 0 ? AudioCue.Coin : AudioCue.CoinAlternate);
        }

        /// <summary>Phát cue phù hợp khi kích hoạt một addon.</summary>
        public static void PlayAddon(AddonType type)
        {
            switch (type)
            {
                case AddonType.Spikes: Play(AudioCue.Skill); break;
                case AddonType.Tnt: Play(AudioCue.WideExplosion); break;
                case AddonType.Boxing: Play(AudioCue.Confirm); break;
                case AddonType.Guardian: Play(AudioCue.Shield); break;
            }
        }
    }
}
