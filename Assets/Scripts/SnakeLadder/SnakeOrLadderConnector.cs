using UnityEngine;

namespace Ludu.Core
{
    /// <summary>
    /// One snake or ladder range on the board, e.g. "4 -> 25" (a ladder) or
    /// "70 -> 20" (a snake). Whether it's a ladder or a snake is derived
    /// automatically from which cell number is bigger - you don't set that
    /// yourself, just the two cell numbers.
    /// </summary>
    [System.Serializable]
    public class SnakeOrLadderConnector
    {
        [Tooltip("Cell number a pawn must land on to trigger this connector (1-100).")]
        public int fromCell;

        [Tooltip("Cell number the pawn ends up at after climbing/sliding (1-100).")]
        public int toCell;

        [Tooltip("Optional - the ladder/snake image spanning fromCell to toCell on the board. Not required for the logic to work.")]
        public GameObject visual;

        public bool IsLadder => toCell > fromCell;
        public bool IsSnake => toCell < fromCell;
    }
}