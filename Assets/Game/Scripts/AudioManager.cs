using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatDefender
{
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
        WideExplosion,
        Merge,
        LevelComplete,
        GameOver,
        WaveStart,
        BossAppear,
        PerkOffer,
        CatTrash,
        WallHit,
        WallCritical,
        BossStun,
        EnemySplit,
        BoxingActivate
    }

    /// <summary>Phát SFX có ưu tiên và nhạc nền dùng chung qua hai scene.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int CriticalPriority = 80;
        const int HighestPriority = 100;

        [SerializeField] AudioConfig config;

        static AudioManager instance;
        static int nextShot;
        static int nextCoin;
        static bool gamePaused;

        readonly Dictionary<AudioCue, AudioCueClip> cueEntries = new Dictionary<AudioCue, AudioCueClip>();
        readonly Dictionary<AudioCue, float> lastPlayed = new Dictionary<AudioCue, float>();
        readonly List<AudioSource> sfxVoices = new List<AudioSource>();
        readonly List<int> voicePriorities = new List<int>();
        readonly List<float> voiceEndTimes = new List<float>();
        readonly List<bool> voiceUiBuses = new List<bool>();
        readonly AudioSource[] musicVoices = new AudioSource[2];

        AudioSource currentMusic;
        AudioSource fadingMusic;
        float currentMusicBaseVolume;
        float fadingMusicStartVolume;
        float musicFadeProgress = 1f;
        float pauseStartedAt;
        bool bossActive;
        bool resultActive;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            if (config == null)
            {
                Debug.LogError("AudioManager has no AudioConfig.", this);
                return;
            }

            cueEntries.Clear();
            if (config.cues != null)
            {
                foreach (AudioCueClip entry in config.cues)
                {
                    if (entry == null || entry.cue == AudioCue.None || entry.clip == null) continue;
                    if (cueEntries.ContainsKey(entry.cue))
                        Debug.LogError($"Duplicate audio cue configuration: {entry.cue}", this);
                    else
                        cueEntries.Add(entry.cue, entry);
                }
            }

            for (int i = 0; i < Mathf.Max(1, config.sfxVoiceCount); i++) AddSfxVoice();
            musicVoices[0] = CreateMusicVoice("Music A");
            musicVoices[1] = CreateMusicVoice("Music B");
            AudioListener.pause = gamePaused;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetMusicForScene(SceneManager.GetActiveScene());
        }

        void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioListener.pause = false;
            gamePaused = false;
            instance = null;
        }

        void Update()
        {
            if (currentMusic == null || config == null) return;
            float delta = Time.unscaledDeltaTime;
            float targetVolume = MusicTargetVolume();
            if (musicFadeProgress < 1f)
            {
                musicFadeProgress = Mathf.Min(1f, musicFadeProgress + delta / Mathf.Max(0.01f, config.musicFadeSeconds));
                currentMusic.volume = targetVolume * musicFadeProgress;
                if (fadingMusic != null)
                {
                    fadingMusic.volume = fadingMusicStartVolume * (1f - musicFadeProgress);
                    if (musicFadeProgress >= 1f)
                    {
                        fadingMusic.Stop();
                        fadingMusic.clip = null;
                        fadingMusic = null;
                    }
                }
            }
            else
            {
                float fadeRate = 1f / Mathf.Max(0.01f, config.musicFadeSeconds);
                currentMusic.volume = Mathf.MoveTowards(currentMusic.volume, targetVolume, fadeRate * delta);
            }

            if (currentMusic.volume <= 0.001f && !Profile.Data.music)
            {
                if (currentMusic.isPlaying) currentMusic.Pause();
            }
            else if (Profile.Data.music && !currentMusic.isPlaying && currentMusic.clip != null)
            {
                currentMusic.UnPause();
            }
        }

        AudioSource AddSfxVoice()
        {
            var voiceObject = new GameObject("SFX Voice");
            voiceObject.transform.SetParent(transform, false);
            AudioSource voice = voiceObject.AddComponent<AudioSource>();
            ConfigureVoice(voice);
            sfxVoices.Add(voice);
            voicePriorities.Add(0);
            voiceEndTimes.Add(0f);
            voiceUiBuses.Add(false);
            return voice;
        }

        AudioSource CreateMusicVoice(string voiceName)
        {
            var voiceObject = new GameObject(voiceName);
            voiceObject.transform.SetParent(transform, false);
            AudioSource voice = voiceObject.AddComponent<AudioSource>();
            ConfigureVoice(voice);
            voice.loop = true;
            voice.ignoreListenerPause = true;
            return voice;
        }

        static void ConfigureVoice(AudioSource voice)
        {
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0f;
            voice.dopplerLevel = 0f;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetGamePaused(false);
            if (instance == null) return;
            instance.bossActive = false;
            instance.resultActive = false;
            instance.SetMusicForScene(scene);
        }

        void SetMusicForScene(Scene scene)
        {
            if (config == null) return;
            AudioClip nextClip = scene.name == "Menu" ? config.menuMusic :
                scene.name == "Gameplay" ? config.gameplayMusic : null;
            if (nextClip == null)
            {
                Debug.LogError($"No music configured for scene '{scene.name}'.", this);
                return;
            }
            if (currentMusic != null && currentMusic.clip == nextClip && currentMusic.isPlaying) return;

            fadingMusic = currentMusic;
            fadingMusicStartVolume = fadingMusic != null ? fadingMusic.volume : 0f;
            currentMusic = currentMusic == musicVoices[0] ? musicVoices[1] : musicVoices[0];
            currentMusic.Stop();
            currentMusic.clip = nextClip;
            currentMusic.volume = 0f;
            currentMusic.Play();
            currentMusicBaseVolume = scene.name == "Menu" ? config.menuMusicVolume : config.gameplayMusicVolume;
            musicFadeProgress = 0f;
        }

        float MusicTargetVolume()
        {
            if (!Profile.Data.music) return 0f;
            float multiplier = bossActive ? config.bossMusicMultiplier : 1f;
            if (resultActive) multiplier *= config.resultMusicMultiplier;
            return currentMusicBaseVolume * multiplier;
        }

        public static void Play(AudioCue cue)
        {
            if (instance == null || cue == AudioCue.None || !Profile.Data.sound) return;
            if (!instance.cueEntries.TryGetValue(cue, out AudioCueClip entry)) return;
            float now = Time.unscaledTime;
            if (instance.lastPlayed.TryGetValue(cue, out float last) && now - last < entry.minInterval) return;

            AudioSource voice = instance.FindVoice(entry.priority);
            if (voice == null) return;
            instance.lastPlayed[cue] = now;
            int voiceIndex = instance.sfxVoices.IndexOf(voice);
            instance.voicePriorities[voiceIndex] = entry.priority;
            voice.ignoreListenerPause = entry.bus == AudioCueBus.UI;
            voice.priority = Mathf.Clamp(HighestPriority - entry.priority, 0, 256);
            voice.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            voice.volume = Mathf.Clamp01(entry.volume * Random.Range(1f - entry.volumeVariation, 1f + entry.volumeVariation));
            instance.voiceUiBuses[voiceIndex] = entry.bus == AudioCueBus.UI;
            instance.voiceEndTimes[voiceIndex] = now + entry.clip.length / Mathf.Max(0.01f, voice.pitch);
            voice.clip = entry.clip;
            voice.Play();
        }

        AudioSource FindVoice(int priority)
        {
            for (int i = 0; i < sfxVoices.Count; i++)
            {
                bool pausedGameplayVoice = gamePaused && !voiceUiBuses[i] && Time.unscaledTime < voiceEndTimes[i];
                if (!sfxVoices[i].isPlaying && !pausedGameplayVoice) return sfxVoices[i];
            }

            int lowestPriority = int.MaxValue;
            int lowestIndex = -1;
            for (int i = 0; i < sfxVoices.Count; i++)
            {
                if (voicePriorities[i] < lowestPriority)
                {
                    lowestPriority = voicePriorities[i];
                    lowestIndex = i;
                }
            }
            if (lowestIndex >= 0 && lowestPriority < priority) return sfxVoices[lowestIndex];
            return priority >= CriticalPriority ? AddSfxVoice() : null;
        }

        /// <summary>Luân phiên tiếng bắn; âm lượng và cao độ được làm lệch nhẹ trong cấu hình.</summary>
        public static void PlayCatShot(int catLevel)
        {
            if (catLevel >= 11) Play(AudioCue.HeavyShot);
            else if (catLevel >= 6) Play(AudioCue.SpecialShot);
            else Play((nextShot++ & 1) == 0 ? AudioCue.CatShoot : AudioCue.CatShootAlternate);
        }

        /// <summary>Luân phiên hai biến thể tiếng coin.</summary>
        public static void PlayCoin() => Play((nextCoin++ & 1) == 0 ? AudioCue.Coin : AudioCue.CoinAlternate);

        /// <summary>Phát cue phù hợp khi kích hoạt một addon.</summary>
        public static void PlayAddon(AddonType type)
        {
            switch (type)
            {
                case AddonType.Spikes: Play(AudioCue.Skill); break;
                case AddonType.Tnt: Play(AudioCue.WideExplosion); break;
                case AddonType.Boxing: Play(AudioCue.BoxingActivate); break;
                case AddonType.Guardian: Play(AudioCue.Shield); break;
            }
        }

        public static void SetMusicEnabled(bool enabled)
        {
            if (instance == null || instance.currentMusic == null) return;
            if (enabled) instance.currentMusic.UnPause();
        }

        public static void SetGamePaused(bool paused)
        {
            if (gamePaused != paused && paused)
            {
                if (instance != null) instance.pauseStartedAt = Time.unscaledTime;
            }
            else if (gamePaused != paused && instance != null)
            {
                float pausedDuration = Time.unscaledTime - instance.pauseStartedAt;
                for (int i = 0; i < instance.voiceEndTimes.Count; i++)
                    if (!instance.voiceUiBuses[i]) instance.voiceEndTimes[i] += pausedDuration;
            }
            gamePaused = paused;
            AudioListener.pause = paused;
        }

        public static void SetBossActive(bool active)
        {
            if (instance != null) instance.bossActive = active;
        }

        public static void SetResultActive(bool active)
        {
            if (instance != null) instance.resultActive = active;
        }

        public static void Vibrate()
        {
            if (Application.isMobilePlatform && Profile.Data.vibration) Handheld.Vibrate();
        }
    }
}
