using ImPark.Pawn.Contracts;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.AI;

public sealed class ReservationService : IReservationService, IDisposable
{
    private readonly Dictionary<long, (long PawnId, string JobId)> byTarget = new();
    private readonly Dictionary<long, List<long>> byPawn = new();
    private readonly List<IDisposable> subscriptions = new();

    public ReservationService(IEventBus eventBus)
    {
        if (eventBus is null) throw new ArgumentNullException(nameof(eventBus));
        subscriptions.Add(eventBus.Subscribe<PawnDiedEvent>(e => ReleaseAllByPawn(e.EntityId)));
        subscriptions.Add(eventBus.Subscribe<PawnDownedEvent>(e => ReleaseAllByPawn(e.EntityId)));
        subscriptions.Add(eventBus.Subscribe<PawnDraftChangedEvent>(e => ReleaseAllByPawn(e.EntityId)));
    }

    public bool TryReserve(long pawnEntityId, long targetEntityId, string jobId)
    {
        if (byTarget.ContainsKey(targetEntityId)) return false;
        byTarget[targetEntityId] = (pawnEntityId, jobId);
        if (!byPawn.TryGetValue(pawnEntityId, out var list))
        {
            list = new List<long>();
            byPawn[pawnEntityId] = list;
        }
        list.Add(targetEntityId);
        return true;
    }

    public void Release(long pawnEntityId, long targetEntityId)
    {
        if (!byTarget.TryGetValue(targetEntityId, out var entry)) return;
        if (entry.PawnId != pawnEntityId) return;
        byTarget.Remove(targetEntityId);
        if (byPawn.TryGetValue(pawnEntityId, out var list))
            list.Remove(targetEntityId);
    }

    public void ReleaseAllByPawn(long pawnEntityId)
    {
        if (!byPawn.TryGetValue(pawnEntityId, out var list)) return;
        for (int i = 0; i < list.Count; i++)
            byTarget.Remove(list[i]);
        list.Clear();
        byPawn.Remove(pawnEntityId);
    }

    public void ReleaseAllByJob(string jobId)
    {
        var toRemove = new List<long>();
        foreach (var kv in byTarget)
        {
            if (kv.Value.JobId == jobId) toRemove.Add(kv.Key);
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            var target = toRemove[i];
            if (byTarget.TryGetValue(target, out var entry))
            {
                if (byPawn.TryGetValue(entry.PawnId, out var list))
                    list.Remove(target);
                byTarget.Remove(target);
            }
        }
    }

    public bool IsReserved(long targetEntityId) => byTarget.ContainsKey(targetEntityId);

    public void Dispose()
    {
        for (int i = 0; i < subscriptions.Count; i++)
            subscriptions[i].Dispose();
        subscriptions.Clear();
    }
}
