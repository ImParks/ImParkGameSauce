namespace ImPark.Shared.Defs;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DefTypeAttribute : Attribute
{
    public string TypeName { get; }

    public DefTypeAttribute(string typeName)
    {
        TypeName = typeName;
    }
}
