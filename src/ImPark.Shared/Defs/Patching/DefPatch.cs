using System.Text.Json;

namespace ImPark.Shared.Defs.Patching;

public sealed record DefPatchTarget(string DefType, string DefId);

public sealed record DefPatchOp(string Op, DefPatchTarget Target, string Path, JsonElement? Value)
{
    public const string OpAdd = "add";
    public const string OpReplace = "replace";
    public const string OpRemove = "remove";
    public const string OpOverride = "override";

    private static readonly HashSet<string> ValidOps = new(StringComparer.OrdinalIgnoreCase)
    {
        OpAdd, OpReplace, OpRemove, OpOverride
    };

    public bool IsValid => ValidOps.Contains(Op);
}
