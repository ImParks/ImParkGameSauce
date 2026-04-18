namespace ImPark.Pawn.Domain.AI.BT;

public sealed record BTNodeSpec(
    string Type,
    Dictionary<string, object> Params,
    BTNodeSpec[] Children)
{
    public int ParamInt(string key)
    {
        if (Params is null || !Params.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Param '{key}' not found in BTNodeSpec '{Type}'.");
        return Convert.ToInt32(v);
    }

    public string ParamString(string key)
    {
        if (Params is null || !Params.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Param '{key}' not found in BTNodeSpec '{Type}'.");
        return v?.ToString() ?? string.Empty;
    }

    public float ParamFloat(string key)
    {
        if (Params is null || !Params.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Param '{key}' not found in BTNodeSpec '{Type}'.");
        return Convert.ToSingle(v);
    }

    public bool ParamBool(string key)
    {
        if (Params is null || !Params.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Param '{key}' not found in BTNodeSpec '{Type}'.");
        return Convert.ToBoolean(v);
    }

    public bool TryParamString(string key, out string value)
    {
        if (Params is not null && Params.TryGetValue(key, out var v) && v is not null)
        {
            value = v.ToString() ?? string.Empty;
            return true;
        }
        value = string.Empty;
        return false;
    }
}
