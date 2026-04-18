using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.skill-xp")]
public readonly record struct PawnSkillXpEvent(
    long EntityId,
    string SkillDefId,
    float XpGained);
