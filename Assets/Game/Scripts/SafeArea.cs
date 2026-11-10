using UnityEngine;

namespace CatDefender
{
    /// <summary>Thu RectTransform về vùng an toàn của màn hình (tránh tai thỏ, góc bo). Đặt mọi UI bên trong nó.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect applied;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            rt.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
