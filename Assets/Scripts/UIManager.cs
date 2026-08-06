using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ludu.Core;

namespace Ludu.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Mode Selection Menu")]
        [SerializeField] private GameObject modeSelectionPanel;
        [SerializeField] private Button twoPlayerButton;
        [SerializeField] private Button fourPlayerButton;

        [Header("HUD Controls")]
        [SerializeField] private Button rollDiceButton;
        [SerializeField] private TextMeshProUGUI turnIndicatorText;

        [Header("Win Overlay")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private TextMeshProUGUI winText;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            // Auto-find refs from canvas hierarchy if not assigned in Inspector
            AutoFindReferences();

            if (twoPlayerButton != null)
                twoPlayerButton.onClick.AddListener(() => OnModeSelected(GameMode.TwoPlayer));
            if (fourPlayerButton != null)
                fourPlayerButton.onClick.AddListener(() => OnModeSelected(GameMode.FourPlayer));
            if (rollDiceButton != null)
                rollDiceButton.onClick.AddListener(OnRollClicked);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            ShowModeSelectionMenu();
        }

        private void AutoFindReferences()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            GameObject root = canvas.gameObject;

            if (modeSelectionPanel == null)
            {
                Transform t = root.transform.Find("ModeSelectionPanel");
                if (t != null) modeSelectionPanel = t.gameObject;
            }

            if (twoPlayerButton == null && modeSelectionPanel != null)
            {
                Transform t = modeSelectionPanel.transform.Find("TwoPlayerButton");
                if (t != null) twoPlayerButton = t.GetComponent<Button>();
            }

            if (fourPlayerButton == null && modeSelectionPanel != null)
            {
                Transform t = modeSelectionPanel.transform.Find("FourPlayerButton");
                if (t != null) fourPlayerButton = t.GetComponent<Button>();
            }

            if (rollDiceButton == null)
            {
                Transform controls = root.transform.Find("ControlsPanel");
                if (controls != null)
                {
                    Transform btn = controls.Find("RollDiceButton");
                    if (btn != null) rollDiceButton = btn.GetComponent<Button>();
                }
            }

            if (turnIndicatorText == null)
            {
                Transform header = root.transform.Find("HeaderPanel");
                if (header != null)
                {
                    Transform t = header.Find("TurnText");
                    if (t != null) turnIndicatorText = t.GetComponent<TextMeshProUGUI>();
                }
            }

            if (winPanel == null)
            {
                Transform t = root.transform.Find("WinScreenPanel");
                if (t != null)
                {
                    winPanel = t.gameObject;
                    Transform winTxt = t.Find("WinText");
                    if (winTxt != null) winText = winTxt.GetComponent<TextMeshProUGUI>();
                    Transform restartBtn = t.Find("RestartButton");
                    if (restartBtn != null) restartButton = restartBtn.GetComponent<Button>();
                }
            }
        }

        public void ShowModeSelectionMenu()
        {
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            SetRollButtonInteractable(false);
        }

        private void OnModeSelected(GameMode mode)
        {
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            GameManager.Instance?.StartGame(mode);
        }

        private void OnRollClicked()
        {
            Debug.Log("[UIManager] Roll button clicked.");
            GameManager.Instance?.RollDice();
        }

        private void OnRestartClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void UpdateTurnIndicator(PlayerColor currentTurn)
        {
            if (turnIndicatorText == null) return;
            turnIndicatorText.text = $"{currentTurn}'s Turn";
            switch (currentTurn)
            {
                case PlayerColor.Red:    turnIndicatorText.color = new Color(0.9f, 0.2f, 0.2f); break;
                case PlayerColor.Green:  turnIndicatorText.color = new Color(0.2f, 0.8f, 0.3f); break;
                case PlayerColor.Yellow: turnIndicatorText.color = new Color(0.9f, 0.8f, 0.1f); break;
                case PlayerColor.Blue:   turnIndicatorText.color = new Color(0.2f, 0.4f, 0.9f); break;
            }
        }

        public void UpdateDiceValue(int val)
        {
            // Dice value is now shown visually by the Dice face GameObjects themselves,
            // so there's no text label to update here anymore.
        }

        public void SetRollButtonInteractable(bool interactable)
        {
            if (rollDiceButton != null)
                rollDiceButton.interactable = interactable;
        }

        public void ShowWinScreen(PlayerColor winner)
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (winText != null) winText.text = $"{winner} Wins! 🎉";
        }
    }
}
