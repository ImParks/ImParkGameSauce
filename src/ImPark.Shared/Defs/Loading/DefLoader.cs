using System.Reflection;
using System.Text.Json;

namespace ImPark.Shared.Defs.Loading;

public sealed class DefLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, Type> _typeMap;

    public DefLoader() : this(AppDomain.CurrentDomain.GetAssemblies()) { }

    public DefLoader(params Assembly[] assemblies)
    {
        _typeMap = BuildTypeMap(assemblies);
    }

    public DefLoadResult LoadFromDirectory(string directoryPath)
    {
        var result = new DefLoadResult();

        if (!Directory.Exists(directoryPath))
        {
            result.Errors.Add(new DefLoadError(directoryPath, "", $"Directory not found: {directoryPath}"));
            return result;
        }

        var files = Directory.GetFiles(directoryPath, "*.def.json", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            LoadFile(file, result);
        }

        return result;
    }

    public DefLoadResult LoadFromJson(string json, string sourceName = "<inline>")
    {
        var result = new DefLoadResult();
        ParseAndLoad(json, sourceName, result);
        return result;
    }

    private void LoadFile(string filePath, DefLoadResult result)
    {
        string json;
        try
        {
            json = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            result.Errors.Add(new DefLoadError(filePath, "", $"Failed to read file: {ex.Message}"));
            return;
        }

        ParseAndLoad(json, filePath, result);
    }

    private void ParseAndLoad(string json, string sourceName, DefLoadResult result)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException ex)
        {
            result.Errors.Add(new DefLoadError(sourceName, "", $"Invalid JSON: {ex.Message}"));
            return;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                    LoadSingleDef(element, sourceName, result);
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                LoadSingleDef(doc.RootElement, sourceName, result);
            }
            else
            {
                result.Errors.Add(new DefLoadError(sourceName, "", "Root must be an object or array."));
            }
        }
    }

    private void LoadSingleDef(JsonElement element, string sourceName, DefLoadResult result)
    {
        if (!element.TryGetProperty("defType", out var defTypeProp))
        {
            result.Errors.Add(new DefLoadError(sourceName, "", "Missing required field: defType"));
            return;
        }

        var typeName = defTypeProp.GetString() ?? "";

        if (!_typeMap.TryGetValue(typeName, out var clrType))
        {
            result.Errors.Add(new DefLoadError(sourceName, "", $"Unknown defType: {typeName}"));
            return;
        }

        if (!element.TryGetProperty("defId", out var defIdProp) || string.IsNullOrWhiteSpace(defIdProp.GetString()))
        {
            result.Errors.Add(new DefLoadError(sourceName, "", $"Missing required field: defId for defType={typeName}"));
            return;
        }

        var defId = defIdProp.GetString()!;

        WarnOnUnknownFields(element, clrType, sourceName, defId, result);

        Def? def;
        try
        {
            def = (Def?)JsonSerializer.Deserialize(element.GetRawText(), clrType, JsonOptions);
        }
        catch (JsonException ex)
        {
            result.Errors.Add(new DefLoadError(sourceName, defId, $"Deserialization failed: {ex.Message}"));
            return;
        }

        if (def is null)
        {
            result.Errors.Add(new DefLoadError(sourceName, defId, "Deserialization returned null."));
            return;
        }

        result.Defs.Add(def);
    }

    private static void WarnOnUnknownFields(
        JsonElement element, Type clrType, string sourceName, string defId, DefLoadResult result)
    {
        var knownProperties = clrType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant())
            .ToHashSet();

        // defType is a discriminator, not a CLR property
        knownProperties.Add("deftype");

        foreach (var prop in element.EnumerateObject())
        {
            if (!knownProperties.Contains(prop.Name.ToLowerInvariant()))
            {
                result.Warnings.Add(new DefLoadWarning(sourceName, defId, $"Unknown field: {prop.Name}"));
            }
        }
    }

    private static Dictionary<string, Type> BuildTypeMap(Assembly[] assemblies)
    {
        var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).ToArray()!;
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(Def)))
                    continue;

                var attr = type.GetCustomAttribute<DefTypeAttribute>();
                if (attr is not null)
                {
                    map[attr.TypeName] = type;
                }
            }
        }

        return map;
    }
}
