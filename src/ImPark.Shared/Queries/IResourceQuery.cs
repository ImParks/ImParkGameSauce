namespace ImPark.Shared.Queries;

public readonly record struct ResourceCost(string ItemDefId, int Amount);

public interface IResourceQuery
{
    int GetStockCount(string itemDefId);
    bool CanAfford(ResourceCost[] costs);
}
