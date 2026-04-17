namespace ImPark.Core.Modules;

public interface IModuleFallback<TQuery>
{
    TQuery CreateFallback();
}
