// Phase 2 미지원 (Phase 3+ 계획):
// - recreation (레크리에이션, Phase 3F)
// - comfort   (편안함, Phase 3 가구)
// - outdoors  (야외, Phase 4C 날씨 연동)
// - beauty    (미관, Phase 3A 방 인식 필요)
// Phase 2는 hunger, sleep, mood(합성)만 지원. 위 항목들은 컴포넌트/Def 가
// 준비되는 단계에서 본 시스템에 루프를 추가해 확장한다.
using System;
using System.Collections.Generic;
using ImPark.Core.Time;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.Needs;

public sealed class NeedsTickSystem : ISystem, IDisposable
{
    // Clock-rate constant (not a balance value): 1 tick == 1 second,
    // 24 * 60 * 60 ticks per in-game day. Balance lives in NeedDef.FallPerDay.
    public const float SecondsPerDay = 24f * 60f * 60f;

    // Hysteresis band: a need is considered "satisfied" once it rises above
    // this value after previously dipping below the highest critical threshold.
    public const float SatisfiedLevel = 0.8f;

    // Conventional DefIds used by this Phase-2 system.
    public const string HungerDefId = "Hunger";
    public const string SleepDefId = "Sleep";

    public int Order => 200;

    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IDefDatabase defs;
    private readonly IDisposable subscription;

    // Per-entity per-need "was below threshold" memory used to emit
    // NeedSatisfiedEvent exactly on the upward transition.
    private readonly Dictionary<long, bool> hungerWasLow = new();
    private readonly Dictionary<long, bool> sleepWasLow = new();

    public NeedsTickSystem(World world, IEventBus eventBus, IDefDatabase defs)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        this.defs = defs ?? throw new ArgumentNullException(nameof(defs));

        subscription = eventBus.Subscribe<TimeTickEvent>(OnTimeTick);
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    // ISystem.Update is a no-op; this system is driven by TimeTickEvent(AI phase).
    public void Update(World world, long currentTick)
    {
    }

    private void OnTimeTick(TimeTickEvent evt)
    {
        if (evt.Phase != TickPhase.AI) return;

        var hungerDef = defs.Get<NeedDef>(HungerDefId);
        var sleepDef = defs.Get<NeedDef>(SleepDefId);

        TickHunger(evt.DeltaTime, hungerDef);
        TickSleep(evt.DeltaTime, sleepDef);
    }

    private void TickHunger(float dt, NeedDef? def)
    {
        if (def is null || def.Disabled) return;

        var fall = def.FallPerDay * dt / SecondsPerDay;
        var highestThreshold = HighestThreshold(def);

        var entities = world.Query<PawnTag, HungerComponent>();
        for (int i = 0; i < entities.Count; i++)
        {
            var id = entities[i];
            ref var h = ref world.GetComponent<HungerComponent>(id);
            var previous = h.Level;
            var next = Math.Clamp(previous - fall, 0f, 1f);
            h.Level = next;

            EmitCriticalIfCrossed(id.Value, HungerDefId, previous, next, def);
            EmitSatisfiedIfRecovered(id.Value, HungerDefId, next, highestThreshold, hungerWasLow);
        }
    }

    private void TickSleep(float dt, NeedDef? def)
    {
        if (def is null || def.Disabled) return;

        var fall = def.FallPerDay * dt / SecondsPerDay;
        var highestThreshold = HighestThreshold(def);

        var entities = world.Query<PawnTag, SleepComponent>();
        for (int i = 0; i < entities.Count; i++)
        {
            var id = entities[i];
            ref var s = ref world.GetComponent<SleepComponent>(id);
            var previous = s.Level;
            var next = Math.Clamp(previous - fall, 0f, 1f);
            s.Level = next;

            EmitCriticalIfCrossed(id.Value, SleepDefId, previous, next, def);
            EmitSatisfiedIfRecovered(id.Value, SleepDefId, next, highestThreshold, sleepWasLow);
        }
    }

    private void EmitCriticalIfCrossed(
        long entityId, string needDefId, float previous, float next, NeedDef def)
    {
        var thresholds = def.ThreshPercentByLevel;
        if (thresholds is null) return;

        for (int t = 0; t < thresholds.Length; t++)
        {
            var th = thresholds[t];
            if (previous > th && next <= th)
            {
                eventBus.PublishSync(new NeedCriticalEvent(entityId, needDefId, next));
            }
        }
    }

    private void EmitSatisfiedIfRecovered(
        long entityId, string needDefId, float level, float highestThreshold,
        Dictionary<long, bool> wasLow)
    {
        var currentlyLow = level < highestThreshold;
        wasLow.TryGetValue(entityId, out var previouslyLow);

        if (previouslyLow && !currentlyLow && level >= SatisfiedLevel)
        {
            eventBus.PublishAsync(new NeedSatisfiedEvent(entityId, needDefId));
        }

        wasLow[entityId] = currentlyLow;
    }

    private static float HighestThreshold(NeedDef def)
    {
        var thresholds = def.ThreshPercentByLevel;
        if (thresholds is null || thresholds.Length == 0) return 0f;
        var max = thresholds[0];
        for (int i = 1; i < thresholds.Length; i++)
            if (thresholds[i] > max) max = thresholds[i];
        return max;
    }
}
