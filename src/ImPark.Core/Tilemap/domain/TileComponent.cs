using ImPark.Shared.ECS;

namespace ImPark.Core.Tilemap;

public readonly struct TileComponent : IComponent
{
    public TerrainType Terrain { get; init; }
    public bool IsWalkable { get; init; }
    public float MoveCost { get; init; }
    public string TerrainDefId { get; init; }
}
