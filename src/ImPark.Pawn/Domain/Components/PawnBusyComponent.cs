using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Marker: absence means the pawn is idle (Phase 2C will replace with JobComponent).
public struct PawnBusyComponent : IComponent
{
}
