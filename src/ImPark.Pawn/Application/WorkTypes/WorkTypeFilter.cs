using ImPark.Pawn.Domain.Components;

namespace ImPark.Pawn.Application.WorkTypes;

public enum WorkTypeFilter
{
    None,
    Construct,
    Haul,
    Craft,
    Cook,
    Grow,
}

public static class WorkTypeFilterExtensions
{
    public static bool Matches(WorkCapabilityComponent cap, WorkTypeFilter filter)
    {
        return filter switch
        {
            WorkTypeFilter.None => true,
            WorkTypeFilter.Construct => cap.CanConstruct,
            WorkTypeFilter.Haul => cap.CanHaul,
            WorkTypeFilter.Craft => cap.CanCraft,
            WorkTypeFilter.Cook => cap.CanCook,
            WorkTypeFilter.Grow => cap.CanGrow,
            _ => false,
        };
    }
}
