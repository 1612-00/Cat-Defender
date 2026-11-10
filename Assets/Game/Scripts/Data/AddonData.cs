using UnityEngine;

namespace CatDefender
{
    public enum AddonType { Spikes, Tnt, Boxing, Guardian }

    [CreateAssetMenu(menuName = "CatDefender/Addon")]
    public sealed class AddonData : ScriptableObject
    {
        public AddonType type;
        public Sprite icon;
        /// <summary>Số lượt dùng trong một level.</summary>
        public int uses = 2;
        /// <summary>Thời gian tồn tại (giây). Không dùng cho TNT.</summary>
        public float duration = 8f;
        /// <summary>Sát thương mỗi lần gây (mỗi nhịp với chông và Cat Boxing, một lần với TNT).</summary>
        public int damage = 10;
        public float tickInterval = 0.5f;
        /// <summary>Bán kính nổ (TNT) hoặc bề rộng vùng tác dụng tính từ tường (chông, Cat Boxing).</summary>
        public float range = 2.5f;
        /// <summary>Khung hình của nhân vật hỗ trợ (Cat Boxing, Cat Guardian).</summary>
        public Sprite[] frames;
    }
}
