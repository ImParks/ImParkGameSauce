using ImPark.Core.Time;
using ImPark.Pawn.Contracts;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Random;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Application.AI;

public sealed class BTRunSystem : ISystem, IDisposable
{
    public int Order => 100;

    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IRandomService rng;
    private readonly IBTLeafRegistry leafRegistry;
    private readonly IDefDatabase? defDb;
    private readonly IDisposable subscription;
    private float pendingDt;
    private bool shouldTick;

    public BTRunSystem(
        World world,
        IEventBus eventBus,
        IRandomService rng,
        IBTLeafRegistry leafRegistry,
        IDefDatabase? defDb = null)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
        this.leafRegistry = leafRegistry ?? throw new ArgumentNullException(nameof(leafRegistry));
        this.defDb = defDb;
        subscription = eventBus.Subscribe<TimeTickEvent>(OnTick);
    }

    private void OnTick(TimeTickEvent evt)
    {
        if (evt.Phase != TickPhase.AI) return;
        pendingDt = evt.DeltaTime;
        shouldTick = true;
    }

    public void Update(World world, long currentTick)
    {
        if (!shouldTick) return;
        shouldTick = false;
        var dt = pendingDt;

        var entities = world.Query<PawnTag, BehaviorTreeComponent>();
        for (int i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            ref var bt = ref world.GetComponent<BehaviorTreeComponent>(e);

            if (bt.BB is null)
                bt.BB = new BB(e.Value);

            if (bt.Root is null)
            {
                if (!TryRebuildTree(ref bt))
                    continue;
            }

            bt.Root!.Tick(bt.BB, dt);
        }
    }

    private bool TryRebuildTree(ref BehaviorTreeComponent bt)
    {
        if (defDb is null || string.IsNullOrEmpty(bt.ThinkTreeDefId)) return false;
        var def = defDb.Get<ThinkTreeDef>(bt.ThinkTreeDefId);
        if (def?.RootNode is null) return false;
        bt.Root = leafRegistry.Resolve(def.RootNode);
        return true;
    }

    public void Dispose() => subscription.Dispose();
}
