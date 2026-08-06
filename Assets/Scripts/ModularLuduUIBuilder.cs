using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ludu.Core;

namespace Ludu.UI
{
    public class ModularLuduUIBuilder : MonoBehaviour
    {
        [ContextMenu("Build Responsive Anchored UI Hierarchy")]
        public void BuildUIHierarchy()
        {
            // 1. Canvas with Responsive Scaler
            GameObject canvasObj = GameObject.Find("LuduCanvas");
            if (canvasObj == null) canvasObj = new GameObject("LuduCanvas");

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObj);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvasObj);
            GetOrAddComponent<UIManager>(canvasObj);

            // 2. Background Panel
            GameObject bgObj = GetOrCreateChild(canvasObj, "BackgroundPanel");
            bgObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            GetOrAddComponent<Image>(bgObj).color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // 3. Header Panel
            GameObject headerObj = GetOrCreateChild(canvasObj, "HeaderPanel");
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.SetAnchor(UIAnchoringHelper.AnchorPreset.TopStretch);
            headerRect.sizeDelta = new Vector2(0, 180);
            GetOrAddComponent<Image>(headerObj).color = new Color(0.2f, 0.22f, 0.28f, 0.9f);

            GameObject turnTextObj = GetOrCreateChild(headerObj, "TurnText");
            turnTextObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter);
            turnTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 100);
            TextMeshProUGUI turnTMP = GetOrAddComponent<TextMeshProUGUI>(turnTextObj);
            turnTMP.text = "Select a Mode to Play";
            turnTMP.alignment = TextAlignmentOptions.Center;
            turnTMP.fontSize = 42;
            turnTMP.color = Color.white;

            // 4. Board Container
            GameObject boardContainerObj = GetOrCreateChild(canvasObj, "BoardContainer");
            RectTransform boardRect = boardContainerObj.GetComponent<RectTransform>();
            boardRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter);
            boardRect.sizeDelta = new Vector2(900, 900);
            boardRect.anchoredPosition = new Vector2(0, 40);
            GetOrAddComponent<Image>(boardContainerObj).color = new Color(0.9f, 0.9f, 0.9f, 0.05f);

            // 5. Controls Panel (Bottom)
            GameObject controlsObj = GetOrCreateChild(canvasObj, "ControlsPanel");
            RectTransform controlsRect = controlsObj.GetComponent<RectTransform>();
            controlsRect.SetAnchor(UIAnchoringHelper.AnchorPreset.BottomStretch);
            controlsRect.sizeDelta = new Vector2(0, 300);
            GetOrAddComponent<Image>(controlsObj).color = new Color(0.15f, 0.17f, 0.22f, 0.92f);

            // Dice display box
            GameObject diceBoxObj = GetOrCreateChild(controlsObj, "DiceDisplayBox");
            RectTransform diceBoxRect = diceBoxObj.GetComponent<RectTransform>();
            diceBoxRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, -140, 30);
            diceBoxRect.sizeDelta = new Vector2(100, 100);
            Image diceBoxImg = GetOrAddComponent<Image>(diceBoxObj);
            diceBoxImg.color = Color.white;
            // The Dice GameObject (with the Dice.cs component and its 6 face children)
            // is placed under DiceDisplayBox manually in the Inspector — no text label needed.

            // Roll Button
            GameObject rollBtnObj = GetOrCreateChild(controlsObj, "RollDiceButton");
            RectTransform rollBtnRect = rollBtnObj.GetComponent<RectTransform>();
            rollBtnRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 80, 30);
            rollBtnRect.sizeDelta = new Vector2(280, 100);
            GetOrAddComponent<Image>(rollBtnObj).color = new Color(0.85f, 0.25f, 0.25f, 1f);
            GetOrAddComponent<Button>(rollBtnObj);

            GameObject rollBtnTextObj = GetOrCreateChild(rollBtnObj, "ButtonText");
            rollBtnTextObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            TextMeshProUGUI rollBtnTMP = GetOrAddComponent<TextMeshProUGUI>(rollBtnTextObj);
            rollBtnTMP.text = "ROLL DICE";
            rollBtnTMP.alignment = TextAlignmentOptions.Center;
            rollBtnTMP.fontSize = 32;
            rollBtnTMP.color = Color.white;

            // Status text below
            GameObject statusObj = GetOrCreateChild(controlsObj, "StatusText");
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.SetAnchor(UIAnchoringHelper.AnchorPreset.BottomStretch, 0, 10);
            statusRect.sizeDelta = new Vector2(0, 55);
            TextMeshProUGUI statusTMP = GetOrAddComponent<TextMeshProUGUI>(statusObj);
            statusTMP.text = "Roll the dice or select a pawn";
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.fontSize = 26;
            statusTMP.color = new Color(1f, 1f, 1f, 0.6f);

            // 6. Mode Selection Overlay
            GameObject menuPanelObj = GetOrCreateChild(canvasObj, "ModeSelectionPanel");
            menuPanelObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            GetOrAddComponent<Image>(menuPanelObj).color = new Color(0.08f, 0.09f, 0.13f, 0.97f);

            GameObject titleObj = GetOrCreateChild(menuPanelObj, "TitleText");
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 0, 280);
            titleRect.sizeDelta = new Vector2(700, 140);
            TextMeshProUGUI titleTMP = GetOrAddComponent<TextMeshProUGUI>(titleObj);
            titleTMP.text = "🎲 LUDU GAME";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 72;
            titleTMP.color = Color.white;

            GameObject subObj = GetOrCreateChild(menuPanelObj, "SubtitleText");
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 0, 170);
            subRect.sizeDelta = new Vector2(600, 70);
            TextMeshProUGUI subTMP = GetOrAddComponent<TextMeshProUGUI>(subObj);
            subTMP.text = "Choose your game mode";
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.fontSize = 34;
            subTMP.color = new Color(1f, 1f, 1f, 0.65f);

            // 2 Players Button
            GameObject twoPBtnObj = GetOrCreateChild(menuPanelObj, "TwoPlayerButton");
            RectTransform twoPRect = twoPBtnObj.GetComponent<RectTransform>();
            twoPRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 0, 40);
            twoPRect.sizeDelta = new Vector2(420, 120);
            GetOrAddComponent<Image>(twoPBtnObj).color = new Color(0.2f, 0.5f, 0.9f, 1f);
            GetOrAddComponent<Button>(twoPBtnObj);

            GameObject twoPTextObj = GetOrCreateChild(twoPBtnObj, "Text");
            twoPTextObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            TextMeshProUGUI twoPTMP = GetOrAddComponent<TextMeshProUGUI>(twoPTextObj);
            twoPTMP.text = "👥  2 PLAYERS";
            twoPTMP.alignment = TextAlignmentOptions.Center;
            twoPTMP.fontSize = 38;
            twoPTMP.color = Color.white;

            // 4 Players Button
            GameObject fourPBtnObj = GetOrCreateChild(menuPanelObj, "FourPlayerButton");
            RectTransform fourPRect = fourPBtnObj.GetComponent<RectTransform>();
            fourPRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 0, -120);
            fourPRect.sizeDelta = new Vector2(420, 120);
            GetOrAddComponent<Image>(fourPBtnObj).color = new Color(0.85f, 0.4f, 0.1f, 1f);
            GetOrAddComponent<Button>(fourPBtnObj);

            GameObject fourPTextObj = GetOrCreateChild(fourPBtnObj, "Text");
            fourPTextObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            TextMeshProUGUI fourPTMP = GetOrAddComponent<TextMeshProUGUI>(fourPTextObj);
            fourPTMP.text = "👥  4 PLAYERS";
            fourPTMP.alignment = TextAlignmentOptions.Center;
            fourPTMP.fontSize = 38;
            fourPTMP.color = Color.white;

            // 7. Win Screen Overlay
            GameObject winPanelObj = GetOrCreateChild(canvasObj, "WinScreenPanel");
            winPanelObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            GetOrAddComponent<Image>(winPanelObj).color = new Color(0f, 0f, 0f, 0.88f);

            GameObject winTextObj = GetOrCreateChild(winPanelObj, "WinText");
            RectTransform winTextRect = winTextObj.GetComponent<RectTransform>();
            winTextRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 0, 120);
            winTextRect.sizeDelta = new Vector2(700, 140);
            TextMeshProUGUI winTMP = GetOrAddComponent<TextMeshProUGUI>(winTextObj);
            winTMP.text = "Winner!";
            winTMP.alignment = TextAlignmentOptions.Center;
            winTMP.fontSize = 62;
            winTMP.color = Color.white;

            GameObject restartBtnObj = GetOrCreateChild(winPanelObj, "RestartButton");
            RectTransform restartBtnRect = restartBtnObj.GetComponent<RectTransform>();
            restartBtnRect.SetAnchor(UIAnchoringHelper.AnchorPreset.MiddleCenter, 0, -60);
            restartBtnRect.sizeDelta = new Vector2(300, 100);
            GetOrAddComponent<Image>(restartBtnObj).color = new Color(0.2f, 0.7f, 0.3f, 1f);
            GetOrAddComponent<Button>(restartBtnObj);

            GameObject restartTextObj = GetOrCreateChild(restartBtnObj, "RestartText");
            restartTextObj.GetComponent<RectTransform>().SetAnchor(UIAnchoringHelper.AnchorPreset.FullStretch);
            TextMeshProUGUI restartTMP = GetOrAddComponent<TextMeshProUGUI>(restartTextObj);
            restartTMP.text = "PLAY AGAIN";
            restartTMP.alignment = TextAlignmentOptions.Center;
            restartTMP.fontSize = 30;
            restartTMP.color = Color.white;

            winPanelObj.SetActive(false);

            // 8. Create GameManager & Dice in scene if missing
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
                Dice d = gmObj.AddComponent<Dice>();
                Debug.Log("[ModularLuduUIBuilder] Created GameManager + Dice GameObject.");
            }
            else if (Object.FindAnyObjectByType<Dice>() == null)
            {
                gm.gameObject.AddComponent<Dice>();
            }

            // 9. Generate Board Visuals
            CanvasBoardGenerator boardGen = GetOrAddComponent<CanvasBoardGenerator>(canvasObj);
            boardGen.GenerateBoard();

            Debug.Log("[ModularLuduUIBuilder] Build complete! Press Play, choose 2P or 4P, then roll!");
        }

        private GameObject GetOrCreateChild(GameObject parent, string childName)
        {
            Transform child = parent.transform.Find(childName);
            if (child != null) return child.gameObject;
            GameObject newChild = new GameObject(childName);
            newChild.transform.SetParent(parent.transform, false);
            newChild.AddComponent<RectTransform>();
            return newChild;
        }

        private T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }
    }
}
