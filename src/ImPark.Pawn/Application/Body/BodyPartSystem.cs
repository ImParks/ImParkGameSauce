// pawn.died (sync) 구독자 우선순위 (결정론):
//  1. ReservationService (2C) - 예약 자동 해제 (먼저 실행)
//  2. BodyPartSystem (2E) - 이 시스템 (사망 원인 기록)
//  3. NeedsTickSystem (2D) - 연결된 polemic thoughts 처리
//  4. UI/로그 시스템 (Phase 5)
// 구독 순서는 시스템 등록 순서에 의해 결정됨 (IModuleRegistry topological sort)
using System;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.Body;

// Consumes CombatDamageEvent and mutates the targeted pawn's body-part state.
// Emits PawnWoundedEvent or PawnDiedEvent (sync) depending on the outcome,
// sets BleedRate on cut/stab damage, and recomputes HealthComponent.PainTotal.
public sealed class BodyPartSystem : ISystem, IDisposable
{
    // Tunable constants (RULE-002: no magic numbers in logic).
    // For Phase 2 bleed rates are expressed as HP-per-second scaled from the
    // amount of the damaging blow.
    private const float CutBleedScalar = 0.05f;
    private const float StabBleedScalar = 0.08f;
    private const float PartPainWeight = 1.0f;
    private const float PainCap = 1.0f;

    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IDisposable subscription;

    public int Order => 0;

    public BodyPartSystem(World world, IEventBus eventBus)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        subscription = eventBus.Subscribe<CombatDamageEvent>(OnDamage);
    }

    // This system is event-driven; Update is a no-op (kept to satisfy ISystem).
    public void Update(World world, long currentTick)
    {
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    private void OnDamage(CombatDamageEvent evt)
    {
        var pawn = new EntityId(evt.TargetEntityId);
        if (!world.IsAlive(pawn)) return;

        if (!world.TryGetComponent<BodyComponent>(pawn, out var body))
            return;

        // Find the child part entity whose BodyPartComponent.PartId matches.
        if (!TryFindPart(body, evt.PartId, out var partEntity))
            return;

        ref var part = ref world.GetComponent<BodyPartComponent>(partEntity);

        part.CurrentHp -= evt.Amount;

        if (part.MaxHp > 0f)
        {
            var painDelta = evt.Amount / part.MaxHp;
            part.Pain += painDelta;
            if (part.Pain > PainCap) part.Pain = PainCap;
        }

        if (evt.DamageType == "cut")
            part.BleedRate += evt.Amount * CutBleedScalar;
        else if (evt.DamageType == "stab")
            part.BleedRate += evt.Amount * StabBleedScalar;

        RecomputePainTotal(pawn, body);

        if (part.CurrentHp <= 0f && part.Critical)
        {
            MarkDead(pawn);
            eventBus.PublishSync(new PawnDiedEvent(
                evt.TargetEntityId,
                $"part_destroyed:{evt.PartId}"));
        }
        else
        {
            eventBus.PublishSync(new PawnWoundedEvent(
                evt.TargetEntityId,
                evt.PartId,
                evt.Amount));
        }
    }

    private bool TryFindPart(in BodyComponent body, string partId, out EntityId partEntity)
    {
        for (int i = 0; i < body.PartEntityIds.Count; i++)
        {
            var candidate = new EntityId(body.PartEntityIds[i]);
            if (!world.IsAlive(candidate)) continue;
            if (!world.TryGetComponent<BodyPartComponent>(candidate, out var bp)) continue;
            if (bp.PartId == partId)
            {
                partEntity = candidate;
                return true;
            }
        }

        partEntity = EntityId.None;
        return false;
    }

    private void RecomputePainTotal(EntityId pawn, in BodyComponent body)
    {
        if (!world.HasComponent<HealthComponent>(pawn)) return;

        var count = body.PartEntityIds.Count;
        if (count == 0) return;

        float painSum = 0f;
        int counted = 0;
        for (int i = 0; i < count; i++)
        {
            var pe = new EntityId(body.PartEntityIds[i]);
            if (!world.IsAlive(pe)) continue;
            if (!world.TryGetComponent<BodyPartComponent>(pe, out var bp)) continue;
            painSum += bp.Pain * PartPainWeight;
            counted++;
        }

        if (counted == 0) return;

        ref var health = ref world.GetComponent<HealthComponent>(pawn);
        health.PainTotal = painSum / counted;
    }

    private void MarkDead(EntityId pawn)
    {
        if (!world.HasComponent<HealthComponent>(pawn)) return;
        ref var health = ref world.GetComponent<HealthComponent>(pawn);
        health.Dead = true;
    }
}
