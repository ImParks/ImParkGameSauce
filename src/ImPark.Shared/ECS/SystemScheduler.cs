namespace ImPark.Shared.ECS;

public sealed class SystemScheduler
{
    private readonly List<ISystem> systems = new();
    private bool isSorted;

    public void Register(ISystem system)
    {
        systems.Add(system);
        isSorted = false;
    }

    public void Unregister(ISystem system)
    {
        systems.Remove(system);
        isSorted = false;
    }

    public void UpdateAll(World world, long currentTick)
    {
        if (!isSorted)
        {
            systems.Sort((a, b) => a.Order.CompareTo(b.Order));
            isSorted = true;
        }

        for (int i = 0; i < systems.Count; i++)
        {
            systems[i].Update(world, currentTick);
        }
    }

    public IReadOnlyList<ISystem> Systems => systems;
}
