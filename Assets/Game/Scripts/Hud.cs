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
        [SerializeField] Button prevLevelButton;
        [SerializeField] Button skipLevelButton;
        [SerializeField] Button[] addonButtons;
        [SerializeField] Text[] addonCountTexts;
        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button[] restartButtons;

        void Start()
        {
            buyButton.onClick.AddListener(game.BuyCat);
            repairButton.onClick.AddListener(game.RepairWall);
            prevLevelButton.onClick.AddListener(() => game.LoadLevel(game.LevelNumber - 2));
            skipLevelButton.onClick.AddListener(() => game.LoadLevel(game.LevelNumber));
            nextLevelButton.onClick.AddListener(() => game.LoadLevel(game.LevelNumber));
            foreach (Button b in restartButtons) b.onClick.AddListener(game.Restart);
            for (int i = 0; i < addonButtons.Length; i++)
            {
                int index = i;
                addonButtons[i].onClick.AddListener(() => game.UseAddon(index));
            }

            game.Changed += Refresh;
            game.Wallet.Changed += Refresh;
            game.Wall.Changed += Refresh;
            Refresh();
        }

        void Refresh()
        {
            coinText.text = game.Wallet.Coins.ToString();
            waveText.text = string.Format(Strings.WaveFormat, game.CurrentWave, game.WaveCount);
            levelText.text = string.Format(Strings.LevelFormat, game.LevelNumber);
            buyCostText.text = game.CatCost.ToString();
            repairCostText.text = game.RepairCost.ToString();
            buyButton.interactable = game.CanBuyCat;
            repairButton.interactable = game.CanRepair;
            for (int i = 0; i < addonButtons.Length; i++)
            {
                addonButtons[i].interactable = game.CanUseAddon(i);
                addonCountTexts[i].text = game.AddonUsesLeft(i) + "/" + game.Addon(i).uses;
            }
            winPanel.SetActive(game.State == GameState.Won);
            losePanel.SetActive(game.State == GameState.Lost);
            nextLevelButton.gameObject.SetActive(game.HasNextLevel);
        }
    }
}
