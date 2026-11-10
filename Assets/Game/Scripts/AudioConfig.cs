using UnityEngine;

namespace CatDefender
{
    public enum AudioCueBus
    {
        Gameplay,
        UI
    }

    /// <summary>One editable sound and its playback policy.</summary>
    [System.Serializable]
    public sealed class AudioCueClip
    {
        public AudioCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
        [Range(0f, 0.5f)] public float pitchVariation;
        [Range(0f, 0.5f)] public float volumeVariation;
        [Min(0f)] public float minInterval = 0.06f;
        [Range(0, 100)] public int priority = 25;
        public AudioCueBus bus;
    }

    [CreateAssetMenu(menuName = "CatDefender/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        public AudioCueClip[] cues;
        public AudioClip menuMusic;
        public AudioClip gameplayMusic;
        [Range(1, 16)] public int sfxVoiceCount = 8;
        [Range(0f, 1f)] public float menuMusicVolume = 0.16f;
        [Range(0f, 1f)] public float gameplayMusicVolume = 0.14f;
        [Range(0f, 1f)] public float bossMusicMultiplier = 0.72f;
        [Range(0f, 1f)] public float resultMusicMultiplier = 0.25f;
        [Min(0.01f)] public float musicFadeSeconds = 1.25f;
    }
}
