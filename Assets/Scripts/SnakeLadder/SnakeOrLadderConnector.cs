using System.Collections.Generic;
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

        [Tooltip("Optional - ordered waypoints (empty RectTransforms) placed along the snake/ladder's body, from fromCell to toCell. When set, the pawn travels through each pointer in order instead of jumping/sliding straight to toCell - use this to make the pawn follow a snake's curve or a ladder's slant. Leave empty for the old direct jump/slide.")]
        public List<RectTransform> pathPoints = new List<RectTransform>();

        public bool IsLadder => toCell > fromCell;
        public bool IsSnake => toCell < fromCell;
    }
}