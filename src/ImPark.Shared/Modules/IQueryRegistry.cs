namespace ImPark.Shared.Modules;

public enum RegistrationPolicy
{
    Strict,
    Replace
}

public interface IQueryRegistry
{
    void Register<TQuery>(TQuery implementation, RegistrationPolicy policy = RegistrationPolicy.Strict)
        where TQuery : class;
    TQuery Get<TQuery>() where TQuery : class;
    TQuery? TryGet<TQuery>() where TQuery : class;
}
