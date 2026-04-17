using ImPark.Shared.ECS;

namespace ImPark.Core.Tilemap;

public struct RegionComponent : IComponent
{
    public int RegionId { get; set; }
}
