using UnityEngine;
using UnityEngine.UI;

namespace CatDefender
{
    /// <summary>Một dòng mua được trong menu: nâng cấp, addon hoặc gói coin.</summary>
    public sealed class ShopRow : MonoBehaviour
    {
        public Image icon;
        public Text title;
        public Text description;
        /// <summary>Thanh 5 nấc hiển thị cấp hiện tại; null với gói coin.</summary>
        public Image pips;
        public Text cost;
        public Image costIcon;
        public Button button;
    }
}
