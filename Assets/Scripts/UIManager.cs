using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ludu.Core;

namespace Ludu.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Game Type Selection (shown FIRST)")]
        [SerializeField] private GameObject gameTypePanel;
        [SerializeField] private Button snakeLadderButton;
        [SerializeField] private Button ludoButton;

        [Header("Board Roots (same scene, toggled active/inactive)")]
        [SerializeField] private GameObject snakeLadderBoardRoot;
        [SerializeField] private GameObject ludoBoardRoot;

        [Header("Snake&Ladder Board Children - drag in 'Ladder&Snakes' and 'Pawns' here")]
        [SerializeField] private GameObject snakeLadderVisuals; // the "Ladder&Snakes" object
        [SerializeField] private GameObject snakeLadderPawns;   // the "Pawns" object under Snake&LadderBoard

        private GameType pendingGameType = GameType.Ludo;

        [Header("Player vs Bot Selection")]
        [SerializeField] private GameObject playerOrBotPanel;
        [SerializeField] private Button playerButton;
        [SerializeField] private Button botButton;

        [Header("Mode Selection Menu")]
        [SerializeField] private GameObject modeSelectionPanel;
        [SerializeField] private Button twoPlayerButton;
        [SerializeField] private Button fourPlayerButton;

        private bool pendingVsBot;

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

            if (snakeLadderButton != null)
                snakeLadderButton.onClick.AddListener(() => OnGameTypeSelected(GameType.SnakeAndLadder));
            if (ludoButton != null)
                ludoButton.onClick.AddListener(() => OnGameTypeSelected(GameType.Ludo));
            if (twoPlayerButton != null)
                twoPlayerButton.onClick.AddListener(() => OnModeSelected(GameMode.TwoPlayer));
            if (fourPlayerButton != null)
                fourPlayerButton.onClick.AddListener(() => OnModeSelected(GameMode.FourPlayer));
            if (playerButton != null)
                playerButton.onClick.AddListener(() => OnPlayerOrBotSelected(false));
            if (botButton != null)
                botButton.onClick.AddListener(() => OnPlayerOrBotSelected(true));
            if (rollDiceButton != null)
                rollDiceButton.onClick.AddListener(OnRollClicked);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (snakeLadderBoardRoot == null) Debug.LogWarning("[UIManager] snakeLadderBoardRoot not found/assigned - it won't be auto-deactivated.");
            if (ludoBoardRoot == null) Debug.LogWarning("[UIManager] ludoBoardRoot not found/assigned - it won't be auto-deactivated.");

            ShowGameTypeMenu();
        }

        private void AutoFindReferences()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            GameObject root = canvas.gameObject;

            if (gameTypePanel == null)
            {
                Transform t = root.transform.Find("GameTypeSelectionPanel");
                if (t != null) gameTypePanel = t.gameObject;
            }

            if (snakeLadderButton == null && gameTypePanel != null)
            {
                Transform t = gameTypePanel.transform.Find("SnakeLadderButton");
                if (t != null) snakeLadderButton = t.GetComponent<Button>();
            }

            if (ludoButton == null && gameTypePanel != null)
            {
                Transform t = gameTypePanel.transform.Find("LudoButton");
                if (t != null) ludoButton = t.GetComponent<Button>();
            }

            if (snakeLadderBoardRoot == null)
            {
                Transform t = root.transform.Find("Snake&LadderBoard");
                if (t != null) snakeLadderBoardRoot = t.gameObject;
            }

            if (ludoBoardRoot == null)
            {
                Transform t = root.transform.Find("LudoBoardContainer");
                if (t != null) ludoBoardRoot = t.gameObject;
            }

            if (snakeLadderVisuals == null)
            {
                Transform t = root.transform.Find("Snake&LadderBoard/Ladder&Snakes");
                if (t != null) snakeLadderVisuals = t.gameObject;
            }

            if (snakeLadderPawns == null)
            {
                Transform t = root.transform.Find("Snake&LadderBoard/Pawns");
                if (t != null) snakeLadderPawns = t.gameObject;
            }

            if (playerOrBotPanel == null)
            {
                Transform t = root.transform.Find("Bot&PlayerSelectionPanel");
                if (t != null) playerOrBotPanel = t.gameObject;
            }

            if (playerButton == null && playerOrBotPanel != null)
            {
                Transform t = playerOrBotPanel.transform.Find("PlayerButton");
                if (t != null) playerButton = t.GetComponent<Button>();
            }

            if (botButton == null && playerOrBotPanel != null)
            {
                Transform t = playerOrBotPanel.transform.Find("BotButton");
                if (t != null) botButton = t.GetComponent<Button>();
            }

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

        public void ShowGameTypeMenu()
        {
            if (gameTypePanel != null) gameTypePanel.SetActive(true);
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(false);
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (snakeLadderBoardRoot != null) snakeLadderBoardRoot.SetActive(false);
            if (ludoBoardRoot != null) ludoBoardRoot.SetActive(false);
            if (snakeLadderVisuals != null) snakeLadderVisuals.SetActive(false);
            if (snakeLadderPawns != null) snakeLadderPawns.SetActive(false);
            SetRollButtonInteractable(false);
        }

        private void OnGameTypeSelected(GameType type)
        {
            pendingGameType = type;

            // Toggle which board is live in the scene.
            bool isSnakeLadder = (type == GameType.SnakeAndLadder);
            if (snakeLadderBoardRoot != null) snakeLadderBoardRoot.SetActive(isSnakeLadder);
            if (ludoBoardRoot != null) ludoBoardRoot.SetActive(!isSnakeLadder);

            // Force these explicitly too - don't rely on the parent's active state alone,
            // in case they were individually left inactive in the scene.
            if (snakeLadderVisuals != null) snakeLadderVisuals.SetActive(isSnakeLadder);
            if (snakeLadderPawns != null) snakeLadderPawns.SetActive(isSnakeLadder);

            if (gameTypePanel != null) gameTypePanel.SetActive(false);
            ShowModeSelectionMenu();
        }

        public void ShowModeSelectionMenu()
        {
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(true);
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            SetRollButtonInteractable(false);
        }

        private void OnPlayerOrBotSelected(bool vsBot)
        {
            pendingVsBot = vsBot;
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(false);
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(true);
        }

        private void OnModeSelected(GameMode mode)
        {
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            GameManager.Instance?.StartGame(pendingGameType, mode, pendingVsBot);
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
                case PlayerColor.Red: turnIndicatorText.color = new Color(0.9f, 0.2f, 0.2f); break;
                case PlayerColor.Green: turnIndicatorText.color = new Color(0.2f, 0.8f, 0.3f); break;
                case PlayerColor.Yellow: turnIndicatorText.color = new Color(0.9f, 0.8f, 0.1f); break;
                case PlayerColor.Blue: turnIndicatorText.color = new Color(0.2f, 0.4f, 0.9f); break;
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