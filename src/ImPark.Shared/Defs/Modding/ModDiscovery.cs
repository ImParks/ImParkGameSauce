using System.Text.Json;

namespace ImPark.Shared.Defs.Modding;

public sealed class ModDiscovery
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public IReadOnlyList<ModManifest> Discover(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
            return Array.Empty<ModManifest>();

        var manifests = new List<ModManifest>();
        var modDirs = Directory.GetDirectories(modsDirectory);

        foreach (var dir in modDirs)
        {
            var manifestPath = Path.Combine(dir, "mod.json");
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ModManifest>(json, JsonOptions);
                if (manifest is not null && !string.IsNullOrWhiteSpace(manifest.Id))
                    manifests.Add(manifest);
            }
            catch (JsonException)
            {
                // Malformed mod.json files are silently skipped
            }
        }

        return TopologicalSort(manifests);
    }

    private static IReadOnlyList<ModManifest> TopologicalSort(List<ModManifest> manifests)
    {
        var byId = manifests.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
        var sorted = new List<ModManifest>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process mods in LoadOrder first, then alphabetically for stable ordering
        var ordered = manifests.OrderBy(m => m.LoadOrder).ThenBy(m => m.Id).ToList();

        foreach (var manifest in ordered)
        {
            Visit(manifest.Id, byId, visited, inStack, sorted);
        }

        return sorted.AsReadOnly();
    }

    private static void Visit(
        string id,
        Dictionary<string, ModManifest> byId,
        HashSet<string> visited,
        HashSet<string> inStack,
        List<ModManifest> sorted)
    {
        if (visited.Contains(id))
            return;

        if (inStack.Contains(id))
            throw new InvalidOperationException($"Cyclic dependency detected involving mod: {id}");

        if (!byId.TryGetValue(id, out var manifest))
            return;

        inStack.Add(id);

        foreach (var dep in manifest.Dependencies)
        {
            Visit(dep, byId, visited, inStack, sorted);
        }

        inStack.Remove(id);
        visited.Add(id);
        sorted.Add(manifest);
    }
}
