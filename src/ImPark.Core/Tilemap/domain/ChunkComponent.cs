using ImPark.Shared.ECS;

namespace ImPark.Core.Tilemap;

public struct ChunkComponent : IComponent
{
    public int ChunkX { get; set; }
    public int ChunkY { get; set; }
}
