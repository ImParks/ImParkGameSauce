namespace ImPark.Pawn.Contracts;

public interface IReservationService
{
    bool TryReserve(long pawnEntityId, long targetEntityId, string jobId);
    void Release(long pawnEntityId, long targetEntityId);
    void ReleaseAllByPawn(long pawnEntityId);
    void ReleaseAllByJob(string jobId);
    bool IsReserved(long targetEntityId);
}
