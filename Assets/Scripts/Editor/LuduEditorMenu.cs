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

        private static void AddToPropArray(SerializedProperty prop, Object obj)
        {
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = obj;
        }
    }
}
