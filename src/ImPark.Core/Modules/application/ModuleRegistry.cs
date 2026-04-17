using ImPark.Shared.Events;
using ImPark.Shared.Modules;

namespace ImPark.Core.Modules;

public sealed class ModuleRegistry
{
    private readonly Dictionary<string, IGameModule> modules = new();
    private readonly Dictionary<string, ModuleState> states = new();
    private readonly IEventBus eventBus;
    private IModuleContext? capturedContext;

    public ModuleRegistry(IEventBus eventBus)
    {
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public void Register(IGameModule module)
    {
        if (module is null)
            throw new ArgumentNullException(nameof(module));
        if (string.IsNullOrWhiteSpace(module.Id))
            throw new ArgumentException("Module Id must not be empty.", nameof(module));
        if (modules.ContainsKey(module.Id))
            throw new InvalidOperationException($"Module '{module.Id}' is already registered.");

        modules[module.Id] = module;
        states[module.Id] = ModuleState.Registered;
    }

    public void ResolveAndEnable(IModuleContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        capturedContext = context;
        var order = TopologicalSort();

        foreach (var id in order)
        {
            var module = modules[id];
            module.OnRegister(context);
        }

        foreach (var id in order)
        {
            EnableInternal(id);
        }
    }

    public void Enable(string moduleId)
    {
        if (!modules.TryGetValue(moduleId, out var module))
            throw new InvalidOperationException($"Module '{moduleId}' not found.");

        if (states[moduleId] == ModuleState.Enabled)
            return;

        EnableInternal(moduleId);
    }

    public void Disable(string moduleId)
    {
        if (!modules.TryGetValue(moduleId, out var module))
            throw new InvalidOperationException($"Module '{moduleId}' not found.");

        if (states[moduleId] != ModuleState.Enabled)
            return;

        module.OnDisable();
        states[moduleId] = ModuleState.Disabled;
        eventBus.PublishSync(new ModuleDisabledEvent(moduleId));
    }

    public ModuleState GetState(string moduleId)
    {
        if (!states.TryGetValue(moduleId, out var state))
            throw new InvalidOperationException($"Module '{moduleId}' not found.");
        return state;
    }

    public bool IsEnabled(string moduleId)
    {
        return states.TryGetValue(moduleId, out var state) && state == ModuleState.Enabled;
    }

    public IReadOnlyCollection<string> ModuleIds => modules.Keys;

    private void EnableInternal(string moduleId)
    {
        var module = modules[moduleId];
        module.OnEnable();
        states[moduleId] = ModuleState.Enabled;
        eventBus.PublishSync(new ModuleLoadedEvent(moduleId));
        eventBus.PublishSync(new ModuleEnabledEvent(moduleId));
    }

    // Kahn's algorithm with explicit cycle detection via remaining-node check
    private List<string> TopologicalSort()
    {
        var inDegree = new Dictionary<string, int>();
        var graph = new Dictionary<string, List<string>>();

        foreach (var id in modules.Keys)
        {
            inDegree[id] = 0;
            graph[id] = new List<string>();
        }

        foreach (var (id, module) in modules)
        {
            foreach (var dep in module.Dependencies)
            {
                if (!modules.ContainsKey(dep))
                    throw new InvalidOperationException(
                        $"Module '{id}' depends on missing module '{dep}'.");

                graph[dep].Add(id);
                inDegree[id]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var sorted = new List<string>();

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            sorted.Add(id);
            foreach (var neighbor in graph[id])
            {
                if (--inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (sorted.Count != modules.Count)
        {
            var remaining = string.Join(", ", inDegree.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key));
            throw new InvalidOperationException(
                $"Module dependency cycle detected among: {remaining}");
        }

        return sorted;
    }
}
