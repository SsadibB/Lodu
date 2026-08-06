using System.Collections.Generic;
using UnityEngine;
using Ludu.Core;

namespace Ludu.EditorTools
{
    public class BoardSetupHelper : MonoBehaviour
    {
        [ContextMenu("Build Basic Track Hierarchy")]
        public void BuildBasicTrackHierarchy()
        {
            GameObject boardObj = GameObject.Find("Board");
            if (boardObj == null)
            {
                boardObj = new GameObject("Board");
                boardObj.AddComponent<BoardPath>();
            }

            GameObject commonParent = GetOrCreateChild(boardObj, "CommonPath");
            GameObject redYardParent = GetOrCreateChild(boardObj, "RedYard");
            GameObject redHomeParent = GetOrCreateChild(boardObj, "RedHomePath");
            GameObject blueYardParent = GetOrCreateChild(boardObj, "BlueYard");
            GameObject blueHomeParent = GetOrCreateChild(boardObj, "BlueHomePath");

            Debug.Log("[BoardSetupHelper] Board hierarchy created successfully.");
        }

        private GameObject GetOrCreateChild(GameObject parent, string childName)
        {
            Transform child = parent.transform.Find(childName);
            if (child != null) return child.gameObject;

            GameObject newChild = new GameObject(childName);
            newChild.transform.SetParent(parent.transform);
            return newChild;
        }
    }
}
