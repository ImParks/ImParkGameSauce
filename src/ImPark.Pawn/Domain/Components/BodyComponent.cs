using System.Collections.Generic;
using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Deviation note (RULE-001):
// Components are normally pure data (plain value types). BodyComponent contains
// a List<long> of child-part entity IDs because a pawn has a variable number of
// parts derived from its BodyDef (torso, head, limbs, organs, ...). Using a
// fixed-size struct array would either waste memory or cap part counts. The
// list is owned by the component and never shared across pawns; external
// mutation is avoided by convention. This deviation is scoped to body graph
// ownership only and is documented here for future reviewers.
public struct BodyComponent : IComponent
{
    public List<long> PartEntityIds;
    public string BodyDefId;
}
