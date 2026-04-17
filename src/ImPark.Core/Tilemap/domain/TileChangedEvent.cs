using ImPark.Shared.Events;

namespace ImPark.Core.Tilemap;

[Event("tile.changed")]
public readonly record struct TileChangedEvent(
    int X,
    int Y,
    TerrainType OldTerrain,
    TerrainType NewTerrain
);
