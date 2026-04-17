using System.Reflection;
using System.Text.Json;

namespace ImPark.Shared.Defs.Patching;

public sealed class PatchApplier
{
    private readonly List<string> _conflictLog = new();

    public IReadOnlyList<string> ConflictLog => _conflictLog;

    public List<Def> Apply(IReadOnlyList<Def> defs, IReadOnlyList<DefPatchOp> patches)
    {
        var lookup = defs
            .ToDictionary(d => (d.GetType().Name, d.DefId), d => CloneDef(d));

        foreach (var patch in patches)
        {
            if (!patch.IsValid)
            {
                _conflictLog.Add($"Invalid op '{patch.Op}' for {patch.Target.DefId}");
                continue;
            }

            var key = (patch.Target.DefType, patch.Target.DefId);

            switch (patch.Op.ToLowerInvariant())
            {
                case DefPatchOp.OpRemove:
                    if (!lookup.Remove(key))
                        _conflictLog.Add($"Remove target not found: {patch.Target.DefType}/{patch.Target.DefId}");
                    break;

                case DefPatchOp.OpAdd:
                case DefPatchOp.OpReplace:
                case DefPatchOp.OpOverride:
                    if (lookup.TryGetValue(key, out var existing))
                    {
                        if (patch.Op.Equals(DefPatchOp.OpAdd, StringComparison.OrdinalIgnoreCase))
                            _conflictLog.Add($"Add conflict (last-writer-wins): {patch.Target.DefType}/{patch.Target.DefId}");

                        ApplyFieldPatch(existing, patch);
                    }
                    else if (patch.Op.Equals(DefPatchOp.OpReplace, StringComparison.OrdinalIgnoreCase))
                    {
                        _conflictLog.Add($"Replace target not found: {patch.Target.DefType}/{patch.Target.DefId}");
                    }
                    break;
            }
        }

        return lookup.Values.ToList();
    }

    private static void ApplyFieldPatch(Def def, DefPatchOp patch)
    {
        if (string.IsNullOrEmpty(patch.Path) || patch.Value is null)
            return;

        var prop = def.GetType().GetProperty(
            patch.Path,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (prop is null || !prop.CanWrite)
            return;

        var value = JsonSerializer.Deserialize(patch.Value.Value.GetRawText(), prop.PropertyType);
        prop.SetValue(def, value);
    }

    private static Def CloneDef(Def source)
    {
        var json = JsonSerializer.Serialize(source, source.GetType());
        return (Def)JsonSerializer.Deserialize(json, source.GetType())!;
    }
}
