using ImPark.Pawn.Domain.AI.BT;

namespace ImPark.Pawn.Domain.AI.Jobs;

public interface IJobDriver
{
    BTStatus Tick(long pawnEntityId, float dt);
    void OnStart();
    void OnCleanup();
}
