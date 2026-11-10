using UnityEngine;
using UnityEngine.UI;

namespace CatDefender
{
    /// <summary>Phát cue UI khi nút được kích hoạt.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class AudioButton : MonoBehaviour
    {
        [SerializeField] AudioCue cue = AudioCue.UiClick;

        Button button;

        void Awake() => button = GetComponent<Button>();

        void OnEnable()
        {
            if (cue != AudioCue.None) button.onClick.AddListener(Play);
        }

        void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Play);
        }

        void Play() => AudioManager.Play(cue);
    }
}
