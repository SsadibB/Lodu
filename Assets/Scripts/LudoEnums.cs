namespace Ludu.Core
{
    public enum PlayerColor
    {
        Red,
        Green,
        Yellow,
        Blue
    }

    public enum GameMode
    {
        TwoPlayer,   // Red vs Blue
        FourPlayer   // Red, Green, Yellow, Blue
    }

    public enum TileType
    {
        Normal,
        Safe,
        BaseYard,
        StartTile,
        HomePath,
        HomeGoal
    }
}
