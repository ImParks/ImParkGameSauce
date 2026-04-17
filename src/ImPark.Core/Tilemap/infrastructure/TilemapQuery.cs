using ImPark.Shared.Queries;

namespace ImPark.Core.Tilemap;

/// <summary>
/// Implements ITilemapQuery by delegating to TilemapSystem.
/// All lookups are O(1) array access.
/// </summary>
public sealed class TilemapQuery : ITilemapQuery
{
    private readonly TilemapSystem _tilemap;

    public TilemapQuery(TilemapSystem tilemap)
    {
        _tilemap = tilemap;
    }

    public TileData GetTile(int x, int y)
    {
        var tile = _tilemap.GetTile(x, y);
        int regionId = _tilemap.GetRegionId(x, y);

        return new TileData(
            TerrainDefId: tile.TerrainDefId,
            IsWalkable: tile.IsWalkable,
            RegionId: regionId,
            MoveCost: tile.MoveCost
        );
    }

    public bool IsWalkable(int x, int y)
    {
        return _tilemap.GetTile(x, y).IsWalkable;
    }

    public int GetRegionId(int x, int y)
    {
        return _tilemap.GetRegionId(x, y);
    }
}
