using UnityEngine;
using UnityEngine.UI;

namespace CatDefender
{
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] Text coinText;
        [SerializeField] Text waveText;
        [SerializeField] Text buyCostText;
        [SerializeField] Text repairCostText;
        [SerializeField] Button buyButton;
        [SerializeField] Button repairButton;
        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        [SerializeField] Button[] restartButtons;

        void Start()
        {
            buyButton.onClick.AddListener(game.BuyCat);
            repairButton.onClick.AddListener(game.RepairWall);
            foreach (Button b in restartButtons) b.onClick.AddListener(game.Restart);

            game.Changed += Refresh;
            game.Wallet.Changed += Refresh;
            game.Wall.Changed += Refresh;
            Refresh();
        }

        void Refresh()
        {
            coinText.text = game.Wallet.Coins.ToString();
            waveText.text = string.Format(Strings.WaveFormat, game.CurrentWave, game.WaveCount);
            buyCostText.text = game.CatCost.ToString();
            repairCostText.text = game.RepairCost.ToString();
            buyButton.interactable = game.CanBuyCat;
            repairButton.interactable = game.CanRepair;
            winPanel.SetActive(game.State == GameState.Won);
            losePanel.SetActive(game.State == GameState.Lost);
        }
    }
}
