using UnityEngine;
using UnityEngine.UI;

namespace CatDefender
{
    /// <summary>
    /// Gắn vào mọi Text hiển thị chữ có dịch. Đổi font theo ngôn ngữ, và nếu có <see cref="key"/>
    /// thì tự điền chữ. Để trống key khi chữ do code khác gán (ví dụ "Đợt 2 / 5").
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocText : MonoBehaviour
    {
        [SerializeField] string key;

        Text text;
        Font defaultFont;

        void Awake()
        {
            text = GetComponent<Text>();
            defaultFont = text.font;
        }

        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        void OnDisable() => Loc.Changed -= Apply;

        void Apply()
        {
            bool vi = Loc.IsVietnamese;
            text.font = vi ? Loc.VietnameseFont : defaultFont;
            text.fontStyle = vi ? FontStyle.Bold : FontStyle.Normal;
            if (!string.IsNullOrEmpty(key)) text.text = Loc.T(key);
        }
    }
}
