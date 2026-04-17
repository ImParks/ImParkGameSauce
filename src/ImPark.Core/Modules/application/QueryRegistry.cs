using ImPark.Shared.Modules;

namespace ImPark.Core.Modules;

public sealed class QueryRegistry : IQueryRegistry
{
    private readonly Dictionary<Type, object> implementations = new();
    private readonly Dictionary<Type, object> fallbacks = new();

    public void Register<TQuery>(TQuery implementation, RegistrationPolicy policy = RegistrationPolicy.Strict)
        where TQuery : class
    {
        if (implementation is null)
            throw new ArgumentNullException(nameof(implementation));

        var type = typeof(TQuery);
        if (implementations.ContainsKey(type) && policy == RegistrationPolicy.Strict)
            throw new InvalidOperationException(
                $"Query '{type.Name}' already registered. Use RegistrationPolicy.Replace to overwrite.");

        implementations[type] = implementation;
    }

    public TQuery Get<TQuery>() where TQuery : class
    {
        var type = typeof(TQuery);
        if (implementations.TryGetValue(type, out var impl))
            return (TQuery)impl;

        if (fallbacks.TryGetValue(type, out var fallbackRaw))
        {
            var fallback = (IModuleFallback<TQuery>)fallbackRaw;
            return fallback.CreateFallback();
        }

        throw new InvalidOperationException(
            $"Query '{type.Name}' not registered and no fallback available.");
    }

    public TQuery? TryGet<TQuery>() where TQuery : class
    {
        var type = typeof(TQuery);
        if (implementations.TryGetValue(type, out var impl))
            return (TQuery)impl;

        return null;
    }

    public void RegisterFallback<TQuery>(IModuleFallback<TQuery> fallback) where TQuery : class
    {
        if (fallback is null)
            throw new ArgumentNullException(nameof(fallback));

        fallbacks[typeof(TQuery)] = fallback;
    }

    // Used by ModuleRegistry when a module disables to revert its query to fallback (RULE-009)
    internal bool Unregister<TQuery>() where TQuery : class
    {
        return implementations.Remove(typeof(TQuery));
    }

    internal bool Unregister(Type queryType)
    {
        return implementations.Remove(queryType);
    }
}
