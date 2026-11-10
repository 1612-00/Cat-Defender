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
            music.onClick.AddListener(() => Toggle(ref Profile.Data.music));
            sound.onClick.AddListener(ToggleSound);
            vibration.onClick.AddListener(() => Toggle(ref Profile.Data.vibration));
            language.onClick.AddListener(Loc.ToggleLanguage);
        }

        void OnEnable()
        {
            Profile.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Profile.Changed -= Refresh;

        static void Toggle(ref bool flag)
        {
            flag = !flag;
            Profile.Save();
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
