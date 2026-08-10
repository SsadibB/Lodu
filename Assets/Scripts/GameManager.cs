using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ludu.UI;

namespace Ludu.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum TurnState
        {
            MainMenu,
            WaitingForRoll,
            WaitingForPawnSelection, // Ludo only
            MovingPawn,
            GameOver
        }

        [Header("Match Settings")]
        [SerializeField] private GameType gameType = GameType.Ludo;
        [SerializeField] private GameMode gameMode = GameMode.TwoPlayer;

        [Header("Shared UI")]
        [SerializeField] private UIManager uiManager;

        // ---------------- LUDO ----------------
        [Header("Ludo References (assign in Inspector)")]
        [SerializeField] private BoardPath boardPath;
        [SerializeField] private Dice ludoDice;

        [Header("Ludo Pawns (assign in Inspector, 4 per color)")]
        [SerializeField] private List<Pawn> redPawns = new List<Pawn>();
        [SerializeField] private List<Pawn> greenPawns = new List<Pawn>();
        [SerializeField] private List<Pawn> yellowPawns = new List<Pawn>();
        [SerializeField] private List<Pawn> bluePawns = new List<Pawn>();

        [Header("Ludo Yard Placeholder Icons (shown only in 4-player mode)")]
        [SerializeField] private List<GameObject> greenYardPlaceholders = new List<GameObject>();
        [SerializeField] private List<GameObject> yellowYardPlaceholders = new List<GameObject>();

        // ------------ SNAKE & LADDER ------------
        [Header("Snake & Ladder References (assign in Inspector)")]
        [SerializeField] private SnakeAndLadderBoard slBoard;
        [SerializeField] private Dice slDice;

        [Header("Snake & Ladder Pawns - place all 4 pawn instances in the scene (e.g. nested under cell 1 / cell(0)) and drag each one into its color slot below.")]
        [SerializeField] private SnakeLadderPawn slRedPawn;
        [SerializeField] private SnakeLadderPawn slGreenPawn;
        [SerializeField] private SnakeLadderPawn slYellowPawn;
        [SerializeField] private SnakeLadderPawn slBluePawn;

        [Header("Bot AI")]
        [SerializeField] private float botMoveDelay = 1.6f;   // was 0.9f

        private HashSet<PlayerColor> botColors = new HashSet<PlayerColor>();

        public PlayerColor CurrentTurn { get; private set; } = PlayerColor.Red;
        public TurnState CurrentState { get; private set; } = TurnState.MainMenu;
        public int CurrentRollValue { get; private set; }
        public GameType CurrentGameType => gameType;

        private List<Pawn> _highlightedPawns = new List<Pawn>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public void StartGame(GameType type, GameMode mode, bool vsBot = false)
        {
            gameType = type;
            gameMode = mode;
            CurrentTurn = PlayerColor.Red;
            CurrentState = TurnState.WaitingForRoll;
            CurrentRollValue = 0;

            botColors.Clear();
            if (vsBot)
                foreach (var color in GetActivePlayers())
                    if (color != PlayerColor.Red) botColors.Add(color);

            SubscribeDice(type);

            if (type == GameType.Ludo)
                StartLudoGame();
            else
                StartSnakeLadderGame();

            UpdateUI();
            Debug.Log($"[GameManager] Game started! Type: {type}, Mode: {mode}, VsBot: {vsBot}, Turn: {CurrentTurn}");
        }

        private void SubscribeDice(GameType type)
        {
            // Always unsubscribe both first so switching game type mid-session never double-fires.
            if (ludoDice != null) ludoDice.OnDiceRolled -= OnLudoDiceRolled;
            if (slDice != null) slDice.OnDiceRolled -= OnSlDiceRolled;

            if (type == GameType.Ludo)
            {
                if (ludoDice != null) ludoDice.OnDiceRolled += OnLudoDiceRolled;
                else Debug.LogError("[GameManager] Ludo Dice not assigned in Inspector!");
            }
            else
            {
                if (slDice != null) slDice.OnDiceRolled += OnSlDiceRolled;
                else Debug.LogError("[GameManager] Snake&Ladder Dice not assigned in Inspector!");
            }
        }

        public void RollDice()
        {
            if (CurrentState != TurnState.WaitingForRoll) { Debug.Log($"[GameManager] Can't roll: state={CurrentState}"); return; }

            uiManager?.SetRollButtonInteractable(false);

            if (gameType == GameType.Ludo)
            {
                if (ludoDice == null) { Debug.LogError("[GameManager] Ludo Dice is null!"); return; }
                ludoDice.RollDice();
            }
            else
            {
                if (slDice == null) { Debug.LogError("[GameManager] Snake&Ladder Dice is null!"); return; }
                slDice.RollDice();
            }
        }

        private void MaybeTriggerBotTurn()
        {
            if (CurrentState != TurnState.WaitingForRoll) return;
            if (!IsBotTurn(CurrentTurn)) return;
            Invoke(nameof(BotRoll), botMoveDelay);
        }

        private void BotRoll()
        {
            if (CurrentState != TurnState.WaitingForRoll || !IsBotTurn(CurrentTurn)) return;
            RollDice();
        }

        private bool IsBotTurn(PlayerColor color) => botColors.Contains(color);

        private void SwitchTurn()
        {
            ClearHighlights();
            var activePlayers = GetActivePlayers();
            int next = (activePlayers.IndexOf(CurrentTurn) + 1) % activePlayers.Count;
            CurrentTurn = activePlayers[next];
            CurrentState = TurnState.WaitingForRoll;
            CurrentRollValue = 0;
            UpdateUI();
        }

        private List<PlayerColor> GetActivePlayers()
        {
            if (gameMode == GameMode.TwoPlayer)
                return new List<PlayerColor> { PlayerColor.Red, PlayerColor.Blue };
            return new List<PlayerColor> { PlayerColor.Red, PlayerColor.Green, PlayerColor.Blue, PlayerColor.Yellow };
        }

        private void UpdateUI()
        {
            uiManager?.UpdateTurnIndicator(CurrentTurn);
            uiManager?.UpdateDiceValue(0);
            uiManager?.SetRollButtonInteractable(CurrentState == TurnState.WaitingForRoll && !IsBotTurn(CurrentTurn));
            MaybeTriggerBotTurn();
        }

        // =========================================================
        //                         LUDO
        // =========================================================

        private void StartLudoGame()
        {
            if (boardPath == null) Debug.LogError("[GameManager] BoardPath not assigned in Inspector!");

            InitializePlayerPawns(PlayerColor.Red, redPawns, true);
            InitializePlayerPawns(PlayerColor.Blue, bluePawns, true);
            bool enable4P = (gameMode == GameMode.FourPlayer);
            InitializePlayerPawns(PlayerColor.Green, greenPawns, enable4P);
            InitializePlayerPawns(PlayerColor.Yellow, yellowPawns, enable4P);

            SetPlaceholdersActive(greenYardPlaceholders, enable4P);
            SetPlaceholdersActive(yellowYardPlaceholders, enable4P);
        }

        private void InitializePlayerPawns(PlayerColor color, List<Pawn> pawns, bool isActive)
        {
            List<TileNode> track = boardPath != null ? boardPath.GetFullTrackForPlayer(color) : new List<TileNode>();

            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] == null) continue;
                pawns[i].gameObject.SetActive(isActive);
                if (isActive)
                {
                    TileNode yardNode = boardPath != null ? boardPath.GetYardNode(color, i) : null;
                    pawns[i].Initialize(color, i, yardNode != null ? yardNode.transform : null, track);
                }
            }
        }

        private void SetPlaceholdersActive(List<GameObject> placeholders, bool active)
        {
            if (placeholders == null) return;
            foreach (var placeholder in placeholders)
                if (placeholder != null) placeholder.SetActive(active);
        }

        private IEnumerator BotPlayPawnAfterDelay(Pawn pawn)
        {
            yield return new WaitForSeconds(botMoveDelay);
            if (CurrentState == TurnState.WaitingForPawnSelection && pawn.Color == CurrentTurn)
                OnPawnClicked(pawn);
        }

        private Pawn ChooseBotMove(List<Pawn> moveablePawns, int roll)
        {
            Pawn best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var pawn in moveablePawns)
            {
                float score = EvaluateMove(pawn, roll);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = pawn;
                }
            }
            return best;
        }

        private float EvaluateMove(Pawn pawn, int roll)
        {
            TileNode destination = pawn.PeekTileAt(roll);
            float score = pawn.CurrentPathIndex + roll;

            if (destination == null) return score;

            if (destination.Type == TileType.HomeGoal)
                score += 1000f;

            if (pawn.IsInYard && roll == 6)
                score += 120f;

            if (destination.Type == TileType.Normal)
            {
                List<Pawn> occupants = GetPawnsOnTile(destination, pawn);
                List<Pawn> enemiesThere = occupants.FindAll(p => p.Color != pawn.Color);

                if (enemiesThere.Count == 1)
                    score += 800f;
                else if (enemiesThere.Count >= 2)
                    score -= 50f;

                if (IsTileThreatenedByEnemy(destination, pawn.Color))
                    score -= 300f;
            }
            else if (destination.Type == TileType.Safe || destination.Type == TileType.StartTile)
            {
                score += 150f;
            }
            else if (destination.Type == TileType.HomePath)
            {
                score += 250f;
            }

            return score;
        }

        private bool IsTileThreatenedByEnemy(TileNode tile, PlayerColor movingColor)
        {
            if (boardPath == null) return false;
            var common = boardPath.CommonPathNodes;

            int tileIndex = -1;
            for (int i = 0; i < common.Count; i++)
                if (common[i] == tile) { tileIndex = i; break; }
            if (tileIndex == -1) return false;

            foreach (var color in GetActivePlayers())
            {
                if (color == movingColor) continue;
                foreach (var enemy in GetPawnsForPlayer(color))
                {
                    if (enemy == null || !enemy.gameObject.activeSelf || enemy.IsInYard || enemy.CurrentTile == null) continue;

                    int enemyIndex = -1;
                    for (int i = 0; i < common.Count; i++)
                        if (common[i] == enemy.CurrentTile) { enemyIndex = i; break; }
                    if (enemyIndex == -1) continue;

                    int distance = (tileIndex - enemyIndex + common.Count) % common.Count;
                    if (distance >= 1 && distance <= 6)
                        return true;
                }
            }
            return false;
        }

        private void OnLudoDiceRolled(int rollValue)
        {
            CurrentRollValue = rollValue;
            uiManager?.UpdateDiceValue(rollValue);

            List<Pawn> activePawns = GetPawnsForPlayer(CurrentTurn);
            List<Pawn> moveablePawns = GetMoveablePawns(activePawns, CurrentRollValue);

            Debug.Log($"[GameManager] Rolled {rollValue}. Moveable pawns: {moveablePawns.Count}");

            if (moveablePawns.Count == 0)
            {
                Invoke(nameof(SwitchTurn), 1.2f);
            }
            else
            {
                CurrentState = TurnState.WaitingForPawnSelection;
                HighlightPawns(moveablePawns);

                if (IsBotTurn(CurrentTurn))
                {
                    Pawn botChoice = ChooseBotMove(moveablePawns, CurrentRollValue);
                    if (botChoice != null)
                        StartCoroutine(BotPlayPawnAfterDelay(botChoice));
                }
            }
        }

        public void OnPawnClicked(Pawn pawn)
        {
            if (CurrentState != TurnState.WaitingForPawnSelection) return;
            if (pawn.Color != CurrentTurn) return;
            if (!pawn.CanMove(CurrentRollValue)) return;

            TileNode originTile = pawn.CurrentTile;
            ClearHighlights();
            CurrentState = TurnState.MovingPawn;
            pawn.MovePawn(CurrentRollValue, () => OnPawnMoveCompleted(pawn, originTile));
        }

        private void OnPawnMoveCompleted(Pawn movedPawn, TileNode originTile)
        {
            RefreshTileStack(originTile);
            RefreshTileStack(movedPawn.CurrentTile);

            bool captured = CheckCaptures(movedPawn);

            if (CheckWinCondition(CurrentTurn))
            {
                CurrentState = TurnState.GameOver;
                uiManager?.ShowWinScreen(CurrentTurn);
                return;
            }

            if (CurrentRollValue == 6 || captured)
            {
                CurrentState = TurnState.WaitingForRoll;
                UpdateUI();
            }
            else
            {
                SwitchTurn();
            }
        }

        private bool CheckCaptures(Pawn movedPawn)
        {
            if (movedPawn.CurrentTile == null) return false;
            if (movedPawn.CurrentTile.Type != TileType.Normal) return false;

            bool didCapture = false;
            foreach (var color in GetActivePlayers())
            {
                if (color == movedPawn.Color) continue;

                List<Pawn> oppPawnsOnTile = new List<Pawn>();
                foreach (var oppPawn in GetPawnsForPlayer(color))
                {
                    if (oppPawn.IsInYard) continue;
                    if (oppPawn.CurrentTile == movedPawn.CurrentTile)
                        oppPawnsOnTile.Add(oppPawn);
                }

                if (oppPawnsOnTile.Count == 0) continue;
                if (oppPawnsOnTile.Count >= 2) continue; // protected block

                oppPawnsOnTile[0].CapturedReturnToYard(() => RefreshTileStack(movedPawn.CurrentTile));
                didCapture = true;
            }
            return didCapture;
        }

        private bool CheckWinCondition(PlayerColor color)
        {
            var pawns = GetPawnsForPlayer(color);
            if (pawns == null || pawns.Count == 0) return false;
            foreach (var p in pawns) if (!p.IsFinished) return false;
            return true;
        }

        private List<Pawn> GetPawnsForPlayer(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return redPawns;
                case PlayerColor.Green: return greenPawns;
                case PlayerColor.Yellow: return yellowPawns;
                case PlayerColor.Blue: return bluePawns;
                default: return redPawns;
            }
        }

        private List<Pawn> GetMoveablePawns(List<Pawn> pawns, int roll)
        {
            var result = new List<Pawn>();
            foreach (var p in pawns)
                if (p != null && p.CanMove(roll)) result.Add(p);
            return result;
        }

        private void HighlightPawns(List<Pawn> pawns)
        {
            ClearHighlights();
            _highlightedPawns = pawns;
            foreach (var p in _highlightedPawns)
                if (p != null) p.SetHighlighted(true);
        }

        private void ClearHighlights()
        {
            foreach (var p in _highlightedPawns)
                if (p != null) p.SetHighlighted(false);
            _highlightedPawns.Clear();
        }

        private void RefreshTileStack(TileNode tile)
        {
            if (tile == null) return;
            foreach (var pawn in GetPawnsOnTile(tile, null))
                pawn.RefreshStackVisual();
        }

        public List<Pawn> GetPawnsOnTile(TileNode tile, Pawn exclude = null)
        {
            var result = new List<Pawn>();
            if (tile == null) return result;

            foreach (var list in new[] { redPawns, greenPawns, yellowPawns, bluePawns })
            {
                foreach (var p in list)
                {
                    if (p == null || p == exclude) continue;
                    if (!p.gameObject.activeSelf) continue;
                    if (p.IsInYard) continue;
                    if (p.CurrentTile == tile) result.Add(p);
                }
            }
            return result;
        }

        // =========================================================
        //                    SNAKE & LADDER
        // =========================================================

        private void StartSnakeLadderGame()
        {
            if (slBoard == null) Debug.LogError("[GameManager] Snake&Ladder Board not assigned in Inspector!");

            bool enable4P = (gameMode == GameMode.FourPlayer);

            SetSlPawnActive(slRedPawn, true);
            SetSlPawnActive(slBluePawn, true);
            SetSlPawnActive(slGreenPawn, enable4P);
            SetSlPawnActive(slYellowPawn, enable4P);

            // Pawns are scene instances - their `board` field is normally already wired via
            // Inspector drag-and-drop, but this covers you if it wasn't.
            slRedPawn?.SetBoard(slBoard);
            slBluePawn?.SetBoard(slBoard);
            slGreenPawn?.SetBoard(slBoard);
            slYellowPawn?.SetBoard(slBoard);

            // Snap each active pawn onto cell 1 in turn, so later pawns see earlier
            // ones already there and fan out correctly from the start.
            foreach (var color in GetActivePlayers())
                GetSlPawnForColor(color)?.SnapToStart();
        }

        private void SetSlPawnActive(SnakeLadderPawn pawn, bool active)
        {
            if (pawn != null) pawn.gameObject.SetActive(active);
        }

        private void OnSlDiceRolled(int rollValue)
        {
            CurrentRollValue = rollValue;
            uiManager?.UpdateDiceValue(rollValue);

            SnakeLadderPawn pawn = GetSlPawnForColor(CurrentTurn);
            if (pawn == null)
            {
                Debug.LogWarning($"[GameManager] No Snake&Ladder pawn assigned for {CurrentTurn}, skipping turn.");
                SwitchTurn();
                return;
            }

            int originCell = pawn.CurrentCell;
            CurrentState = TurnState.MovingPawn;
            pawn.Move(rollValue, () => OnSlPawnMoveCompleted(pawn, originCell));
        }

        private void OnSlPawnMoveCompleted(SnakeLadderPawn pawn, int originCell)
        {
            RefreshSlCellStack(originCell);
            RefreshSlCellStack(pawn.CurrentCell);

            if (pawn.HasWon)
            {
                CurrentState = TurnState.GameOver;
                uiManager?.ShowWinScreen(CurrentTurn);
                return;
            }

            if (CurrentRollValue == 6)
            {
                // Extra turn: applies whether the 6 unlocked the pawn or moved it forward.
                CurrentState = TurnState.WaitingForRoll;
                UpdateUI();
            }
            else
            {
                SwitchTurn();
            }
        }

        private void RefreshSlCellStack(int cell)
        {
            if (cell <= 0) return;
            foreach (var pawn in GetSlPawnsOnCell(cell, null))
                pawn.RefreshStackVisual();
        }

        /// <summary>
        /// Returns every active Snake&amp;Ladder pawn currently sitting on the given cell,
        /// excluding "exclude". Used by SnakeLadderPawn to compute its stacking slot.
        /// </summary>
        public List<SnakeLadderPawn> GetSlPawnsOnCell(int cell, SnakeLadderPawn exclude)
        {
            var result = new List<SnakeLadderPawn>();
            foreach (var p in new[] { slRedPawn, slGreenPawn, slYellowPawn, slBluePawn })
            {
                if (p == null || p == exclude) continue;
                if (!p.gameObject.activeSelf) continue;
                if (p.CurrentCell == cell) result.Add(p);
            }
            return result;
        }

        private SnakeLadderPawn GetSlPawnForColor(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return slRedPawn;
                case PlayerColor.Green: return slGreenPawn;
                case PlayerColor.Yellow: return slYellowPawn;
                case PlayerColor.Blue: return slBluePawn;
                default: return null;
            }
        }
    }
}