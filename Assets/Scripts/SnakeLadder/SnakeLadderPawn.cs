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
        [SerializeField] private float stackOffsetDistance = 20f;
        [SerializeField] private float stackedScaleMultiplier = 0.65f;

        [Tooltip("Distance (in the same units as Stack Offset Distance) each pawn is pushed off-center at the very start on cell 1, so all 4 sit in their own visible corner instead of overlapping and hiding one another. 0 = perfectly stacked on top of each other (pawns WILL hide behind one another); the default matches the normal mid-game fan spacing so all 4 are clearly visible from the first frame. Raise it if pawns still overlap on your board, lower it if you want them tucked tighter together.")]
        [SerializeField] private float startStackOffsetScale = 0.5f;

        [Header("Step Hop Animation")]
        [SerializeField] private float stepDuration = 0.28f;
        [SerializeField] private float jumpPower = 15f;

        [Header("Ladder Climb / Snake Slide Animation")]
        [Tooltip("Pause after landing on a snake/ladder's starting cell, before the climb/slide begins.")]
        [SerializeField] private float connectorPauseDuration = 0.35f;

        [Tooltip("How fast the pawn climbs a ladder, in canvas units per second (same units as Stack Offset Distance). The travel time scales with how long the ladder's path actually is (including any Path Points pointers), so short and long ladders both move at this same pace instead of the long one rushing to fit a fixed time. Lower = slower climb, higher = faster.")]
        [SerializeField] private float ladderClimbSpeed = 260f;

        [Tooltip("How fast the pawn slides down a snake, in canvas units per second. Same idea as Ladder Climb Speed - the travel time scales with the snake's actual path length (including any Path Points pointers), so a long snake takes proportionally longer at this pace instead of rushing. Lower = slower slide, higher = faster.")]
        [SerializeField] private float snakeSlideSpeed = 260f;

        [Tooltip("Safety floor, in seconds, so a very short ladder/snake (or one with no Path Points assigned) never finishes instantly regardless of speed.")]
        [SerializeField] private float minConnectorDuration = 0.25f;

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
        /// <paramref name="onComplete"/> reports whether this specific roll was the one that
        /// unlocked the pawn from the starting cell - callers use that (not "rolled a 6") to
        /// decide whether an extra roll is earned, since only the unlocking roll rewards one.
        /// </summary>
        public void Move(int steps, Action<bool> onComplete = null)
        {
            if (IsMoving || board == null)
            {
                if (board == null)
                    Debug.LogWarning("[SnakeLadderPawn] Move() called but board is not assigned. Call SetBoard() first.");
                onComplete?.Invoke(false);
                return;
            }

            if (!HasEntered)
            {
                if (steps != entryRoll)
                {
                    Debug.Log($"[SnakeLadderPawn] Needs a {entryRoll} to unlock - rolled {steps}, staying put.");
                    onComplete?.Invoke(false);
                    return;
                }

                HasEntered = true;
                Debug.Log("[SnakeLadderPawn] Unlocked! Free to move on future rolls. Reward: one extra roll.");
                RefreshStackVisual();
                onComplete?.Invoke(true); // this roll is what unlocked the pawn - earns the one-time extra roll
                return;
            }

            int maxCell = board.TotalCells;
            int target = CurrentCell + steps;

            if (target > maxCell)
            {
                Debug.Log($"[SnakeLadderPawn] Roll of {steps} overshoots cell {maxCell} from {CurrentCell} - no move this turn.");
                onComplete?.Invoke(false);
                return;
            }

            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveRoutine(target, onComplete));
        }

        private IEnumerator MoveRoutine(int target, Action<bool> onComplete)
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
            // A regular in-board move never earns the extra-roll reward, even on a rolled 6 -
            // that reward is reserved for the one roll that unlocks the pawn from the start.
            onComplete?.Invoke(false);
        }

        private IEnumerator ResolveConnector(SnakeOrLadderConnector connector)
        {
            yield return new WaitForSeconds(connectorPauseDuration);

            Vector3 destTarget = GetLandingPosition(connector.toCell);
            List<Vector3> waypoints = BuildConnectorWaypoints(connector, destTarget);

            if (connector.IsLadder)
            {
                Debug.Log($"[SnakeLadderPawn] Climbing ladder {connector.fromCell} -> {connector.toCell}" +
                          (waypoints.Count > 1 ? $" through {waypoints.Count - 1} pointer(s)" : ""));
                yield return ClimbLadder(waypoints);
            }
            else
            {
                Debug.Log($"[SnakeLadderPawn] Sliding down snake {connector.fromCell} -> {connector.toCell}" +
                          (waypoints.Count > 1 ? $" through {waypoints.Count - 1} pointer(s)" : ""));
                yield return SlideDownSnake(waypoints);
            }

            CurrentCell = connector.toCell;
        }

        /// <summary>
        /// Turns a connector's pointer RectTransforms (if any) into this pawn's pivot-corrected
        /// world-space path, ending at its final landing spot on toCell. A connector with no
        /// pointers assigned collapses to a single-point path - callers use that to fall back
        /// to the old direct jump/slide.
        /// </summary>
        private List<Vector3> BuildConnectorWaypoints(SnakeOrLadderConnector connector, Vector3 destTarget)
        {
            var waypoints = new List<Vector3>();
            if (connector.pathPoints != null)
            {
                foreach (RectTransform point in connector.pathPoints)
                {
                    if (point == null) continue;
                    waypoints.Add(GetRectCenterWorld(point) + GetOwnPivotToCenterOffset());
                }
            }
            waypoints.Add(destTarget);
            return waypoints;
        }

        private IEnumerator Hop(Vector3 targetPos, float duration)
        {
            bool done = false;
            transform.DOJump(targetPos, jumpPower, 1, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => done = true);
            while (!done) yield return null;
        }

        /// <summary>
        /// Climbs a ladder. Travel time is distance / ladderClimbSpeed, so the pawn moves at a
        /// consistent pace no matter how long the ladder's path is (crucial once Path Points
        /// are involved - a fixed duration would make a long pointer path look rushed and a
        /// short one look sluggish). With pointers assigned, glides through them as one smooth
        /// path; with none, falls back to the original single arc-jump straight to toCell.
        /// </summary>
        private IEnumerator ClimbLadder(List<Vector3> waypoints)
        {
            float distance = GetPathLength(transform.position, waypoints);
            float duration = Mathf.Max(minConnectorDuration, distance / Mathf.Max(1f, ladderClimbSpeed));

            bool done = false;
            Tween tween = waypoints.Count > 1
                ? transform.DOPath(waypoints.ToArray(), duration, PathType.CatmullRom)
                    .SetEase(Ease.OutQuad)
                : transform.DOJump(waypoints[0], 40f, 1, duration)
                    .SetEase(Ease.OutQuad);
            tween.OnComplete(() => done = true);
            while (!done) yield return null;
        }

        /// <summary>
        /// Slides down a snake. Travel time is distance / snakeSlideSpeed, for the same reason
        /// as ClimbLadder - keeps the pace consistent regardless of how long the snake's pointer
        /// path is. With pointers assigned, glides through them as a smooth path so the pawn
        /// follows the snake's body instead of cutting straight to toCell; with none, falls back
        /// to the original direct slide.
        /// </summary>
        private IEnumerator SlideDownSnake(List<Vector3> waypoints)
        {
            float distance = GetPathLength(transform.position, waypoints);
            float duration = Mathf.Max(minConnectorDuration, distance / Mathf.Max(1f, snakeSlideSpeed));

            bool done = false;
            Tween tween = waypoints.Count > 1
                ? transform.DOPath(waypoints.ToArray(), duration, PathType.CatmullRom)
                    .SetEase(Ease.InQuad)
                : transform.DOMove(waypoints[0], duration)
                    .SetEase(Ease.InQuad);
            tween.OnComplete(() => done = true);
            while (!done) yield return null;
        }

        /// <summary>Total straight-line length of a waypoint path, starting from a given world position.</summary>
        private static float GetPathLength(Vector3 start, List<Vector3> waypoints)
        {
            float length = 0f;
            Vector3 prev = start;
            foreach (Vector3 wp in waypoints)
            {
                length += Vector3.Distance(prev, wp);
                prev = wp;
            }
            return length;
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
                // Slot 3 (Blue, when all 4 pawns share a cell) sat directly under/behind
                // the other pawns and was getting hidden. Push it further right (1.6x the
                // normal distance) so it clears the other three instead of just mirroring
                // Green's x-offset with an opposite y.
                default: return new Vector2(d , -d);
            }
        }
    }
}