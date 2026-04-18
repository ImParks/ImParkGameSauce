namespace ImPark.Pawn.Domain.Components;

// Value type (not IComponent) held inside ThoughtsComponent.Thoughts.
// Offset is added to mood while the thought is active; RemainingTicks
// decreases each AI tick until the thought expires.
public struct ActiveThought
{
    public string DefId;
    public float Offset;
    public float RemainingTicks;
}
