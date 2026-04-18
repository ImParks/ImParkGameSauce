using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Domain.AI.Blackboard;

public struct BlackboardValue
{
    public BlackboardValueKind Kind;
    public Point PointValue;
    public long EntityIdValue;
    public float FloatValue;
    public int IntValue;
    public bool BoolValue;
    public string? StringValue;
    public ulong PathRequestTokenValue;

    public static BlackboardValue FromPoint(Point p)
        => new() { Kind = BlackboardValueKind.Point, PointValue = p };

    public static BlackboardValue FromEntityId(long id)
        => new() { Kind = BlackboardValueKind.EntityId, EntityIdValue = id };

    public static BlackboardValue FromFloat(float f)
        => new() { Kind = BlackboardValueKind.Float, FloatValue = f };

    public static BlackboardValue FromInt(int i)
        => new() { Kind = BlackboardValueKind.Int, IntValue = i };

    public static BlackboardValue FromBool(bool b)
        => new() { Kind = BlackboardValueKind.Bool, BoolValue = b };

    public static BlackboardValue FromString(string s)
        => new() { Kind = BlackboardValueKind.String, StringValue = s };

    public static BlackboardValue FromPathRequestToken(ulong token)
        => new() { Kind = BlackboardValueKind.PathRequestToken, PathRequestTokenValue = token };
}
