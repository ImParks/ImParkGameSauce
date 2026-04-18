using System;
using ImPark.Core.Time;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.Body;

// Per-tick bleeding resolution. Subscribes to TimeTickEvent (any phase) and
// reduces CurrentHp on every part with BleedRate > 0. If a critical part
// bleeds to zero, emits PawnDiedEvent (sync) exactly once for that pawn on
// that tick.
public sealed class BleedingTickSystem : ISystem, IDisposable
{
    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IDisposable subscription;

    public int Order => 0;

    public BleedingTickSystem(World world, IEventBus eventBus)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        subscription = eventBus.Subscribe<TimeTickEvent>(OnTick);
    }

    // Event-driven; ISystem.Update is a no-op.
    public void Update(World world, long currentTick)
    {
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    private void OnTick(TimeTickEvent evt)
    {
        var dt = evt.DeltaTime;
        if (dt <= 0f) return;

        var pawns = world.Query<BodyComponent>();
        // Snapshot into a local array because Query shares a reusable buffer.
        var pawnIds = new long[pawns.Count];
        for (int i = 0; i < pawns.Count; i++)
            pawnIds[i] = pawns[i].Value;

        for (int p = 0; p < pawnIds.Length; p++)
        {
            var pawn = new EntityId(pawnIds[p]);
            if (!world.IsAlive(pawn)) continue;
            if (!world.TryGetComponent<BodyComponent>(pawn, out var body)) continue;

            float amountLost = 0f;
            bool criticalDestroyed = false;
            string destroyedPartId = string.Empty;

            for (int i = 0; i < body.PartEntityIds.Count; i++)
            {
                var partEntity = new EntityId(body.PartEntityIds[i]);
                if (!world.IsAlive(partEntity)) continue;
                if (!world.TryGetComponent<BodyPartComponent>(partEntity, out var snapshot)) continue;

                if (snapshot.BleedRate <= 0f) continue;

                ref var part = ref world.GetComponent<BodyPartComponent>(partEntity);
                var loss = part.BleedRate * dt;

                // Clamp loss so CurrentHp does not go below zero for accounting,
                // but only the first transition across zero counts as destruction.
                bool wasAboveZero = part.CurrentHp > 0f;
                part.CurrentHp -= loss;
                amountLost += loss;

                if (wasAboveZero && part.CurrentHp <= 0f && part.Critical && !criticalDestroyed)
                {
                    criticalDestroyed = true;
                    destroyedPartId = part.PartId;
                }
            }

            if (amountLost > 0f)
            {
                eventBus.PublishAsync(new PawnBleedingTickEvent(pawn.Value, amountLost));
            }

            if (criticalDestroyed)
            {
                MarkDead(pawn);
                eventBus.PublishSync(new PawnDiedEvent(
                    pawn.Value,
                    $"bleed_out:{destroyedPartId}"));
            }
        }
    }

    private void MarkDead(EntityId pawn)
    {
        if (!world.HasComponent<HealthComponent>(pawn)) return;
        ref var health = ref world.GetComponent<HealthComponent>(pawn);
        health.Dead = true;
    }
}
