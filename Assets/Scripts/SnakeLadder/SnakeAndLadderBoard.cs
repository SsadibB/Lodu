using System.Collections.Generic;
using UnityEngine;

namespace Ludu.Core
{
    /// <summary>
    /// Holds the board's 100 cell transforms plus the snake/ladder ranges you
    /// define in the Inspector. SnakeLadderPawn reads from this to know where
    /// each cell physically is, and whether landing on a cell should trigger
    /// a climb or a slide.
    /// </summary>
    public class SnakeAndLadderBoard : MonoBehaviour
    {
        [Header("Board Cells (assign cell(0)..cell(99) in order - cell(0) = board number 1)")]
        [SerializeField] private List<RectTransform> cells = new List<RectTransform>();

        [Header("Ladders (climb up) - e.g. From 4 To 25")]
        [SerializeField] private List<SnakeOrLadderConnector> ladders = new List<SnakeOrLadderConnector>();

        [Header("Snakes (slide down) - e.g. From 70 To 20")]
        [SerializeField] private List<SnakeOrLadderConnector> snakes = new List<SnakeOrLadderConnector>();

        public int TotalCells => cells.Count;

        /// <summary>Returns the RectTransform for a 1-based board cell number (1-100).</summary>
        public RectTransform GetCellTransform(int cellNumber)
        {
            int index = cellNumber - 1;
            if (index < 0 || index >= cells.Count)
            {
                Debug.LogWarning($"[SnakeAndLadderBoard] Cell {cellNumber} is out of range (1-{cells.Count}). Check the Cells list is fully assigned.");
                return null;
            }
            return cells[index];
        }

        /// <summary>
        /// If a snake or ladder starts at this cell, returns it via <paramref name="connector"/>
        /// and returns true. Otherwise returns false and the pawn just stays where it landed.
        /// </summary>
        public bool TryGetConnector(int cellNumber, out SnakeOrLadderConnector connector)
        {
            foreach (var ladder in ladders)
            {
                if (ladder.fromCell == cellNumber)
                {
                    connector = ladder;
                    return true;
                }
            }

            foreach (var snake in snakes)
            {
                if (snake.fromCell == cellNumber)
                {
                    connector = snake;
                    return true;
                }
            }

            connector = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var ladder in ladders)
            {
                if (ladder.toCell <= ladder.fromCell)
                    Debug.LogWarning($"[SnakeAndLadderBoard] Ladder {ladder.fromCell}->{ladder.toCell} should climb UP (To > From). Did you mean to put this under Snakes instead?");
                ValidateRange(ladder, "Ladder");
            }

            foreach (var snake in snakes)
            {
                if (snake.toCell >= snake.fromCell)
                    Debug.LogWarning($"[SnakeAndLadderBoard] Snake {snake.fromCell}->{snake.toCell} should slide DOWN (To < From). Did you mean to put this under Ladders instead?");
                ValidateRange(snake, "Snake");
            }

            CheckForDuplicateStarts();
        }

        private void ValidateRange(SnakeOrLadderConnector c, string label)
        {
            int max = cells.Count > 0 ? cells.Count : 100;
            if (c.fromCell < 1 || c.fromCell > max || c.toCell < 1 || c.toCell > max)
                Debug.LogWarning($"[SnakeAndLadderBoard] {label} {c.fromCell}->{c.toCell} has a cell number outside 1-{max}.");
        }

        private void CheckForDuplicateStarts()
        {
            var seen = new HashSet<int>();
            foreach (var c in ladders)
            {
                if (!seen.Add(c.fromCell))
                    Debug.LogWarning($"[SnakeAndLadderBoard] Cell {c.fromCell} has more than one connector starting on it.");
            }
            foreach (var c in snakes)
            {
                if (!seen.Add(c.fromCell))
                    Debug.LogWarning($"[SnakeAndLadderBoard] Cell {c.fromCell} has more than one connector starting on it.");
            }
        }
#endif
    }
}