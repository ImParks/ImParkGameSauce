using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.draft-changed")]
public readonly record struct PawnDraftChangedEvent(long EntityId, bool Drafted);
