using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ludu.Core
{
    /// <summary>
    /// Attach to the root Dice GameObject.
    /// Assign the six child GameObjects (One, Two, Three, Four, Five, Six) in the Inspector.
    /// Each child contains Dot objects that visually show the face value.
    /// On roll, only the matching face child is SetActive(true); all others are disabled.
    /// </summary>
    public class Dice : MonoBehaviour
    {
        [Header("Dice Face GameObjects (assign One → Six in Inspector)")]
        [SerializeField] private GameObject faceOne;
        [SerializeField] private GameObject faceTwo;
        [SerializeField] private GameObject faceThree;
        [SerializeField] private GameObject faceFour;
        [SerializeField] private GameObject faceFive;
        [SerializeField] private GameObject faceSix;

        [Header("Roll Settings")]
        [SerializeField] private float rollDuration = 0.7f;
        [SerializeField] private float shuffleInterval = 0.07f;

        public event Action<int> OnDiceRolled;

        public bool IsRolling { get; private set; }
        public int LastRolledValue { get; private set; } = 1;

        private GameObject[] _faces;

        private void Awake()
        {
            _faces = new GameObject[]
            {
                faceOne, faceTwo, faceThree, faceFour, faceFive, faceSix
            };

            // Start showing face 1
            ShowFace(1);
        }

        public void RollDice()
        {
            if (IsRolling) return;
            StartCoroutine(RollRoutine());
        }

        private IEnumerator RollRoutine()
        {
            IsRolling = true;
            float elapsed = 0f;
            int tempVal = 1;

            while (elapsed < rollDuration)
            {
                tempVal = Random.Range(1, 7);
                ShowFace(tempVal);
                elapsed += shuffleInterval;
                yield return new WaitForSeconds(shuffleInterval);
            }

            // Final settled value
            LastRolledValue = tempVal;
            ShowFace(LastRolledValue);
            IsRolling = false;

            Debug.Log($"[Dice] Rolled: {LastRolledValue}");
            OnDiceRolled?.Invoke(LastRolledValue);
        }

        /// <summary>
        /// Activates only the face GameObject matching <paramref name="value"/> (1-6).
        /// All others are deactivated.
        /// </summary>
        private void ShowFace(int value)
        {
            if (_faces == null) return;
            for (int i = 0; i < _faces.Length; i++)
            {
                if (_faces[i] != null)
                    _faces[i].SetActive(i == value - 1);
            }
        }
    }
}
