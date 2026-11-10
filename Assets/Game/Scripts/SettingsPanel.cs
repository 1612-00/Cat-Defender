using UnityEngine;
using UnityEngine.UI;

namespace CatDefender
{
    /// <summary>Các nút bật/tắt nhạc, âm thanh, rung và đổi ngôn ngữ. Dùng chung cho menu và màn tạm dừng.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Button music, sound, vibration, language;
        [SerializeField] Sprite musicOn, musicOff, soundOn, soundOff, vibrationOn, vibrationOff;

        void Start()
        {
            music.onClick.AddListener(ToggleMusic);
            sound.onClick.AddListener(ToggleSound);
            vibration.onClick.AddListener(ToggleVibration);
            language.onClick.AddListener(Loc.ToggleLanguage);
            AudioManager.SetMusicEnabled(Profile.Data.music);
        }

        void OnEnable()
        {
            Profile.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Profile.Changed -= Refresh;

        static void ToggleMusic()
        {
            Profile.Data.music = !Profile.Data.music;
            Profile.Save();
            AudioManager.SetMusicEnabled(Profile.Data.music);
        }

        static void ToggleVibration()
        {
            Profile.Data.vibration = !Profile.Data.vibration;
            Profile.Save();
            if (Profile.Data.vibration) AudioManager.Vibrate();
        }

        static void ToggleSound()
        {
            Profile.Data.sound = !Profile.Data.sound;
            Profile.Save();
            if (Profile.Data.sound) AudioManager.Play(AudioCue.Switch);
        }

        void Refresh()
        {
            SaveData d = Profile.Data;
            music.image.sprite = d.music ? musicOn : musicOff;
            sound.image.sprite = d.sound ? soundOn : soundOff;
            vibration.image.sprite = d.vibration ? vibrationOn : vibrationOff;
        }
    }
}
