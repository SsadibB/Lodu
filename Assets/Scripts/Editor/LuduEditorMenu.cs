using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ludu.UI;
using Ludu.Core;

namespace Ludu.EditorTools
{
    public static class AutoUIInitializer
    {
        // NOTE: This used to run automatically on every script recompile via [InitializeOnLoad],
        // which rebuilt the UI and re-linked all pawns into color lists by reading each Pawn's
        // "color" field. Since PlayerColor.Red is the enum's default, any pawn whose color was
        // never explicitly set in the Inspector got swept into redPawns on every recompile —
        // silently overwriting manual GameManager setup. It's now menu-only (Ludu/Generate Full
        // Canvas & Board UI) so it never runs unless you click it on purpose.

        [MenuItem("Ludu/Generate Full Canvas & Board UI")]
        public static void ExecuteBuild()
        {
            GameObject builderObj = new GameObject("TempUIBuilder");
            ModularLuduUIBuilder builder = builderObj.AddComponent<ModularLuduUIBuilder>();
            builder.BuildUIHierarchy();
            Object.DestroyImmediate(builderObj);

            // Link references on GameManager
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gm = gmObj.AddComponent<GameManager>();
            }

            BoardPath bp = Object.FindAnyObjectByType<BoardPath>();
            Dice dice = Object.FindAnyObjectByType<Dice>();
            UIManager uim = Object.FindAnyObjectByType<UIManager>();

            SerializedObject soGM = new SerializedObject(gm);
            if (bp != null) soGM.FindProperty("boardPath").objectReferenceValue = bp;
            if (dice != null) soGM.FindProperty("dice").objectReferenceValue = dice;
            if (uim != null) soGM.FindProperty("uiManager").objectReferenceValue = uim;

            // Pawns are assigned manually in the GameManager Inspector (4 per color list) —
            // no auto-linking here anymore.
            soGM.ApplyModifiedProperties();

            // Wire UIManager serialized fields
            if (uim != null)
            {
                SerializedObject soUI = new SerializedObject(uim);
                GameObject canvas = GameObject.Find("LuduCanvas");
                if (canvas != null)
                {
                    Transform menu = canvas.transform.Find("ModeSelectionPanel");
                    if (menu != null)
                    {
                        soUI.FindProperty("modeSelectionPanel").objectReferenceValue = menu.gameObject;
                        Transform btn2P = menu.Find("TwoPlayerButton");
                        Transform btn4P = menu.Find("FourPlayerButton");
                        if (btn2P != null) soUI.FindProperty("twoPlayerButton").objectReferenceValue = btn2P.GetComponent<Button>();
                        if (btn4P != null) soUI.FindProperty("fourPlayerButton").objectReferenceValue = btn4P.GetComponent<Button>();
                    }

                    Transform controls = canvas.transform.Find("ControlsPanel");
                    if (controls != null)
                    {
                        Transform rollBtn = controls.Find("RollDiceButton");
                        if (rollBtn != null) soUI.FindProperty("rollDiceButton").objectReferenceValue = rollBtn.GetComponent<Button>();
                    }

                    Transform header = canvas.transform.Find("HeaderPanel");
                    if (header != null)
                    {
                        Transform turnTxt = header.Find("TurnText");
                        if (turnTxt != null) soUI.FindProperty("turnIndicatorText").objectReferenceValue = turnTxt.GetComponent<TextMeshProUGUI>();
                    }

                    Transform winPanel = canvas.transform.Find("WinScreenPanel");
                    if (winPanel != null)
                    {
                        soUI.FindProperty("winPanel").objectReferenceValue = winPanel.gameObject;
                        Transform winTxt = winPanel.Find("WinText");
                        Transform restartBtn = winPanel.Find("RestartButton");
                        if (winTxt != null) soUI.FindProperty("winText").objectReferenceValue = winTxt.GetComponent<TextMeshProUGUI>();
                        if (restartBtn != null) soUI.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
                    }
                }
                soUI.ApplyModifiedProperties();
            }

            Debug.Log("[AutoUIInitializer] Successfully auto-linked GameManager and UIManager Inspector properties!");
        }

        [MenuItem("Ludu/Snake & Ladder/Auto-Wire Board Cells")]
        public static void AutoWireSnakeAndLadderCells()
        {
            SnakeAndLadderBoard board = Object.FindAnyObjectByType<SnakeAndLadderBoard>();
            if (board == null)
            {
                Debug.LogError("[AutoUIInitializer] No SnakeAndLadderBoard component found in the scene.");
                return;
            }

            // Look for a child named "Snake&LadderBoardContainer" holding the cell(0)..cell(99)
            // RectTransforms. Falls back to the board's own transform if not found under that name.
            Transform container = null;
            foreach (Transform child in board.transform)
            {
                if (child.name.Contains("BoardContainer")) { container = child; break; }
            }
            if (container == null) container = board.transform;

            // cell(0)..cell(99) => board numbers 1..100. Sort by the number in the name rather
            // than raw sibling order, since Hierarchy order can drift from creation order.
            var ordered = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in container)
            {
                RectTransform rt = child as RectTransform;
                if (rt != null) ordered.Add(rt);
            }
            ordered.Sort((a, b) =>
            {
                int na = ExtractCellIndex(a.name);
                int nb = ExtractCellIndex(b.name);
                return na.CompareTo(nb);
            });

            if (ordered.Count != 100)
                Debug.LogWarning($"[AutoUIInitializer] Found {ordered.Count} children under '{container.name}', expected 100 (cell(0)..cell(99)). Wiring them anyway in sibling order - double check the Cells list afterward.");

            SerializedObject soBoard = new SerializedObject(board);
            SerializedProperty cellsProp = soBoard.FindProperty("cells");
            cellsProp.ClearArray();
            for (int i = 0; i < ordered.Count; i++)
                AddToPropArray(cellsProp, ordered[i]);
            soBoard.ApplyModifiedProperties();

            Debug.Log($"[AutoUIInitializer] Wired {ordered.Count} cells onto {board.name} from '{container.name}' in sibling order (cell(0) -> board number 1).");
        }

        /// <summary>
        /// Numbers a 10x10 Snake &amp; Ladder grid in classic zigzag (boustrophedon) order:
        /// row 0 (bottom row on screen) reads 1-10 left-to-right, row 1 reads 20-11
        /// left-to-right, row 2 reads 21-30 left-to-right, and so on alternating.
        /// Reads the container's GridLayoutGroup (Start Corner / Start Axis) to work out
        /// each child's actual on-screen row/column, so it doesn't depend on object names -
        /// then relabels each cell's TMP text AND rewires SnakeAndLadderBoard's Cells list
        /// to match, so the visuals and the gameplay logic always agree.
        /// Requires GridLayoutGroup.startAxis = Horizontal (fills row by row).
        /// </summary>
        [MenuItem("Ludu/Snake & Ladder/Auto-Number Cells (Zigzag)")]
        public static void AutoNumberSnakeAndLadderCellsZigzag()
        {
            SnakeAndLadderBoard board = Object.FindAnyObjectByType<SnakeAndLadderBoard>();
            if (board == null)
            {
                Debug.LogError("[AutoUIInitializer] No SnakeAndLadderBoard component found in the scene.");
                return;
            }

            Transform container = null;
            foreach (Transform child in board.transform)
            {
                if (child.name.Contains("BoardContainer")) { container = child; break; }
            }
            if (container == null) container = board.transform;

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                Debug.LogError($"[AutoUIInitializer] No GridLayoutGroup found on '{container.name}'. This tool reads its Start Corner / Start Axis to work out cell order - add one there (or move this script to the object that has it) and try again.");
                return;
            }

            if (grid.startAxis != GridLayoutGroup.Axis.Horizontal)
            {
                Debug.LogError("[AutoUIInitializer] This tool only supports GridLayoutGroup Start Axis = Horizontal (fills row by row). Change it in the Inspector and try again.");
                return;
            }

            const int columns = 10;
            int childCount = container.childCount;
            int rows = Mathf.CeilToInt(childCount / (float)columns);

            if (childCount != rows * columns)
                Debug.LogWarning($"[AutoUIInitializer] '{container.name}' has {childCount} children - expected a multiple of {columns} (10x10=100). Numbering may be off.");

            var rectByBoardNumber = new RectTransform[rows * columns];

            for (int sib = 0; sib < childCount; sib++)
            {
                RectTransform rt = container.GetChild(sib) as RectTransform;
                if (rt == null) continue;

                int rasterRow = sib / columns; // raw fill order before Start Corner correction
                int rasterCol = sib % columns;

                // Convert to a 0,0-is-top-left-of-screen row/col regardless of Start Corner.
                int visualRow, visualCol;
                switch (grid.startCorner)
                {
                    case GridLayoutGroup.Corner.UpperLeft:
                        visualRow = rasterRow; visualCol = rasterCol; break;
                    case GridLayoutGroup.Corner.UpperRight:
                        visualRow = rasterRow; visualCol = columns - 1 - rasterCol; break;
                    case GridLayoutGroup.Corner.LowerLeft:
                        visualRow = rows - 1 - rasterRow; visualCol = rasterCol; break;
                    default: // LowerRight
                        visualRow = rows - 1 - rasterRow; visualCol = columns - 1 - rasterCol; break;
                }

                // Board row 0 = bottom row on screen (numbers 1-10).
                int boardRow = rows - 1 - visualRow;
                int boardCol = visualCol; // left to right

                int boardNumber = (boardRow % 2 == 0)
                    ? boardRow * columns + boardCol + 1          // even rows: ascend left -> right (1-10, 21-30, ...)
                    : boardRow * columns + (columns - boardCol); // odd rows: descend left -> right (20-11, 40-31, ...)

                if (boardNumber < 1 || boardNumber > rows * columns)
                {
                    Debug.LogWarning($"[AutoUIInitializer] Computed out-of-range board number {boardNumber} for '{rt.name}' - skipping.");
                    continue;
                }

                rectByBoardNumber[boardNumber - 1] = rt;

                TMP_Text label = rt.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = (boardNumber == rows * columns) ? "GOAL" : boardNumber.ToString();
            }

            int missing = 0;
            for (int i = 0; i < rectByBoardNumber.Length; i++)
                if (rectByBoardNumber[i] == null) missing++;
            if (missing > 0)
                Debug.LogWarning($"[AutoUIInitializer] {missing} board number(s) have no cell assigned - check the grid has exactly {rows * columns} children.");

            SerializedObject soBoard = new SerializedObject(board);
            SerializedProperty cellsProp = soBoard.FindProperty("cells");
            cellsProp.ClearArray();
            for (int i = 0; i < rectByBoardNumber.Length; i++)
                AddToPropArray(cellsProp, rectByBoardNumber[i]);
            soBoard.ApplyModifiedProperties();

            Debug.Log($"[AutoUIInitializer] Zigzag-numbered {childCount} cells under '{container.name}' and rewired {board.name}'s Cells list to match (board number 1 = bottom-left, direction alternates each row).");
        }

        private static int ExtractCellIndex(string objectName)
        {
            var digits = new System.Text.StringBuilder();
            foreach (char c in objectName)
                if (char.IsDigit(c)) digits.Append(c);
            return digits.Length > 0 ? int.Parse(digits.ToString()) : int.MaxValue;
        }

        private static void AddToPropArray(SerializedProperty prop, Object obj)
        {
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = obj;
        }
    }
}