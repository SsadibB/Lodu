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
            WaitingForPawnSelection,
            MovingPawn,
            GameOver
        }

        [Header("Match Settings")]
        [SerializeField] private GameMode gameMode = GameMode.TwoPlayer;

        [Header("References (assign in Inspector)")]
        [SerializeField] private BoardPath boardPath;
        [SerializeField] private Dice dice;
        [SerializeField] private UIManager uiManager;

        [Header("Pawns (assign in Inspector, 4 per color)")]
        [SerializeField] private List<Pawn> redPawns = new List<Pawn>();
        [SerializeField] private List<Pawn> greenPawns = new List<Pawn>();
        [SerializeField] private List<Pawn> yellowPawns = new List<Pawn>();
        [SerializeField] private List<Pawn> bluePawns = new List<Pawn>();

        public PlayerColor CurrentTurn { get; private set; } = PlayerColor.Red;
        public TurnState CurrentState { get; private set; } = TurnState.MainMenu;
        public int CurrentRollValue { get; private set; }

        private List<Pawn> _highlightedPawns = new List<Pawn>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (boardPath == null) Debug.LogError("[GameManager] BoardPath not assigned in Inspector!");
            if (dice == null) Debug.LogError("[GameManager] Dice not assigned in Inspector!");
            if (uiManager == null) Debug.LogError("[GameManager] UIManager not assigned in Inspector!");
        }

        private void Start()
        {
            if (dice != null)
            {
                dice.OnDiceRolled -= OnDiceRolled; // prevent double subscribe
                dice.OnDiceRolled += OnDiceRolled;
            }
            else
            {
                Debug.LogError("[GameManager] Dice component not found! Make sure Dice is in the scene.");
            }
        }

        public void StartGame(GameMode mode)
        {
            gameMode = mode;
            CurrentTurn = PlayerColor.Red;
            CurrentState = TurnState.WaitingForRoll;

            InitializePlayerPawns(PlayerColor.Red, redPawns, true);
            InitializePlayerPawns(PlayerColor.Blue, bluePawns, true);
            bool enable4P = (gameMode == GameMode.FourPlayer);
            InitializePlayerPawns(PlayerColor.Green, greenPawns, enable4P);
            InitializePlayerPawns(PlayerColor.Yellow, yellowPawns, enable4P);

            UpdateUI();
            Debug.Log($"[GameManager] Game started! Mode: {mode}, Turn: {CurrentTurn}");
        }

        private void InitializePlayerPawns(PlayerColor color, List<Pawn> pawns, bool isActive)
        {
            List<TileNode> track = boardPath != null ? boardPath.GetFullTrackForPlayer(color) : new List<TileNode>();
            List<TileNode> yardNodes = boardPath != null ? boardPath.GetYardNodes(color) : new List<TileNode>();

            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] == null) continue;
                pawns[i].gameObject.SetActive(isActive);
                if (isActive)
                {
                    Transform yardSlot = (i < yardNodes.Count && yardNodes[i] != null) ? yardNodes[i].transform : null;
                    pawns[i].Initialize(color, i, yardSlot, track);
                }
            }
        }

        public void RollDice()
        {
            if (CurrentState != TurnState.WaitingForRoll) { Debug.Log($"[GameManager] Can't roll: state={CurrentState}"); return; }
            if (dice == null) { Debug.LogError("[GameManager] Dice is null!"); return; }
            uiManager?.SetRollButtonInteractable(false);
            dice.RollDice();
        }

        private void OnDiceRolled(int rollValue)
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
            }
        }

        public void OnPawnClicked(Pawn pawn)
        {
            if (CurrentState != TurnState.WaitingForPawnSelection) return;
            if (pawn.Color != CurrentTurn) return;
            if (!pawn.CanMove(CurrentRollValue)) return;

            ClearHighlights();
            CurrentState = TurnState.MovingPawn;
            pawn.MovePawn(CurrentRollValue, () => OnPawnMoveCompleted(pawn));
        }

        private void OnPawnMoveCompleted(Pawn movedPawn)
        {
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
            if (movedPawn.CurrentTile == null)
            {
                Debug.Log($"[CheckCaptures] {movedPawn.Color} pawn has no CurrentTile — skipping capture check.");
                return false;
            }

            Debug.Log($"[CheckCaptures] {movedPawn.Color} pawn landed on '{movedPawn.CurrentTile.name}' (type={movedPawn.CurrentTile.Type}).");

            if (movedPawn.CurrentTile.Type != TileType.Normal)
            {
                Debug.Log($"[CheckCaptures] Tile is {movedPawn.CurrentTile.Type} — kills only happen on Normal tiles, skipping.");
                return false;
            }

            bool didCapture = false;
            foreach (var color in GetActivePlayers())
            {
                if (color == movedPawn.Color) continue;
                foreach (var oppPawn in GetPawnsForPlayer(color))
                {
                    if (oppPawn.IsInYard) continue;

                    string oppTileName = oppPawn.CurrentTile != null ? oppPawn.CurrentTile.name : "null";
                    bool sameTile = oppPawn.CurrentTile == movedPawn.CurrentTile;
                    Debug.Log($"[CheckCaptures] Comparing against {color} pawn on '{oppTileName}' — sameTile={sameTile}");

                    if (sameTile)
                    {
                        oppPawn.CapturedReturnToYard();
                        didCapture = true;
                        Debug.Log($"[CheckCaptures] CAPTURED {color} pawn!");
                    }
                }
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
            return new List<PlayerColor> { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };
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

        private void UpdateUI()
        {
            uiManager?.UpdateTurnIndicator(CurrentTurn);
            uiManager?.UpdateDiceValue(0);
            uiManager?.SetRollButtonInteractable(CurrentState == TurnState.WaitingForRoll);
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

        /// <summary>
        /// Returns every pawn (of any color) currently standing on the given tile,
        /// excluding "exclude" itself. Used by Pawn to figure out its stacking slot
        /// when multiple pawns share the same tile (e.g. a safe tile).
        /// </summary>
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
    }
}