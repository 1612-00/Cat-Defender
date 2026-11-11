using UnityEngine;
using UnityEngine.UI;

namespace CatDefender
{
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] Text coinText;
        [SerializeField] Text waveText;
        [SerializeField] Text levelText;
        [SerializeField] Text buyCostText;
        [SerializeField] Text repairCostText;
        [SerializeField] Button buyButton;
        [SerializeField] Button repairButton;
        [SerializeField] Button[] addonButtons;
        [SerializeField] Text[] addonCountTexts;

        [Header("Kết thúc level")]
        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        /// <summary>Dòng "hàng nào bị thủng nhiều nhất" trên popup thua.</summary>
        [SerializeField] Text loseReasonText;
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button[] restartButtons;
        [SerializeField] Button[] menuButtons;
        /// <summary>Số coin thưởng trên popup thắng và popup thua.</summary>
        [SerializeField] Text[] rewardCoinTexts;
        [SerializeField] Text rewardGemText;
        [SerializeField] GameObject rewardGemRow;

        [Header("Tạm dừng")]
        [SerializeField] Button pauseButton;
        [SerializeField] Button resumeButton;
        [SerializeField] GameObject pausePanel;

        [Header("Thẻ nâng cấp")]
        [SerializeField] GameObject perkPanel;
        [SerializeField] Button[] perkButtons;
        [SerializeField] Image[] perkIcons;
        [SerializeField] Text[] perkTitles;
        [SerializeField] Text[] perkDescriptions;

        void Start()
        {
            buyButton.onClick.AddListener(game.BuyCat);
            repairButton.onClick.AddListener(game.RepairWall);
            nextLevelButton.onClick.AddListener(() => game.LoadLevel(game.LevelNumber));
            pauseButton.onClick.AddListener(() => game.SetPaused(true));
            resumeButton.onClick.AddListener(() => game.SetPaused(false));
            foreach (Button b in restartButtons) b.onClick.AddListener(game.Restart);
            foreach (Button b in menuButtons) b.onClick.AddListener(game.GoToMenu);
            for (int i = 0; i < addonButtons.Length; i++)
            {
                int index = i;
                addonButtons[i].onClick.AddListener(() => game.UseAddon(index));
            }
            for (int i = 0; i < perkButtons.Length; i++)
            {
                int index = i;
                perkButtons[i].onClick.AddListener(() => game.ChoosePerk(index));
            }

            game.Changed += Refresh;
            game.Wallet.Changed += Refresh;
            game.Wall.Changed += Refresh;
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        void Refresh()
        {
            bool playing = game.State == GameState.Playing;
            coinText.text = game.Wallet.Coins.ToString();
            waveText.text = Loc.T("wave", game.CurrentWave, game.WaveCount);
            levelText.text = Loc.T("level", game.LevelNumber);
            buyCostText.text = game.CatCost.ToString();
            repairCostText.text = game.RepairCost.ToString();
            buyButton.interactable = game.CanAttemptBuyCat && !game.Paused;
            SetAffordabilityColor(buyButton, game.Wallet.Coins >= game.CatCost);
            repairButton.interactable = game.State == GameState.Playing && game.Wall.Hp < game.Wall.MaxHp && !game.Paused;
            SetAffordabilityColor(repairButton, game.Wallet.Coins >= game.RepairCost);
            pauseButton.interactable = playing && game.PerkChoices == null;
            for (int i = 0; i < addonButtons.Length; i++)
            {
                addonButtons[i].interactable = game.CanUseAddon(i);
                addonCountTexts[i].text = game.AddonUsesLeft(i).ToString();
            }

            winPanel.SetActive(game.State == GameState.Won);
            losePanel.SetActive(game.State == GameState.Lost);
            int worst = game.WorstLane;
            loseReasonText.gameObject.SetActive(worst >= 0);
            if (worst >= 0) loseReasonText.text = Loc.T("lose_reason", worst + 1);
            pausePanel.SetActive(playing && game.UserPaused);
            nextLevelButton.gameObject.SetActive(game.HasNextLevel);
            foreach (Text t in rewardCoinTexts) t.text = "+" + game.RewardCoins;
            rewardGemText.text = "+" + game.RewardGems;
            rewardGemRow.SetActive(game.RewardGems > 0);

            PerkData[] choices = game.PerkChoices;
            perkPanel.SetActive(choices != null);
            if (choices == null) return;
            for (int i = 0; i < perkButtons.Length; i++)
            {
                bool used = i < choices.Length;
                perkButtons[i].gameObject.SetActive(used);
                if (!used) continue;
                perkIcons[i].sprite = choices[i].icon;
                perkTitles[i].text = Loc.T(choices[i].titleKey);
                perkDescriptions[i].text = Loc.T(choices[i].descKey);
            }
        }

        static void SetAffordabilityColor(Button button, bool affordable)
        {
            ColorBlock colors = button.colors;
            Color tint = affordable ? Color.white : new Color(0.65f, 0.65f, 0.65f);
            colors.normalColor = tint;
            colors.highlightedColor = tint;
            colors.selectedColor = tint;
            button.colors = colors;
        }
    }
}
