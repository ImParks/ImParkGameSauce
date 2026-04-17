namespace ImPark.Shared.ECS;

public interface ISystem
{
    int Order { get; }

    void Update(World world, long currentTick);
}
