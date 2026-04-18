using ImPark.Pawn.Domain.AI.Jobs;

namespace ImPark.Pawn.Contracts;

public interface IJobGiver
{
    Job? GetJobFor(long pawnEntityId);
}

// Phase 2 stub: always returns null so pawns stay idle until a real giver is wired in.
public sealed class NoOpJobGiver : IJobGiver
{
    public Job? GetJobFor(long pawnEntityId) => null;
}
