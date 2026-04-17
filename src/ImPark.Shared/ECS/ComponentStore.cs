using System.Runtime.CompilerServices;

namespace ImPark.Shared.ECS;

internal interface IComponentStoreInternal
{
    void TryRemove(EntityId entity);
}

public sealed class ComponentStore<T> : IComponentStoreInternal where T : struct, IComponent
{
    private T[] denseData;
    private EntityId[] denseEntities;
    private int count;

    private readonly Dictionary<long, int> entityToIndex;

    public ComponentStore(int initialCapacity = 64)
    {
        denseData = new T[initialCapacity];
        denseEntities = new EntityId[initialCapacity];
        entityToIndex = new Dictionary<long, int>(initialCapacity);
        count = 0;
    }

    public int Count => count;

    public void Add(EntityId entity, T component)
    {
        if (entityToIndex.ContainsKey(entity.Value))
            throw new InvalidOperationException(
                $"Entity {entity} already has component {typeof(T).Name}.");

        EnsureCapacity();

        int index = count;
        denseData[index] = component;
        denseEntities[index] = entity;
        entityToIndex[entity.Value] = index;
        count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(EntityId entity)
    {
        if (!entityToIndex.TryGetValue(entity.Value, out int index))
            throw new KeyNotFoundException(
                $"Entity {entity} does not have component {typeof(T).Name}.");

        return ref denseData[index];
    }

    public bool TryGet(EntityId entity, out T component)
    {
        if (entityToIndex.TryGetValue(entity.Value, out int index))
        {
            component = denseData[index];
            return true;
        }

        component = default;
        return false;
    }

    public bool Has(EntityId entity) => entityToIndex.ContainsKey(entity.Value);

    public void Remove(EntityId entity)
    {
        if (!entityToIndex.TryGetValue(entity.Value, out int index))
            throw new KeyNotFoundException(
                $"Entity {entity} does not have component {typeof(T).Name}.");

        int lastIndex = count - 1;

        if (index != lastIndex)
        {
            denseData[index] = denseData[lastIndex];
            denseEntities[index] = denseEntities[lastIndex];
            entityToIndex[denseEntities[index].Value] = index;
        }

        denseData[lastIndex] = default;
        denseEntities[lastIndex] = default;
        entityToIndex.Remove(entity.Value);
        count--;
    }

    public void TryRemove(EntityId entity)
    {
        if (entityToIndex.ContainsKey(entity.Value))
            Remove(entity);
    }

    public ReadOnlySpan<EntityId> GetEntities() => new(denseEntities, 0, count);

    public ReadOnlySpan<T> GetData() => new(denseData, 0, count);

    private void EnsureCapacity()
    {
        if (count < denseData.Length) return;

        int newCapacity = denseData.Length * 2;
        var newData = new T[newCapacity];
        var newEntities = new EntityId[newCapacity];

        Array.Copy(denseData, newData, count);
        Array.Copy(denseEntities, newEntities, count);

        denseData = newData;
        denseEntities = newEntities;
    }
}
