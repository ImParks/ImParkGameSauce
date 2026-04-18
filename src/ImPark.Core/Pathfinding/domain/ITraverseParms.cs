using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

// Hook for future zone/faction restrictions. Return >= 1.0f to add cost,
// float.PositiveInfinity to block. Default impl returns 1.0f (no-op).
public interface ITraverseParms
{
    float GetCostModifier(Point p);
}
