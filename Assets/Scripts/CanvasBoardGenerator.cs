using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ludu.Core;

namespace Ludu.UI
{
    public class CanvasBoardGenerator : MonoBehaviour
    {
        [ContextMenu("Generate Full Visual Canvas Board")]
        public void GenerateBoard()
        {
            GameObject canvasObj = GameObject.Find("LuduCanvas");
            if (canvasObj == null)
            {
                Debug.LogError("[CanvasBoardGenerator] LuduCanvas not found!");
                return;
            }

            Transform boardContainer = canvasObj.transform.Find("BoardContainer");
            if (boardContainer == null)
            {
                Debug.LogError("[CanvasBoardGenerator] BoardContainer not found!");
                return;
            }

            for (int i = boardContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(boardContainer.GetChild(i).gameObject);
            }

            float cellSize = 60f;
            float originX = -450f + (cellSize / 2f);
            float originY = 450f - (cellSize / 2f);

            // 1. Base Yards
            CreateYard(boardContainer, "RedBaseYard", new Vector2(-270, 270), 360f, new Color(0.9f, 0.25f, 0.25f, 1f));
            CreateYard(boardContainer, "GreenBaseYard", new Vector2(270, 270), 360f, new Color(0.25f, 0.85f, 0.3f, 1f));
            CreateYard(boardContainer, "YellowBaseYard", new Vector2(-270, -270), 360f, new Color(0.95f, 0.85f, 0.2f, 1f));
            CreateYard(boardContainer, "BlueBaseYard", new Vector2(270, -270), 360f, new Color(0.25f, 0.45f, 0.9f, 1f));

            // 2. Goal Area
            GameObject homeGoalObj = CreateUIElement(boardContainer, "CenterHomeGoal", new Vector2(0, 0), new Vector2(180, 180));
            Image homeGoalImg = homeGoalObj.AddComponent<Image>();
            homeGoalImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            // 3. Grid Tiles
            GameObject pathParent = CreateUIElement(boardContainer, "PathWaypoints", Vector2.zero, new Vector2(900, 900));

            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 15; c++)
                {
                    if ((r < 6 && c < 6) || (r < 6 && c > 8) || (r > 8 && c < 6) || (r > 8 && c > 8) || (r >= 6 && r <= 8 && c >= 6 && c <= 8))
                    {
                        continue;
                    }

                    Vector2 pos = new Vector2(originX + (c * cellSize), originY - (r * cellSize));
                    GameObject tileObj = CreateUIElement(pathParent.transform, $"Tile_{r}_{c}", pos, new Vector2(cellSize - 4f, cellSize - 4f));
                    Image tileImg = tileObj.AddComponent<Image>();
                    TileNode tileNode = tileObj.AddComponent<TileNode>();

                    if (r == 7 && c >= 1 && c <= 5) tileImg.color = new Color(0.9f, 0.3f, 0.3f, 1f);
                    else if (r == 7 && c >= 9 && c <= 13) tileImg.color = new Color(0.3f, 0.5f, 0.9f, 1f);
                    else if (c == 7 && r >= 1 && r <= 5) tileImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);
                    else if (c == 7 && r >= 9 && r <= 13) tileImg.color = new Color(0.9f, 0.8f, 0.2f, 1f);
                    else if (r == 6 && c == 1) tileImg.color = new Color(0.9f, 0.3f, 0.3f, 1f);
                    else if (r == 8 && c == 13) tileImg.color = new Color(0.3f, 0.5f, 0.9f, 1f);
                    else if (r == 1 && c == 8) tileImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);
                    else if (r == 13 && c == 6) tileImg.color = new Color(0.9f, 0.8f, 0.2f, 1f);
                    else tileImg.color = Color.white;
                }
            }

            // 4. Instantiate 4 Pawns Per Color
            GameObject pawnsParent = CreateUIElement(boardContainer, "Pawns", Vector2.zero, new Vector2(900, 900));

            // Red Pawns (4)
            CreatePawnUI(pawnsParent.transform, "RedPawn_1", PlayerColor.Red, 0, new Vector2(-330, 330), new Color(0.85f, 0.15f, 0.15f, 1f));
            CreatePawnUI(pawnsParent.transform, "RedPawn_2", PlayerColor.Red, 1, new Vector2(-210, 330), new Color(0.85f, 0.15f, 0.15f, 1f));
            CreatePawnUI(pawnsParent.transform, "RedPawn_3", PlayerColor.Red, 2, new Vector2(-330, 210), new Color(0.85f, 0.15f, 0.15f, 1f));
            CreatePawnUI(pawnsParent.transform, "RedPawn_4", PlayerColor.Red, 3, new Vector2(-210, 210), new Color(0.85f, 0.15f, 0.15f, 1f));

            // Green Pawns (4)
            CreatePawnUI(pawnsParent.transform, "GreenPawn_1", PlayerColor.Green, 0, new Vector2(210, 330), new Color(0.15f, 0.75f, 0.2f, 1f));
            CreatePawnUI(pawnsParent.transform, "GreenPawn_2", PlayerColor.Green, 1, new Vector2(330, 330), new Color(0.15f, 0.75f, 0.2f, 1f));
            CreatePawnUI(pawnsParent.transform, "GreenPawn_3", PlayerColor.Green, 2, new Vector2(210, 210), new Color(0.15f, 0.75f, 0.2f, 1f));
            CreatePawnUI(pawnsParent.transform, "GreenPawn_4", PlayerColor.Green, 3, new Vector2(330, 210), new Color(0.15f, 0.75f, 0.2f, 1f));

            // Yellow Pawns (4)
            CreatePawnUI(pawnsParent.transform, "YellowPawn_1", PlayerColor.Yellow, 0, new Vector2(-330, -210), new Color(0.85f, 0.75f, 0.15f, 1f));
            CreatePawnUI(pawnsParent.transform, "YellowPawn_2", PlayerColor.Yellow, 1, new Vector2(-210, -210), new Color(0.85f, 0.75f, 0.15f, 1f));
            CreatePawnUI(pawnsParent.transform, "YellowPawn_3", PlayerColor.Yellow, 2, new Vector2(-330, -330), new Color(0.85f, 0.75f, 0.15f, 1f));
            CreatePawnUI(pawnsParent.transform, "YellowPawn_4", PlayerColor.Yellow, 3, new Vector2(-210, -330), new Color(0.85f, 0.75f, 0.15f, 1f));

            // Blue Pawns (4)
            CreatePawnUI(pawnsParent.transform, "BluePawn_1", PlayerColor.Blue, 0, new Vector2(210, -210), new Color(0.15f, 0.35f, 0.85f, 1f));
            CreatePawnUI(pawnsParent.transform, "BluePawn_2", PlayerColor.Blue, 1, new Vector2(330, -210), new Color(0.15f, 0.35f, 0.85f, 1f));
            CreatePawnUI(pawnsParent.transform, "BluePawn_3", PlayerColor.Blue, 2, new Vector2(210, -330), new Color(0.15f, 0.35f, 0.85f, 1f));
            CreatePawnUI(pawnsParent.transform, "BluePawn_4", PlayerColor.Blue, 3, new Vector2(330, -330), new Color(0.15f, 0.35f, 0.85f, 1f));

            Debug.Log("[CanvasBoardGenerator] Generated 4 pawns per player with UI Button components!");
        }

        private void CreateYard(Transform parent, string name, Vector2 pos, float size, Color yardColor)
        {
            GameObject yardObj = CreateUIElement(parent, name, pos, new Vector2(size, size));
            Image yardImg = yardObj.AddComponent<Image>();
            yardImg.color = yardColor;

            GameObject innerBox = CreateUIElement(yardObj.transform, "InnerYardBox", Vector2.zero, new Vector2(size * 0.65f, size * 0.65f));
            Image innerImg = innerBox.AddComponent<Image>();
            innerImg.color = Color.white;
        }

        private void CreatePawnUI(Transform parent, string name, PlayerColor color, int pawnId, Vector2 pos, Color pawnColor)
        {
            GameObject pawnObj = CreateUIElement(parent, name, pos, new Vector2(48f, 48f));
            Image pawnImg = pawnObj.AddComponent<Image>();
            pawnImg.color = pawnColor;

            Button pawnBtn = pawnObj.AddComponent<Button>();
            Pawn pawnComp = pawnObj.AddComponent<Pawn>();
        }

        private GameObject CreateUIElement(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return obj;
        }
    }
}
