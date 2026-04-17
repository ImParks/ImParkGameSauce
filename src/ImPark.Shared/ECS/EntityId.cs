namespace ImPark.Shared.ECS;

public readonly record struct EntityId(long Value)
{
    public static readonly EntityId None = new(0);

    public override string ToString() => $"Entity({Value})";
}
