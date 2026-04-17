using FluentAssertions;
using ImPark.Shared.Defs.Loading;
using Xunit;

namespace ImPark.Shared.Tests.Defs;

public class DefLoaderTests
{
    [Fact]
    public void LoadFromJson_ParsesValidDef()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        {
            "defType": "ThingDef",
            "defId": "wood",
            "label": "Wood",
            "description": "A piece of wood"
        }
        """;

        var result = loader.LoadFromJson(json);

        result.HasErrors.Should().BeFalse();
        result.Defs.Should().HaveCount(1);

        var def = result.Defs[0];
        def.Should().BeOfType<ThingDef>();
        def.DefId.Should().Be("wood");
        def.Label.Should().Be("Wood");
        def.Description.Should().Be("A piece of wood");
    }

    [Fact]
    public void LoadFromJson_ParsesArray()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        [
            { "defType": "ThingDef", "defId": "wood", "label": "Wood" },
            { "defType": "HediffDef", "defId": "burn", "label": "Burn" }
        ]
        """;

        var result = loader.LoadFromJson(json);

        result.HasErrors.Should().BeFalse();
        result.Defs.Should().HaveCount(2);
        result.Defs[0].Should().BeOfType<ThingDef>();
        result.Defs[1].Should().BeOfType<HediffDef>();
    }

    [Fact]
    public void LoadFromJson_ReportsError_WhenDefTypeMissing()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        {
            "defId": "wood",
            "label": "Wood"
        }
        """;

        var result = loader.LoadFromJson(json);

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("defType");
    }

    [Fact]
    public void LoadFromJson_ReportsError_WhenDefIdMissing()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        {
            "defType": "ThingDef",
            "label": "Wood"
        }
        """;

        var result = loader.LoadFromJson(json);

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("defId");
    }

    [Fact]
    public void LoadFromJson_ReportsError_WhenDefTypeUnknown()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        {
            "defType": "NonExistentDef",
            "defId": "x"
        }
        """;

        var result = loader.LoadFromJson(json);

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("NonExistentDef");
    }

    [Fact]
    public void LoadFromJson_WarnsOnUnknownField()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        {
            "defType": "ThingDef",
            "defId": "wood",
            "label": "Wood",
            "unknownField": 42
        }
        """;

        var result = loader.LoadFromJson(json);

        result.HasErrors.Should().BeFalse();
        result.Defs.Should().HaveCount(1);
        result.Warnings.Should().ContainSingle()
            .Which.Message.Should().Contain("unknownField");
    }

    [Fact]
    public void LoadFromJson_AggregatesMultipleErrors()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var json = """
        [
            { "defId": "noType" },
            { "defType": "ThingDef" },
            { "defType": "ThingDef", "defId": "valid", "label": "OK" }
        ]
        """;

        var result = loader.LoadFromJson(json);

        result.Errors.Should().HaveCount(2);
        result.Defs.Should().HaveCount(1);
        result.Defs[0].DefId.Should().Be("valid");
    }

    [Fact]
    public void LoadFromJson_ReportsError_OnInvalidJson()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);

        var result = loader.LoadFromJson("{ not valid json }");

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Invalid JSON");
    }

    [Fact]
    public void LoadFromDirectory_ReportsError_WhenDirectoryNotFound()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);

        var result = loader.LoadFromDirectory("/nonexistent/path");

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Directory not found");
    }

    [Fact]
    public void LoadFromDirectory_LoadsDefJsonFiles()
    {
        var loader = new DefLoader(typeof(ThingDef).Assembly);
        var tempDir = Path.Combine(Path.GetTempPath(), $"def_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var json = """
            {
                "defType": "ThingDef",
                "defId": "iron",
                "label": "Iron"
            }
            """;
            File.WriteAllText(Path.Combine(tempDir, "materials.def.json"), json);

            File.WriteAllText(Path.Combine(tempDir, "readme.json"), "{}");

            var result = loader.LoadFromDirectory(tempDir);

            result.HasErrors.Should().BeFalse();
            result.Defs.Should().HaveCount(1);
            result.Defs[0].DefId.Should().Be("iron");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
