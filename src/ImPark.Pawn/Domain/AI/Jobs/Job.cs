using ImPark.Pawn.Application.WorkTypes;
using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Domain.AI.Jobs;

public record struct Job(
    string Id,
    string DefId,
    long? TargetThing,
    Point? TargetCell,
    string[] Materials,
    WorkTypeFilter WorkerFilter,
    int Priority,
    long[] Reservations,
    bool Interruptible
);
