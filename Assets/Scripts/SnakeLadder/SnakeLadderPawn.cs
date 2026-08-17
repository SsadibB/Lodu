using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Ludu.Core
{
    /// <summary>
    /// A pawn on the Snake &amp; Ladder board. All pawns start stacked on cell 1 and stay
    /// locked there until their player rolls the entry value (default 6) - that roll
    /// unlocks the pawn but doesn't move it (it's already sitting on cell 1). From then
    /// on, any roll moves it forward normally, auto-resolving snakes/ladders.
    /// When 2+ pawns share a cell, they auto-shrink and fan out - same pattern as the
    /// Ludo Pawn's stacking visuals.
    /// </summary>
    public class SnakeLadderPawn : MonoBehaviour
    {
        [Header("Board")]
        [Tooltip("Leave empty if this pawn is a prefab - assign via SetBoard(...) at spawn time instead, since prefab assets cannot hold scene references.")]
        [SerializeField] private SnakeAndLadderBoard board;

        [Header("Entry Rule")]
        [Tooltip("Roll value required to unlock this pawn from cell 1 and start moving. Classic rule is 6.")]
        [SerializeField] private int entryRoll = 6;

        [Header("Stacking (multiple pawns sharing one cell)")]
        [SerializeField] private float stackOffsetDistance = 14f;
        [SerializeField] private float stackedScaleMultiplier = 0.65f;

        [Tooltip("Fraction of the normal corner-fan spacing used only for the pawns' starting positions on cell 1, so they sit together in the middle instead of spread to the corners. 0 = perfectly centered/stacked on top of each other; 1 = same spread as normal mid-game stacking.")]
        [SerializeField] private float startStackOffsetScale = 0.25f;

        [Header("Step Hop Animation")]
        [SerializeField] private float stepDuration = 0.28f;
        [SerializeField] private float jumpPower = 15f;

        [Header("Ladder Climb / Snake Slide Animation")]
        [SerializeField] private float connectorPauseDuration = 0.35f;
        [SerializeField] private float ladderClimbDuration = 0.8f;
        [SerializeField] private float snakeSlideDuration = 0.7f;

        public int CurrentCell { get; private set; } = 1;
        public bool HasEntered { get; private set; } = false; // true once unlocked and free to move
        public bool IsMoving { get; private set; }
        public bool HasWon => board != null && CurrentCell >= board.TotalCells;

        private Coroutine moveCoroutine;
        private bool hasSnapped;
        private Vector3 baseScale = Vector3.one;
        private Vector3 targetStackScale = Vector3.one;
        private Tween stackTween;
        private RectTransform _rectTransform;
        private bool baseScaleCaptured;

        /// <summary>
        /// Lazily resolved instead of only being cached in Awake(). GameManager calls
        /// SetBoard()/SnapToStart() on pawns that may still be inactive (e.g. Green/Yellow
        /// in a 2-player game) - an inactive GameObject's Awake() hasn't run yet, so a
        /// plain Awake-only cache would still be null here and throw a NullReferenceException.
        /// </summary>
        private RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }

        private void Awake()
        {
            CaptureBaseScaleIfNeeded();
            _rectTransform = transform as RectTransform;
        }

        /// <summary>
        /// Grabs the pawn's authored (prefab/editor) scale exactly once, before anything
        /// ever rescales it for stacking. Must run even if this pawn is still inactive when
        /// SnapToStart()/ApplyStackTransform() first fire (e.g. an unused Green/Yellow pawn
        /// in a 2-player game) - otherwise Awake() hasn't captured it yet and the stack math
        /// would fall back to a default of (1,1,1) instead of the real authored scale.
        /// </summary>
        private void CaptureBaseScaleIfNeeded()
        {
            if (baseScaleCaptured) return;
            baseScale = transform.localScale;
            targetStackScale = baseScale;
            baseScaleCaptured = true;
        }

        private void Start()
        {
            if (!hasSnapped)
                SnapToStart();
        }

        /// <summary>
        /// Assigns the board reference at runtime. Required for prefab-spawned pawns,
        /// since prefab assets cannot hold direct references to scene objects.
        /// </summary>
        public void SetBoard(SnakeAndLadderBoard boardInstance)
        {
            board = boardInstance;
            if (!hasSnapped)
                SnapToStart();
        }

        /// <summary>Places the pawn on cell 1, locked, shrunk and tucked in tight near the middle of that cell alongside any other pawns already there.</summary>
        public void SnapToStart()
        {
            CaptureBaseScaleIfNeeded();
            HasEntered = false;
            CurrentCell = 1;

            ApplyStackTransform(1, animate: false);
            hasSnapped = true;
        }

        /// <summary>Instantly places the pawn directly on a board cell, unlocked. Useful for manual testing/resets.</summary>
        public void SnapToCell(int cell)
        {
            CaptureBaseScaleIfNeeded();
            int max = board != null ? board.TotalCells : 100;
            HasEntered = true;
            CurrentCell = Mathf.Clamp(cell, 1, max);

            ApplyStackTransform(CurrentCell, animate: false);
            hasSnapped = true;
        }

        /// <summary>
        /// Moves this pawn by <paramref name="steps"/> (a dice roll). While locked on cell 1,
        /// only a roll matching entryRoll (default 6) unlocks it - no hop happens since it's
        /// already there. Once unlocked, any roll moves it forward; overshooting the last
        /// cell forfeits the turn.
        /// </summary>
        public void Move(int steps, Action onComplete = null)
        {
            if (IsMoving || board == null)
            {
                if (board == null)
                    Debug.LogWarning("[SnakeLadderPawn] Move() called but board is not assigned. Call SetBoard() first.");
                onComplete?.Invoke();
                return;
            }

            if (!HasEntered)
            {
                if (steps != entryRoll)
                {
                    Debug.Log($"[SnakeLadderPawn] Needs a {entryRoll} to unlock - rolled {steps}, staying put.");
                    onComplete?.Invoke();
                    return;
                }

                HasEntered = true;
                Debug.Log("[SnakeLadderPawn] Unlocked! Free to move on future rolls.");
                RefreshStackVisual();
                onComplete?.Invoke();
                return;
            }

            int maxCell = board.TotalCells;
            int target = CurrentCell + steps;

            if (target > maxCell)
            {
                Debug.Log($"[SnakeLadderPawn] Roll of {steps} overshoots cell {maxCell} from {CurrentCell} - no move this turn.");
                onComplete?.Invoke();
                return;
            }

            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveRoutine(target, onComplete));
        }

        private IEnumerator MoveRoutine(int target, Action onComplete)
        {
            IsMoving = true;

            for (int cell = CurrentCell + 1; cell <= target; cell++)
            {
                bool isFinalStep = (cell == target);
                // Pass-through cells: hop to exact center. Final landing cell: hop into a stacking slot.
                Vector3 hopTarget = isFinalStep
                    ? GetLandingPosition(cell)
                    : GetCellCenterForThisPawn(cell);

                yield return Hop(hopTarget, stepDuration);
                CurrentCell = cell;
            }

            if (board.TryGetConnector(CurrentCell, out SnakeOrLadderConnector connector))
                yield return ResolveConnector(connector);

            IsMoving = false;
            moveCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator ResolveConnector(SnakeOrLadderConnector connector)
        {
            yield return new WaitForSeconds(connectorPauseDuration);

            Vector3 destTarget = GetLandingPosition(connector.toCell);

            if (connector.IsLadder)
            {
                Debug.Log($"[SnakeLadderPawn] Climbing ladder {connector.fromCell} -> {connector.toCell}");
                yield return ClimbLadder(destTarget);
            }
            else
            {
                Debug.Log($"[SnakeLadderPawn] Sliding down snake {connector.fromCell} -> {connector.toCell}");
                yield return SlideDownSnake(destTarget);
            }

            CurrentCell = connector.toCell;
        }

        private IEnumerator Hop(Vector3 targetPos, float duration)
        {
            bool done = false;
            transform.DOJump(targetPos, jumpPower, 1, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => done = true);
            while (!done) yield return null;
        }

        private IEnumerator ClimbLadder(Vector3 targetPos)
        {
            bool done = false;
            transform.DOJump(targetPos, 40f, 1, ladderClimbDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => done = true);
            while (!done) yield return null;
        }

        private IEnumerator SlideDownSnake(Vector3 targetPos)
        {
            bool done = false;
            transform.DOMove(targetPos, snakeSlideDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => done = true);
            while (!done) yield return null;
        }

        /// <summary>
        /// World-space center of a RectTransform, regardless of its own pivot setting.
        /// `.position` only equals the center when pivot is (0.5, 0.5) - this project's cells
        /// use pivot (0,0), so we derive the true center from the rect's corners instead.
        /// </summary>
        private static Vector3 GetRectCenterWorld(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners); // [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right
            return (corners[0] + corners[2]) * 0.5f;
        }

        /// <summary>
        /// How far this pawn's own pivot sits from ITS visual center, in world units. Since
        /// transform.position always places the pivot (not the visual center) at a given world
        /// point, we need this to make the pawn's actual body land on the cell's center - works
        /// for any pivot value (0,0 / 0.5,0.5 / 1,1 / anything), so the pawn's Inspector pivot
        /// never has to be touched. Pass the scale the pawn is about to have (e.g. the new
        /// stacked scale) when computing a position that will apply alongside a scale change,
        /// so the offset matches the pawn's size AFTER that change rather than before it.
        /// </summary>
        private Vector3 GetOwnPivotToCenterOffset(Vector3? scaleOverride = null)
        {
            if (rectTransform == null) return Vector3.zero;
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            Vector3 scale = scaleOverride ?? rectTransform.lossyScale;
            return new Vector3((0.5f - pivot.x) * size.x * scale.x, (0.5f - pivot.y) * size.y * scale.y, 0f);
        }

        /// <summary>Plain center of a board cell, corrected for this pawn's own pivot - no stacking offset.</summary>
        private Vector3 GetCellCenterForThisPawn(int cell)
        {
            RectTransform cellTransform = board != null ? board.GetCellTransform(cell) : null;
            if (cellTransform == null) return transform.position;
            return GetRectCenterWorld(cellTransform) + GetOwnPivotToCenterOffset();
        }

        /// <summary>
        /// Works out where on a cell this pawn should land: dead center if it's the only
        /// pawn there, or fanned into a corner slot if others already share that cell.
        /// </summary>
        private Vector3 GetLandingPosition(int cell)
        {
            RectTransform cellTransform = board != null ? board.GetCellTransform(cell) : null;
            if (cellTransform == null) return transform.position;

            Vector3 center = GetRectCenterWorld(cellTransform) + GetOwnPivotToCenterOffset();
            if (GameManager.Instance == null) return center;

            List<SnakeLadderPawn> others = GameManager.Instance.GetSlPawnsOnCell(cell, this);
            if (others.Count == 0) return center; // alone on this cell - no fan needed

            Vector2 offset = GetStackSlotOffset(others.Count);
            return center + (Vector3)offset;
        }

        /// <summary>
        /// Re-settles this pawn into its correct stacking slot (position + size) based on
        /// how many pawns are on its current cell right now. Called by GameManager whenever
        /// the stack on a cell might have changed - a pawn arrived, left, or unlocked.
        /// </summary>
        public void RefreshStackVisual()
        {
            if (moveCoroutine != null) return; // don't fight an in-progress hop
            ApplyStackTransform(CurrentCell, animate: true);
        }

        /// <summary>
        /// Shared math for both the initial spawn (SnapToStart/SnapToCell, instant) and
        /// mid-game restacking (RefreshStackVisual, animated): counts how many active
        /// pawns share this cell right now and settles this pawn into the correct
        /// shrunk + fanned slot, or dead center if it's alone.
        /// Cell 1 always targets the board's own first-cell rect (never a separate marker),
        /// using the tighter startStackOffsetScale spacing so multiple pawns sit tucked
        /// together in its middle - this applies to every caller that targets cell 1, not
        /// just SnapToStart, so a pawn parked there stays visually put (no jump) even when
        /// RefreshStackVisual() fires on the entry-roll unlock, since CurrentCell is still 1
        /// at that point.
        /// </summary>
        private void ApplyStackTransform(int cell, bool animate)
        {
            RectTransform cellTransform = board != null ? board.GetCellTransform(cell) : null;
            if (cellTransform == null) return;

            if (!animate)
            {
                // Instant snaps (SnapToStart/SnapToCell) can fire before a pending Layout
                // Group (e.g. Grid Layout Group on Snake&LadderBoardContainer) has arranged
                // its cells - Unity defers that rebuild to end-of-frame. Force it now so
                // cellTransform's rect/anchoredPosition is already settled before we read it.
                if (cellTransform.parent is RectTransform parentRect)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            Debug.Log($"[SLPawn-DEBUG] {name}: cell {cell} -> rect '{cellTransform.name}' " +
                      $"worldCenter={GetRectCenterWorld(cellTransform)} " +
                      $"localPos={cellTransform.localPosition} anchoredPos={cellTransform.anchoredPosition} " +
                      $"lossyScale={cellTransform.lossyScale} parentChain={DescribeParentChain(cellTransform)}");

            float offsetScale = cell == 1 ? startStackOffsetScale : 1f;

            int totalCount = 1;
            int myIndex = 0;
            if (GameManager.Instance != null)
            {
                List<SnakeLadderPawn> allOnCell = GameManager.Instance.GetSlPawnsOnCell(cell, null); // includes self
                totalCount = allOnCell.Count;
                myIndex = Mathf.Max(0, allOnCell.IndexOf(this));
            }

            targetStackScale = totalCount >= 2 ? baseScale * stackedScaleMultiplier : baseScale;
            Vector2 offset = totalCount >= 2 ? GetStackSlotOffset(myIndex) * offsetScale : Vector2.zero;

            // Pivot correction must reflect the scale the pawn is ABOUT to have, not its
            // current one, or the final landed position drifts slightly whenever the stack
            // count changes (e.g. a pawn joining/leaving triggers a scale change).
            Vector3 currentLocal = transform.localScale;
            Vector3 parentLossy = new Vector3(
                Mathf.Abs(currentLocal.x) > 0.0001f ? rectTransform.lossyScale.x / currentLocal.x : 1f,
                Mathf.Abs(currentLocal.y) > 0.0001f ? rectTransform.lossyScale.y / currentLocal.y : 1f,
                1f);
            Vector3 newLossyScale = new Vector3(parentLossy.x * targetStackScale.x, parentLossy.y * targetStackScale.y, 1f);

            Vector3 targetPos = GetRectCenterWorld(cellTransform) + GetOwnPivotToCenterOffset(newLossyScale) + (Vector3)offset;

            stackTween?.Kill();
            if (animate)
            {
                Sequence seq = DOTween.Sequence();
                seq.Join(transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad));
                seq.Join(transform.DOScale(targetStackScale, 0.2f).SetEase(Ease.OutQuad));
                stackTween = seq;
            }
            else
            {
                transform.position = targetPos;
                transform.localScale = targetStackScale;
            }
        }

        private static string DescribeParentChain(Transform t)
        {
            var sb = new System.Text.StringBuilder();
            Transform current = t;
            while (current != null)
            {
                sb.Append($"[{current.name}: localScale={current.localScale}, localRot={current.localEulerAngles}] <- ");
                current = current.parent;
            }
            return sb.ToString();
        }

        private Vector2 GetStackSlotOffset(int slotIndex)
        {
            float d = stackOffsetDistance;
            switch (slotIndex % 4)
            {
                case 0: return new Vector2(-d, d);
                case 1: return new Vector2(d, d);
                case 2: return new Vector2(-d, -d);
                default: return new Vector2(d, -d);
            }
        }
    }
}