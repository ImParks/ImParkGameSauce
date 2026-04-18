using ImPark.Core.Pathfinding.Domain;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Application;

public interface IPathRequestService
{
    PathRequestHandle Request(
        int entityId,
        Point start,
        Point goal,
        int priority,
        Action<PathResult> cb,
        ITraverseParms? parms = null);

    PathRequestHandle RequestToNearestOf(
        int entityId,
        Point start,
        Point[] goals,
        int priority,
        Action<PathResult> cb,
        ITraverseParms? parms = null);

    bool IsReachable(Point from, Point to, ITraverseParms? parms = null);

    void Cancel(PathRequestHandle h);

    void Tick(float budgetMs);
}
