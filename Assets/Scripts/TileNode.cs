using UnityEngine;

namespace Ludu.Core
{
    public class TileNode : MonoBehaviour
    {
        [Header("Tile Settings")]
        [SerializeField] private int tileIndex;
        [SerializeField] private TileType tileType = TileType.Normal;

        public int TileIndex => tileIndex;
        public TileType Type => tileType;

        private void OnDrawGizmos()
        {
            switch (tileType)
            {
                case TileType.Normal:
                    Gizmos.color = Color.white;
                    break;
                case TileType.Safe:
                    Gizmos.color = Color.yellow;
                    break;
                case TileType.BaseYard:
                    Gizmos.color = Color.gray;
                    break;
                case TileType.StartTile:
                    Gizmos.color = Color.cyan;
                    break;
                case TileType.HomePath:
                    Gizmos.color = Color.magenta;
                    break;
                case TileType.HomeGoal:
                    Gizmos.color = Color.green;
                    break;
            }

            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
