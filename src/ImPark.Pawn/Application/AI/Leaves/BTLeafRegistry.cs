using ImPark.Pawn.Contracts;
using ImPark.Pawn.Domain.AI.BT;

namespace ImPark.Pawn.Application.AI.Leaves;

public sealed class BTLeafRegistry : IBTLeafRegistry
{
    private readonly Dictionary<string, Func<BTNodeSpec, IBTNode>> factories = new();

    public BTLeafRegistry()
    {
        Register("selector", BuildSelector);
        Register("sequence", BuildSequence);
        Register("inverter", BuildInverter);
        Register("parallel", BuildParallel);
    }

    public void Register(string leafDefId, Func<BTNodeSpec, IBTNode> factory)
    {
        if (string.IsNullOrEmpty(leafDefId))
            throw new ArgumentException("leafDefId cannot be null or empty.", nameof(leafDefId));
        if (factory is null)
            throw new ArgumentNullException(nameof(factory));

        factories[leafDefId] = factory;
    }

    public IBTNode Resolve(BTNodeSpec spec)
    {
        if (spec is null)
            throw new ArgumentNullException(nameof(spec));
        if (!factories.TryGetValue(spec.Type, out var factory))
            throw new InvalidOperationException(
                $"No BT leaf factory registered for type '{spec.Type}'.");
        return factory(spec);
    }

    private IBTNode BuildSelector(BTNodeSpec spec)
        => new Selector(ResolveChildren(spec));

    private IBTNode BuildSequence(BTNodeSpec spec)
        => new Sequence(ResolveChildren(spec));

    private IBTNode BuildInverter(BTNodeSpec spec)
    {
        var children = ResolveChildren(spec);
        if (children.Length != 1)
            throw new InvalidOperationException("inverter requires exactly one child.");
        return new Inverter(children[0]);
    }

    private IBTNode BuildParallel(BTNodeSpec spec)
    {
        var policyStr = spec.TryParamString("policy", out var p) ? p : "RequireAll";
        var policy = policyStr.Equals("RequireOne", StringComparison.OrdinalIgnoreCase)
            ? ParallelPolicy.RequireOne
            : ParallelPolicy.RequireAll;
        return new Parallel(policy, ResolveChildren(spec));
    }

    private IBTNode[] ResolveChildren(BTNodeSpec spec)
    {
        if (spec.Children is null || spec.Children.Length == 0)
            return Array.Empty<IBTNode>();
        var result = new IBTNode[spec.Children.Length];
        for (int i = 0; i < spec.Children.Length; i++)
            result[i] = Resolve(spec.Children[i]);
        return result;
    }
}
