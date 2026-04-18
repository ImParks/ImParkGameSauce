namespace ImPark.Core.Pathfinding.Domain;

public enum PathFailReason
{
    NoPath,
    Cancelled,
    RegionUnreachable,
    BudgetExceeded
}
