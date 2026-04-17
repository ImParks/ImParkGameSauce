namespace ImPark.Shared.Modules;

public interface IGameModule
{
    string Id { get; }
    IReadOnlyList<string> Dependencies { get; }
    void OnRegister(IModuleContext context);
    void OnEnable();
    void OnDisable();
}
