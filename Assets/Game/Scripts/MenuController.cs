using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatDefender
{
    /// <summary>Scene Menu: màn chào, bản đồ level, nâng cấp, addon, shop, cài đặt.</summary>
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] MetaConfig meta;
        [SerializeField] GameObject landing;
        [SerializeField] GameObject main;
        [SerializeField] Button landingButton;
        [SerializeField] Button[] tabButtons;
        [SerializeField] GameObject[] tabPanels;
        [SerializeField] Sprite tabOn, tabOff;
        [SerializeField] Text coinText, gemText;
        [SerializeField] Button[] levelButtons;
        [SerializeField] Sprite levelCleared, levelOpen, levelLocked;
        [SerializeField] ShopRow[] upgradeRows, addonRows, packRows;
        /// <summary>Ảnh thanh cấp độ, chỉ số = cấp hiện tại (0..5).</summary>
        [SerializeField] Sprite[] pipSprites;
        [SerializeField] Button settingsButton, settingsClose;
        [SerializeField] GameObject settingsPanel;

        // Chỉ hiện màn chào ở lần mở game đầu tiên, không hiện lại khi từ màn chơi quay về.
        static bool landingSeen;
        int tab;

        void Awake()
        {
            Time.timeScale = 1f;
            Profile.Init(meta);
        }

        void Start()
        {
            landingButton.onClick.AddListener(() => ShowLanding(false));
            settingsButton.onClick.AddListener(() => settingsPanel.SetActive(true));
            settingsClose.onClick.AddListener(() => settingsPanel.SetActive(false));
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                tabButtons[i].onClick.AddListener(() => { tab = index; Refresh(); });
            }
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;
                levelButtons[i].onClick.AddListener(() => Play(index));
            }
            for (int i = 0; i < upgradeRows.Length; i++)
            {
                int index = i;
                upgradeRows[i].button.onClick.AddListener(() => BuyUpgrade(index));
            }
            for (int i = 0; i < addonRows.Length; i++)
            {
                int index = i;
                addonRows[i].button.onClick.AddListener(() => BuyAddon(index));
            }
            for (int i = 0; i < packRows.Length; i++)
            {
                int index = i;
                packRows[i].button.onClick.AddListener(() => BuyPack(index));
            }

            settingsPanel.SetActive(false);
            ShowLanding(!landingSeen);
        }

        void OnEnable()
        {
            Profile.Changed += Refresh;
            Loc.Changed += Refresh;
        }

        void OnDisable()
        {
            Profile.Changed -= Refresh;
            Loc.Changed -= Refresh;
        }

        void ShowLanding(bool show)
        {
            if (!show) landingSeen = true;
            landing.SetActive(show);
            main.SetActive(!show);
            Refresh();
        }

        public void Play(int levelIndex)
        {
            if (levelIndex > Profile.Data.unlockedLevel) return;
            Session.LevelIndex = levelIndex;
            SceneManager.LoadScene("Gameplay");
        }

        public void BuyUpgrade(int i)
        {
            SaveData d = Profile.Data;
            int level = d.upgradeLevels[i];
            int cost = meta.UpgradeCost(i, level);
            if (level >= meta.maxLevel || d.coins < cost) return;
            d.coins -= cost;
            d.upgradeLevels[i]++;
            Profile.Save();
        }

        public void BuyAddon(int i)
        {
            SaveData d = Profile.Data;
            int level = d.addonLevels[i];
            int cost = meta.AddonCost(level);
            if (level >= meta.maxLevel || d.coins < cost) return;
            d.coins -= cost;
            d.addonLevels[i]++;
            Profile.Save();
        }

        public void BuyPack(int i)
        {
            SaveData d = Profile.Data;
            MetaConfig.CoinPack pack = meta.packs[i];
            if (d.gems < pack.gems) return;
            d.gems -= pack.gems;
            d.coins += pack.coins;
            Profile.Save();
        }

        void Refresh()
        {
            SaveData d = Profile.Data;
            coinText.text = d.coins.ToString();
            gemText.text = d.gems.ToString();

            for (int i = 0; i < tabPanels.Length; i++)
            {
                tabPanels[i].SetActive(i == tab);
                tabButtons[i].image.sprite = i == tab ? tabOn : tabOff;
            }

            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool open = i <= d.unlockedLevel;
                levelButtons[i].image.sprite = i < d.unlockedLevel ? levelCleared : open ? levelOpen : levelLocked;
                levelButtons[i].interactable = open;
                levelButtons[i].GetComponentInChildren<Text>(true).gameObject.SetActive(open);
            }

            for (int i = 0; i < upgradeRows.Length; i++)
                RefreshLevelRow(upgradeRows[i], d.upgradeLevels[i], meta.UpgradeCost(i, d.upgradeLevels[i]), d.coins);
            for (int i = 0; i < addonRows.Length; i++)
                RefreshLevelRow(addonRows[i], d.addonLevels[i], meta.AddonCost(d.addonLevels[i]), d.coins);

            for (int i = 0; i < packRows.Length; i++)
            {
                MetaConfig.CoinPack pack = meta.packs[i];
                packRows[i].title.text = Loc.T("pack_coins", pack.coins);
                packRows[i].cost.text = pack.gems.ToString();
                packRows[i].button.interactable = d.gems >= pack.gems;
            }
        }

        void RefreshLevelRow(ShopRow row, int level, int cost, int coins)
        {
            bool maxed = level >= meta.maxLevel;
            row.pips.sprite = pipSprites[Mathf.Clamp(level, 0, pipSprites.Length - 1)];
            row.cost.text = maxed ? Loc.T("max") : cost.ToString();
            row.costIcon.gameObject.SetActive(!maxed);
            row.button.interactable = !maxed && coins >= cost;
        }
    }
}
