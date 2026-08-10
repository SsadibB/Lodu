using System.Collections.Generic;
using UnityEngine;

namespace Ludu.Core
{
    public class BoardPath : MonoBehaviour
    {
        [Header("Common 52 Track Waypoints")]
        [SerializeField] private List<TileNode> commonPathNodes = new List<TileNode>();

        [Header("Red Player Waypoints")]
        [SerializeField] private List<TileNode> redYardNodes = new List<TileNode>();
        [SerializeField] private TileNode redStartNode;
        [SerializeField] private List<TileNode> redHomePathNodes = new List<TileNode>();
        [SerializeField] private TileNode redHomeGoalNode;

        [Header("Green Player Waypoints")]
        [SerializeField] private List<TileNode> greenYardNodes = new List<TileNode>();
        [SerializeField] private TileNode greenStartNode;
        [SerializeField] private List<TileNode> greenHomePathNodes = new List<TileNode>();
        [SerializeField] private TileNode greenHomeGoalNode;

        [Header("Yellow Player Waypoints")]
        [SerializeField] private List<TileNode> yellowYardNodes = new List<TileNode>();
        [SerializeField] private TileNode yellowStartNode;
        [SerializeField] private List<TileNode> yellowHomePathNodes = new List<TileNode>();
        [SerializeField] private TileNode yellowHomeGoalNode;

        [Header("Blue Player Waypoints")]
        [SerializeField] private List<TileNode> blueYardNodes = new List<TileNode>();
        [SerializeField] private TileNode blueStartNode;
        [SerializeField] private List<TileNode> blueHomePathNodes = new List<TileNode>();
        [SerializeField] private TileNode blueHomeGoalNode;

        public IReadOnlyList<TileNode> CommonPathNodes => commonPathNodes;

        public List<TileNode> GetFullTrackForPlayer(PlayerColor color)
        {
            List<TileNode> fullPath = new List<TileNode>();

            TileNode startNode = GetStartNode(color);
            List<TileNode> homePath = GetHomePathNodes(color);
            TileNode goalNode = GetHomeGoalNode(color);

            if (commonPathNodes == null || commonPathNodes.Count == 0 || startNode == null)
            {
                Debug.LogWarning($"[BoardPath] Common path or start node missing for player {color}");
                return fullPath;
            }

            int startIndex = commonPathNodes.IndexOf(startNode);
            if (startIndex == -1)
            {
                Debug.LogError($"[BoardPath] Start node for {color} is not present in commonPathNodes!");
                return fullPath;
            }

            int totalCommon = commonPathNodes.Count;
            int commonStepsToTravel = totalCommon > 0 ? totalCommon - 1 : 0; // 51 of the 52 shared tiles, then turn into the home column
            for (int i = 0; i < commonStepsToTravel; i++)
            {
                int nodeIdx = (startIndex + i) % totalCommon;
                fullPath.Add(commonPathNodes[nodeIdx]);
            }

            if (homePath != null) fullPath.AddRange(homePath);
            if (goalNode != null) fullPath.Add(goalNode);

            return fullPath;
        }

        public TileNode GetStartNode(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return redStartNode;
                case PlayerColor.Green: return greenStartNode;
                case PlayerColor.Yellow: return yellowStartNode;
                case PlayerColor.Blue: return blueStartNode;
                default: return redStartNode;
            }
        }

        public List<TileNode> GetHomePathNodes(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return redHomePathNodes;
                case PlayerColor.Green: return greenHomePathNodes;
                case PlayerColor.Yellow: return yellowHomePathNodes;
                case PlayerColor.Blue: return blueHomePathNodes;
                default: return redHomePathNodes;
            }
        }

        public TileNode GetHomeGoalNode(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return redHomeGoalNode;
                case PlayerColor.Green: return greenHomeGoalNode;
                case PlayerColor.Yellow: return yellowHomeGoalNode;
                case PlayerColor.Blue: return blueHomeGoalNode;
                default: return redHomeGoalNode;
            }
        }

        public List<TileNode> GetYardNodes(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return redYardNodes;
                case PlayerColor.Green: return greenYardNodes;
                case PlayerColor.Yellow: return yellowYardNodes;
                case PlayerColor.Blue: return blueYardNodes;
                default: return redYardNodes;
            }
        }

        /// <summary>
        /// Returns the yard slot TileNode for a specific pawn index (0-3) of a color.
        /// Logs a clear warning if that slot hasn't been wired up in the Inspector yet,
        /// instead of silently returning null and leaving pawns with nowhere to return to.
        /// </summary>
        public TileNode GetYardNode(PlayerColor color, int pawnIndex)
        {
            List<TileNode> yardNodes = GetYardNodes(color);

            if (yardNodes == null || pawnIndex < 0 || pawnIndex >= yardNodes.Count || yardNodes[pawnIndex] == null)
            {
                Debug.LogWarning($"[BoardPath] {color} yard slot [{pawnIndex}] is not assigned. " +
                    $"Select this BoardPath object and assign a TileNode into '{color} Player Waypoints > Yard Nodes[{pawnIndex}]'.");
                return null;
            }

            return yardNodes[pawnIndex];
        }
    }
}