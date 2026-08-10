using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Ludu.Core
{
    [RequireComponent(typeof(Button))]
    public class Pawn : MonoBehaviour
    {
        [Header("Pawn Info")]
        [SerializeField] private PlayerColor color;
        [SerializeField] private int pawnId;

        [Header("Visual — assign your pawn sprite here")]
        [SerializeField] private Sprite pawnSprite;

        [Header("Stacking (multiple pawns sharing one tile)")]
        [SerializeField] private float stackOffsetDistance = 14f;
        [SerializeField] private float stackedScaleMultiplier = 0.65f; // shrink applied to every pawn when 2+ share a tile

        [Header("Turn Highlight (pulsing scale + come forward)")]
        [SerializeField] private float highlightScaleMultiplier = 1.35f;
        [SerializeField] private float highlightPulseDuration = 0.45f;
        [SerializeField] private float highlightResetDuration = 0.2f;

        [Header("Move Animation (hop between tiles)")]
        [SerializeField] private float jumpPower = 30f;
        [SerializeField] private float stepDuration = 0.18f;
        [SerializeField] private float captureReturnStepDuration = 0.08f; // faster hops when walking back to the yard after being captured

        [Header("State (Read-only)")]
        [SerializeField] private bool isInYard = true;
        [SerializeField] private int currentPathIndex = -1;

        private Transform defaultYardPosition;
        private Vector3 originalYardPosition;
        private List<TileNode> assignedPath = new List<TileNode>();
        private Coroutine moveCoroutine;
        private Button pawnButton;
        private Image pawnImage;
        private Vector3 baseScale = Vector3.one;
        private Vector3 targetStackScale = Vector3.one; // the scale this pawn should rest at (shrinks when 2+ pawns share its tile)
        private Tween highlightTween;
        private Tween moveTween;
        private Tween stackTween;
        private bool isHighlighted;

        public PlayerColor Color => color;
        public int PawnId => pawnId;
        public bool IsInYard => isInYard;
        public int CurrentPathIndex => currentPathIndex;
        public bool IsMoving => moveCoroutine != null;
        public bool IsFinished { get; private set; }

        public TileNode CurrentTile => (currentPathIndex >= 0 && currentPathIndex < assignedPath.Count)
            ? assignedPath[currentPathIndex] : null;

        private void Awake()
        {
            pawnImage = GetComponent<Image>();
            pawnButton = GetComponent<Button>();
            baseScale = transform.localScale;
            targetStackScale = baseScale;
            originalYardPosition = transform.position; // its spot inside InnerYardBox, set by CanvasBoardGenerator

            if (pawnButton != null)
                pawnButton.onClick.AddListener(OnPawnClickedUI);

            // Apply sprite if assigned
            ApplySprite();
        }

        private void ApplySprite()
        {
            if (pawnImage != null && pawnSprite != null)
            {
                pawnImage.sprite = pawnSprite;
                pawnImage.color = UnityEngine.Color.white; // let the sprite carry the color
                pawnImage.preserveAspect = true;
            }
        }

        /// <summary>
        /// Called by GameManager to set up this pawn before game starts.
        /// </summary>
        /// <summary>
        /// Call this from the board generator to stamp the correct color before Play mode starts.
        /// </summary>
        public void SetupColor(PlayerColor playerColor, int id)
        {
            color = playerColor;
            pawnId = id;
        }

        public void Initialize(PlayerColor playerColor, int id, Transform yardTransform, List<TileNode> path)
        {
            color = playerColor;
            pawnId = id;
            defaultYardPosition = yardTransform;
            assignedPath = path ?? new List<TileNode>();
            ApplySprite();
            ResetToYard();
        }

        public void ResetToYard()
        {
            isInYard = true;
            currentPathIndex = -1;
            IsFinished = false;
            SetHighlighted(false);
            moveTween?.Kill();
            stackTween?.Kill();
            transform.position = defaultYardPosition != null ? defaultYardPosition.position : originalYardPosition;
        }

        /// <summary>
        /// Called when this pawn gets captured. Instead of teleporting, it hops
        /// backward tile-by-tile along its own assigned path until it reaches
        /// index 0, then hops one final time into its own yard slot (matching
        /// its pawnId — assigned back in Initialize).
        /// </summary>
        public void CapturedReturnToYard(Action onComplete = null)
        {
            SetHighlighted(false);
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveTween?.Kill();
            stackTween?.Kill();
            moveCoroutine = StartCoroutine(CapturedReturnRoutine(onComplete));
        }

        private IEnumerator CapturedReturnRoutine(Action onComplete)
        {
            for (int i = currentPathIndex - 1; i >= 0; i--)
            {
                if (assignedPath[i] == null) continue;
                Vector3 target = GetLandingPosition(assignedPath[i]);
                yield return HopTo(target, captureReturnStepDuration);
                currentPathIndex = i;
            }

            Vector3 yardTarget = defaultYardPosition != null ? defaultYardPosition.position : originalYardPosition;
            yield return HopTo(yardTarget, captureReturnStepDuration);

            isInYard = true;
            currentPathIndex = -1;
            IsFinished = false;
            moveCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Returns true if this pawn can legally move <paramref name="steps"/> spaces.
        /// Requires 6 to exit the yard.
        /// </summary>
        public bool CanMove(int steps)
        {
            if (IsFinished) return false;
            if (isInYard) return steps == 6;
            return (currentPathIndex + steps) < assignedPath.Count;
        }

        /// <summary>
        /// Returns the tile this pawn would land on if it moved <paramref name="steps"/>
        /// spaces right now, WITHOUT actually moving it. Used by the bot AI to score
        /// candidate moves before picking one. Returns null if there's no such tile
        /// (e.g. exiting the yard on anything but a 6).
        /// </summary>
        public TileNode PeekTileAt(int steps)
        {
            if (isInYard)
                return (steps == 6 && assignedPath.Count > 0) ? assignedPath[0] : null;

            int targetIndex = Mathf.Clamp(currentPathIndex + steps, 0, assignedPath.Count - 1);
            return (targetIndex >= 0 && targetIndex < assignedPath.Count) ? assignedPath[targetIndex] : null;
        }

        public void MovePawn(int steps, Action onComplete)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveTween?.Kill();
            stackTween?.Kill();
            moveCoroutine = StartCoroutine(MoveRoutine(steps, onComplete));
        }

        private IEnumerator MoveRoutine(int steps, Action onComplete)
        {
            if (isInYard)
            {
                // Exit yard — hop straight to the start tile
                isInYard = false;
                currentPathIndex = 0;

                if (assignedPath.Count > 0 && assignedPath[0] != null)
                {
                    Vector3 target = GetLandingPosition(assignedPath[0]);
                    yield return HopTo(target);
                }
            }
            else
            {
                int targetIndex = currentPathIndex + steps;
                targetIndex = Mathf.Clamp(targetIndex, 0, assignedPath.Count - 1);

                for (int i = currentPathIndex + 1; i <= targetIndex; i++)
                {
                    if (assignedPath[i] == null) continue;
                    bool isFinalStep = (i == targetIndex);

                    // Pass-through tiles: hop to exact center (no stacking offset).
                    // Final landing tile: hop into a stacking slot, centered if alone.
                    Vector3 target = isFinalStep
                        ? GetLandingPosition(assignedPath[i])
                        : assignedPath[i].transform.position;

                    yield return HopTo(target);
                    currentPathIndex = i;
                }
            }

            if (currentPathIndex >= assignedPath.Count - 1)
                IsFinished = true;

            moveCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Hops the pawn to targetPos with a small arc (jump effect) instead of sliding flat.
        /// </summary>
        private IEnumerator HopTo(Vector3 targetPos, float? duration = null)
        {
            moveTween?.Kill();
            stackTween?.Kill();
            bool done = false;
            moveTween = transform.DOJump(targetPos, jumpPower, 1, duration ?? stepDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => done = true);

            while (!done) yield return null;
        }

        private void OnPawnClickedUI()
        {
            GameManager.Instance?.OnPawnClicked(this);
        }

        // Fallback for non-UI click (3D scene)
        private void OnMouseDown() => OnPawnClickedUI();

        /// <summary>
        /// Works out where on "tile" this pawn should actually land: dead center if it's
        /// the only pawn there, or fanned into a small corner slot around the center if
        /// other pawns are already occupying that tile (e.g. a safe tile).
        /// </summary>
        private Vector3 GetLandingPosition(TileNode tile)
        {
            if (tile == null) return transform.position;
            if (GameManager.Instance == null) return tile.transform.position;

            List<Pawn> others = GameManager.Instance.GetPawnsOnTile(tile, this);
            int slotIndex = others.Count; // 0 = first/only pawn here -> exact center
            Vector2 offset = GetStackSlotOffset(slotIndex);
            return tile.transform.position + (Vector3)offset;
        }

        /// <summary>
        /// Re-settles this pawn into its correct stacking slot (position + size) based on
        /// how many pawns are on its current tile right now. Called by GameManager whenever
        /// the stack on a tile might have changed — a pawn arrived, left, or got captured —
        /// so the survivors shrink/grow and re-fan automatically.
        /// </summary>
        public void RefreshStackVisual()
        {
            if (isInYard || CurrentTile == null || GameManager.Instance == null) return;
            if (moveCoroutine != null) return; // don't fight an in-progress hop

            List<Pawn> allOnTile = GameManager.Instance.GetPawnsOnTile(CurrentTile, null); // includes self
            int totalCount = allOnTile.Count;
            int myIndex = Mathf.Max(0, allOnTile.IndexOf(this));

            targetStackScale = totalCount >= 2 ? baseScale * stackedScaleMultiplier : baseScale;
            Vector2 offset = totalCount >= 2 ? GetStackSlotOffset(myIndex) : Vector2.zero;
            Vector3 targetPos = CurrentTile.transform.position + (Vector3)offset;

            stackTween?.Kill();
            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad));
            if (!isHighlighted)
                seq.Join(transform.DOScale(targetStackScale, 0.2f).SetEase(Ease.OutQuad));
            stackTween = seq;
        }

        private Vector2 GetStackSlotOffset(int slotIndex)
        {
            // Alone on the tile: dead center, no offset.
            if (slotIndex <= 0) return Vector2.zero;

            // Second, third, fourth+ pawn on the tile fan out into corners around the center.
            float d = stackOffsetDistance;
            switch ((slotIndex - 1) % 4)
            {
                case 0: return new Vector2(-d, d);   // top-left
                case 1: return new Vector2(d, d);    // top-right
                case 2: return new Vector2(-d, -d);  // bottom-left
                default: return new Vector2(d, -d);  // bottom-right
            }
        }

        /// <summary>
        /// Called by GameManager when it's this pawn's turn and it's a legal move.
        /// While highlighted, the pawn pulses (loops scale up/down) and stays on top of
        /// the sibling order so it's clearly visible and clickable even if stacked.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted) return;
            isHighlighted = highlighted;

            highlightTween?.Kill();
            stackTween?.Kill();

            if (highlighted)
            {
                transform.SetAsLastSibling(); // render on top of the stack
                highlightTween = transform.DOScale(baseScale * highlightScaleMultiplier, highlightPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                highlightTween = transform.DOScale(targetStackScale, highlightResetDuration)
                    .SetEase(Ease.OutQuad);
            }
        }
    }
}