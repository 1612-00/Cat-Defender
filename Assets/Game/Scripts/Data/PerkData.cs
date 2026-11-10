using UnityEngine;

namespace CatDefender
{
    [CreateAssetMenu(menuName = "CatDefender/Perk")]
    public sealed class PerkData : ScriptableObject
    {
        public PerkType type;
        public Sprite icon;
        public string title;
        public string description;
        /// <summary>Ý nghĩa tuỳ loại: phần cộng thêm vào hệ số, máu tường, số lượt addon, hoặc cấp mèo tặng.</summary>
        public float value;
    }
}
