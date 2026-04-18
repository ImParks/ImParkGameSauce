using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Input event consumed by BodyPartSystem. Producers (combat module, traps,
// environment) publish this synchronously so damage is resolved inline.
[Event("combat.damage")]
public readonly record struct CombatDamageEvent(
    long TargetEntityId,
    string PartId,
    float Amount,
    string DamageType);
