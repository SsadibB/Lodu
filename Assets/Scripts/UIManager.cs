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

        [Header("Board Roots (same scene, toggled active/inactive) - all live under 'Boards' parent")]
        [SerializeField] private GameObject boardsRoot; // the "Boards" parent GameObject
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
        [SerializeField] private Button botPlayerBackButton; // Bot&PlayerSelectionPanel -> GameTypeSelectionPanel

        [Header("Mode Selection Menu")]
        [SerializeField] private GameObject modeSelectionPanel;
        [SerializeField] private Button twoPlayerButton;
        [SerializeField] private Button fourPlayerButton;
        [SerializeField] private Button modeBackButton; // ModeSelectionPanel -> Bot&PlayerSelectionPanel

        private bool pendingVsBot;

        [Header("HUD Controls")]
        [SerializeField] private TextMeshProUGUI turnIndicatorText;

        [Header("Shared Controls Panel (lives under 'Boards' parent, used by both games)")]
        [SerializeField] private GameObject controlsPanel;   // single shared ControlsPanel
        [SerializeField] private Button rollDiceButton;      // single shared Roll Dice button

        [Header("Shared Home Button (sibling of the board roots/ControlsPanel under 'Boards' - active only while a board is active)")]
        [SerializeField] private Button homeButton;           // single shared Home button

        [Header("Win Overlay")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private TextMeshProUGUI winText;
        [SerializeField] private Button playAgainButton; // -> Bot&PlayerSelectionPanel, same game type
        [SerializeField] private Button winHomeButton;    // -> GameTypeSelectionPanel

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
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (winHomeButton != null)
                winHomeButton.onClick.AddListener(OnWinHomeClicked);
            if (rollDiceButton != null)
                rollDiceButton.onClick.AddListener(OnRollClicked);
            if (homeButton != null)
                homeButton.onClick.AddListener(OnGameHomeClicked);
            if (botPlayerBackButton != null)
                botPlayerBackButton.onClick.AddListener(OnBotPlayerBackClicked);
            if (modeBackButton != null)
                modeBackButton.onClick.AddListener(OnModeBackClicked);

            if (snakeLadderBoardRoot == null) Debug.LogWarning("[UIManager] snakeLadderBoardRoot not found/assigned - it won't be auto-deactivated.");
            if (ludoBoardRoot == null) Debug.LogWarning("[UIManager] ludoBoardRoot not found/assigned - it won't be auto-deactivated.");
            if (controlsPanel == null) Debug.LogWarning("[UIManager] controlsPanel not found/assigned - controls won't show.");
            if (homeButton == null) Debug.LogWarning("[UIManager] homeButton not found/assigned - Home button won't function.");
            if (rollDiceButton == null) Debug.LogWarning("[UIManager] rollDiceButton not found/assigned - Roll Dice button won't function.");

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

            // "Boards" parent holds both board roots, the shared ControlsPanel and Home button
            if (boardsRoot == null)
            {
                Transform t = root.transform.Find("Boards");
                if (t != null) boardsRoot = t.gameObject;
            }

            if (boardsRoot != null)
            {
                if (snakeLadderBoardRoot == null)
                {
                    Transform t = boardsRoot.transform.Find("Snake&LadderBoard");
                    if (t == null) t = boardsRoot.transform.Find("Snake&LadderBoardContainer"); // fallback naming
                    if (t != null) snakeLadderBoardRoot = t.gameObject;
                }

                if (ludoBoardRoot == null)
                {
                    Transform t = boardsRoot.transform.Find("LudoBoardContainer");
                    if (t != null) ludoBoardRoot = t.gameObject;
                }

                if (controlsPanel == null)
                {
                    Transform t = boardsRoot.transform.Find("ControlsPanel");
                    if (t != null) controlsPanel = t.gameObject;
                }

                if (rollDiceButton == null && controlsPanel != null)
                {
                    Transform t = controlsPanel.transform.Find("RollDiceButton");
                    if (t != null) rollDiceButton = t.GetComponent<Button>();
                }

                // HomeButton is a direct child of Boards (sibling of the board roots and
                // ControlsPanel) - its active state is driven explicitly by whether a board
                // is showing, not by ControlsPanel's active state.
                if (homeButton == null)
                {
                    Transform t = boardsRoot.transform.Find("HomeButton");
                    if (t != null) homeButton = t.GetComponent<Button>();
                }
            }

            if (snakeLadderVisuals == null && snakeLadderBoardRoot != null)
            {
                Transform t = snakeLadderBoardRoot.transform.Find("Ladder&Snakes");
                if (t != null) snakeLadderVisuals = t.gameObject;
            }

            if (snakeLadderPawns == null && snakeLadderBoardRoot != null)
            {
                Transform t = snakeLadderBoardRoot.transform.Find("Pawns");
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

            if (botPlayerBackButton == null && playerOrBotPanel != null)
            {
                Transform t = playerOrBotPanel.transform.Find("BackButton");
                if (t != null) botPlayerBackButton = t.GetComponent<Button>();
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

            if (modeBackButton == null && modeSelectionPanel != null)
            {
                Transform t = modeSelectionPanel.transform.Find("BackButton");
                if (t != null) modeBackButton = t.GetComponent<Button>();
            }

            Transform headerPanel = root.transform.Find("HeaderPanel");

            if (turnIndicatorText == null)
            {
                if (headerPanel != null)
                {
                    Transform t = headerPanel.Find("TurnText");
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

                    Transform playAgainBtn = t.Find("PlayAgainButton");
                    if (playAgainBtn == null) playAgainBtn = t.Find("RestartButton"); // legacy name fallback
                    if (playAgainBtn != null) playAgainButton = playAgainBtn.GetComponent<Button>();

                    Transform homeBtn = t.Find("HomeButton");
                    if (homeBtn != null) winHomeButton = homeBtn.GetComponent<Button>();
                }
            }
        }

        public void ShowGameTypeMenu()
        {
            if (gameTypePanel != null) gameTypePanel.SetActive(true);
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(false);
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            HideGameplayUI();
            SetRollButtonInteractable(false);
        }

        private void OnGameTypeSelected(GameType type)
        {
            pendingGameType = type;

            if (gameTypePanel != null) gameTypePanel.SetActive(false);
            ShowBotPlayerSelection();
        }

        /// <summary>
        /// Shows the Bot&PlayerSelectionPanel. Used both when moving forward from
        /// GameTypeSelectionPanel, and when backing up from ModeSelectionPanel, and
        /// when hitting "Play Again" from the WinScreen (pendingGameType is left
        /// untouched in all of these cases, so whichever game type was last picked
        /// stays selected). The board/controls panel stay hidden here too - they
        /// only appear once a mode (2P/4P) is actually picked and the match starts.
        /// </summary>
        public void ShowBotPlayerSelection()
        {
            if (gameTypePanel != null) gameTypePanel.SetActive(false);
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(true);
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            HideGameplayUI(); // covers the "Play Again" case where a board was left showing from the previous match
            SetRollButtonInteractable(false);
        }

        /// <summary>
        /// Hides both board roots, the Snake&Ladder visuals/pawns, and the single
        /// shared ControlsPanel (which carries the Roll Dice and Home buttons) -
        /// i.e. everything that should only be visible while a match is actually
        /// being played.
        /// </summary>
        private void HideGameplayUI()
        {
            if (snakeLadderBoardRoot != null) snakeLadderBoardRoot.SetActive(false);
            if (ludoBoardRoot != null) ludoBoardRoot.SetActive(false);
            if (snakeLadderVisuals != null) snakeLadderVisuals.SetActive(false);
            if (snakeLadderPawns != null) snakeLadderPawns.SetActive(false);

            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (homeButton != null) homeButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the board matching "type" (and hides the other one), plus reveals
        /// the single shared ControlsPanel (same Roll Dice + Home buttons regardless
        /// of which game type is active - nothing game-specific left to toggle on it).
        /// Called only once a mode is actually picked and GameManager.StartGame() has run.
        /// </summary>
        private void ShowGameplayUI(GameType type)
        {
            bool isSnakeLadder = (type == GameType.SnakeAndLadder);
            if (snakeLadderBoardRoot != null) snakeLadderBoardRoot.SetActive(isSnakeLadder);
            if (ludoBoardRoot != null) ludoBoardRoot.SetActive(!isSnakeLadder);

            // Force these explicitly too - don't rely on the parent's active state alone,
            // in case they were individually left inactive in the scene.
            if (snakeLadderVisuals != null) snakeLadderVisuals.SetActive(isSnakeLadder);
            if (snakeLadderPawns != null) snakeLadderPawns.SetActive(isSnakeLadder);

            if (controlsPanel != null) controlsPanel.SetActive(true);

            // Home button is active whenever either board is active (i.e. a match is in progress) -
            // driven explicitly here rather than inheriting ControlsPanel's active state.
            if (homeButton != null) homeButton.gameObject.SetActive(true);
        }

        private void OnBotPlayerBackClicked()
        {
            // Bot&PlayerSelectionPanel -> GameTypeSelectionPanel
            ShowGameTypeMenu();
        }

        private void OnPlayerOrBotSelected(bool vsBot)
        {
            pendingVsBot = vsBot;
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(false);
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(true);
        }

        private void OnModeBackClicked()
        {
            // ModeSelectionPanel -> Bot&PlayerSelectionPanel
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
            if (playerOrBotPanel != null) playerOrBotPanel.SetActive(true);
        }

        private void OnModeSelected(GameMode mode)
        {
            if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);

            // Activate the board FIRST - pawns (and other board children) only run their
            // own Awake() once their GameObject is active, and SnakeLadderPawn relies on
            // Awake() having already set its rectTransform before SetBoard()/SnapToStart()
            // touches it. Calling StartGame() while the board is still inactive causes a
            // NullReferenceException inside ApplyStackTransform, which then aborts this
            // method before the board ever gets shown.
            ShowGameplayUI(pendingGameType);
            GameManager.Instance?.StartGame(pendingGameType, mode, pendingVsBot);
        }

        private void OnRollClicked()
        {
            Debug.Log("[UIManager] Roll button clicked.");
            GameManager.Instance?.RollDice();
        }

        /// <summary>
        /// Shared in-game Home button (in the shared ControlsPanel, used by both
        /// Ludo and Snake&Ladder) - abandons the current match and returns all the
        /// way to the GameTypeSelectionPanel.
        /// </summary>
        private void OnGameHomeClicked()
        {
            GameManager.Instance?.ReturnToMenu();
            ShowGameTypeMenu();
        }

        /// <summary>
        /// WinScreen Home button - same as the in-game Home button: full reset,
        /// back to the GameTypeSelectionPanel.
        /// </summary>
        private void OnWinHomeClicked()
        {
            GameManager.Instance?.ReturnToMenu();
            ShowGameTypeMenu();
        }

        /// <summary>
        /// WinScreen Play Again button - keeps the game type that was just played
        /// (pendingGameType is untouched) and drops straight into Bot&PlayerSelectionPanel,
        /// skipping GameTypeSelectionPanel.
        /// </summary>
        private void OnPlayAgainClicked()
        {
            GameManager.Instance?.ReturnToMenu();
            ShowBotPlayerSelection();
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