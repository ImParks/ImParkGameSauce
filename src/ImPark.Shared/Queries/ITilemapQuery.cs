namespace ImPark.Shared.Queries;

public readonly record struct Vec2(int X, int Y);

public readonly record struct TileData(
    string TerrainDefId,
    bool IsWalkable,
    int RegionId,
    float MoveCost
);

public interface ITilemapQuery
{
    TileData GetTile(int x, int y);
    bool IsWalkable(int x, int y);
    int GetRegionId(int x, int y);
}
