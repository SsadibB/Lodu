using System;
using DG.Tweening;
using UnityEngine;

namespace Ludu.UI
{
    /// <summary>
    /// Controls the "LoadingPanel" shown once at launch, before GameTypeSelectionPanel.
    /// While visible it idles forever: LOGO and the small Dices icon pulse gently, and the
    /// ring-shaped LoadingIcon spins clockwise - all driven by DOTween. UIManager owns when
    /// the panel actually appears/disappears (see UIManager.ShowLoadingScreenThenGameTypeMenu);
    /// this script only knows how to animate itself and fade in/out.
    ///
    /// All four fields below (logo, dices, loadingIcon, canvasGroup) are plain manual
    /// Inspector references - nothing is auto-found. Assign them yourself after attaching
    /// this script; see the field tooltips for exactly which object each one wants.
    /// </summary>
    public class LoadingPanel : MonoBehaviour
    {
        [Header("Required - assign manually in the Inspector")]
        [Tooltip("Drag the CanvasGroup component on this same 'LoadingPanel' GameObject here. Add one via Add Component > Canvas Group first if it doesn't have one yet.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Pulsing icons")]
        [Tooltip("Drag the 'LOGO' RectTransform here.")]
        [SerializeField] private RectTransform logo;
        [Tooltip("Drag the 'Loading/Dices' RectTransform here.")]
        [SerializeField] private RectTransform dices;
        [Tooltip("Peak scale each icon pulses up to (1 = no pulse).")]
        [SerializeField] private float pulseScale = 1.08f;
        [Tooltip("Time for one half of the pulse (scale up OR back down).")]
        [SerializeField] private float pulseDuration = 0.55f;

        [Header("Spinning ring")]
        [Tooltip("Drag the 'Loading/LoadingIcon' RectTransform here.")]
        [SerializeField] private RectTransform loadingIcon;
        [Tooltip("Seconds for one full clockwise rotation of the ring.")]
        [SerializeField] private float spinDuration = 1.1f;

        [Header("Timing")]
        [Tooltip("Minimum time the loading screen stays visible before Show()'s callback fires, so it never just flashes by even if the rest of the game is ready instantly.")]
        [SerializeField] private float minDisplayDuration = 1.5f;
        [Tooltip("Fade duration used by Hide().")]
        [SerializeField] private float fadeOutDuration = 0.35f;

        private Sequence logoPulse;
        private Sequence dicesPulse;
        private Tween ringSpin;
        private Tween fadeTween;
        private bool animationsRunning;

        private void Awake()
        {
            if (canvasGroup == null) Debug.LogWarning("[LoadingPanel] canvasGroup not assigned in the Inspector - Show()/Hide() will not work.");
            if (logo == null) Debug.LogWarning("[LoadingPanel] logo not assigned in the Inspector - it won't pulse.");
            if (dices == null) Debug.LogWarning("[LoadingPanel] dices not assigned in the Inspector - it won't pulse.");
            if (loadingIcon == null) Debug.LogWarning("[LoadingPanel] loadingIcon not assigned in the Inspector - it won't spin.");
        }

        /// <summary>
        /// Activates the panel, snaps it fully visible, and starts the idle pulse/spin
        /// animations. <paramref name="onMinDurationElapsed"/> fires once minDisplayDuration
        /// has passed - callers typically use that to trigger Hide() and move on. Animations
        /// keep running until Hide() (or OnDestroy) stops them, so the ring/logo never freeze
        /// mid-pulse while still on screen.
        /// </summary>
        public void Show(Action onMinDurationElapsed)
        {
            fadeTween?.Kill();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            StartIdleAnimations();

            if (onMinDurationElapsed != null)
                DOVirtual.DelayedCall(minDisplayDuration, () => onMinDurationElapsed.Invoke());
        }

        private void StartIdleAnimations()
        {
            if (animationsRunning) return;
            animationsRunning = true;

            if (logo != null)
            {
                logo.localScale = Vector3.one;
                logoPulse = DOTween.Sequence()
                    .Append(logo.DOScale(pulseScale, pulseDuration).SetEase(Ease.InOutSine))
                    .Append(logo.DOScale(1f, pulseDuration).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Restart);
            }

            if (dices != null)
            {
                dices.localScale = Vector3.one;
                // Small delay offset from the logo so the two don't pulse in perfect lockstep.
                dicesPulse = DOTween.Sequence()
                    .AppendInterval(pulseDuration * 0.5f)
                    .Append(dices.DOScale(pulseScale, pulseDuration).SetEase(Ease.InOutSine))
                    .Append(dices.DOScale(1f, pulseDuration).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Restart);
            }

            if (loadingIcon != null)
            {
                // Negative Z = clockwise as seen on screen in UI space.
                loadingIcon.localEulerAngles = Vector3.zero;
                ringSpin = loadingIcon
                    .DORotate(new Vector3(0f, 0f, -360f), spinDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            }
        }

        private void StopIdleAnimations()
        {
            logoPulse?.Kill();
            dicesPulse?.Kill();
            ringSpin?.Kill();
            animationsRunning = false;

            if (logo != null) logo.localScale = Vector3.one;
            if (dices != null) dices.localScale = Vector3.one;
        }

        /// <summary>Fades the panel out, stops the idle animations, deactivates it, then calls back.</summary>
        public void Hide(Action onHidden = null)
        {
            fadeTween?.Kill();
            fadeTween = canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    StopIdleAnimations();
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    gameObject.SetActive(false);
                    onHidden?.Invoke();
                });
        }

        private void OnDestroy()
        {
            logoPulse?.Kill();
            dicesPulse?.Kill();
            ringSpin?.Kill();
            fadeTween?.Kill();
        }
    }
}