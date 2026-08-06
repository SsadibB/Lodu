using System.Collections.Generic;
using UnityEngine;
using Ludu.Core;
using Ludu.UI;

namespace Ludu.EditorTools
{
    public class BasicSceneBuilder : MonoBehaviour
    {
        [ContextMenu("Build Basic Ludo Scene Objects")]
        public void BuildSceneObjects()
        {
            // 1. Board & Paths
            GameObject boardObj = GameObject.Find("Board");
            if (boardObj == null) boardObj = new GameObject("Board");

            BoardPath boardPath = boardObj.GetComponent<BoardPath>();
            if (boardPath == null) boardPath = boardObj.AddComponent<BoardPath>();

            // 2. Managers
            GameObject managersObj = GameObject.Find("GameManagers");
            if (managersObj == null) managersObj = new GameObject("GameManagers");

            GameManager gameManager = managersObj.GetComponent<GameManager>();
            if (gameManager == null) gameManager = managersObj.AddComponent<GameManager>();

            Dice dice = managersObj.GetComponent<Dice>();
            if (dice == null) dice = managersObj.AddComponent<Dice>();

            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();

            Debug.Log("[BasicSceneBuilder] Built core Ludo game object hierarchy!");
        }
    }
}
