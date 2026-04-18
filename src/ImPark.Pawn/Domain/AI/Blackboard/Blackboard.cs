using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Domain.AI.Blackboard;

public sealed class Blackboard
{
    public long PawnId { get; }
    private readonly Dictionary<string, BlackboardValue> values = new();

    public Blackboard(long pawnId)
    {
        PawnId = pawnId;
    }

    public void Set<T>(string key, T val)
    {
        values[key] = ToValue(val);
    }

    public T Get<T>(string key)
    {
        if (!values.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Blackboard key '{key}' not found.");
        return FromValue<T>(v);
    }

    public bool TryGet<T>(string key, out T val)
    {
        if (!values.TryGetValue(key, out var v))
        {
            val = default!;
            return false;
        }
        val = FromValue<T>(v);
        return true;
    }

    public bool Has(string key) => values.ContainsKey(key);

    public void Remove(string key) => values.Remove(key);

    private static BlackboardValue ToValue<T>(T val)
    {
        return val switch
        {
            Point p => BlackboardValue.FromPoint(p),
            long l => BlackboardValue.FromEntityId(l),
            float f => BlackboardValue.FromFloat(f),
            int i => BlackboardValue.FromInt(i),
            bool b => BlackboardValue.FromBool(b),
            string s => BlackboardValue.FromString(s),
            ulong t => BlackboardValue.FromPathRequestToken(t),
            _ => throw new ArgumentException(
                $"Unsupported blackboard type '{typeof(T).Name}'. " +
                "Allowed: Point, long(EntityId), float, int, bool, string, ulong(PathRequestToken).")
        };
    }

    private static T FromValue<T>(BlackboardValue v)
    {
        object boxed = v.Kind switch
        {
            BlackboardValueKind.Point => v.PointValue,
            BlackboardValueKind.EntityId => v.EntityIdValue,
            BlackboardValueKind.Float => v.FloatValue,
            BlackboardValueKind.Int => v.IntValue,
            BlackboardValueKind.Bool => v.BoolValue,
            BlackboardValueKind.String => (object?)v.StringValue ?? string.Empty,
            BlackboardValueKind.PathRequestToken => v.PathRequestTokenValue,
            _ => throw new InvalidOperationException($"Unknown kind {v.Kind}.")
        };
        return (T)boxed;
    }
}
