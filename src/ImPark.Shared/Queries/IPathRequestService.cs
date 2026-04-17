namespace ImPark.Shared.Queries;

public readonly record struct PathResult(
    bool IsSuccess,
    Vec2[] Waypoints,
    float TotalCost
);

public interface IPathRequestService
{
    void RequestPath(Vec2 from, Vec2 to, Action<PathResult> callback);
}
