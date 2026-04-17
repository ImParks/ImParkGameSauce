using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Queries;

namespace ImPark.Core.Time;

public sealed class TickSystem : ISystem
{
    // RULE-003: 200ms tick budget at 1x speed.
    public const float BaseTickDuration = 0.2f;

    private static readonly TickPhase[] PhasesInOrder =
    {
        TickPhase.Input,
        TickPhase.AI,
        TickPhase.Movement,
        TickPhase.Events,
        TickPhase.Render
    };

    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly EntityId timeEntity;

    private float accumulator;

    public int Order => int.MinValue;

    public TickSystem(World world, IEventBus eventBus)
    {
        this.world = world;
        this.eventBus = eventBus;

        timeEntity = world.CreateEntity();
        world.AddComponent(timeEntity, new TimeState
        {
            CurrentTick = 0,
            Speed = GameSpeed.Normal,
            IsPaused = false
        });
    }

    public EntityId TimeEntity => timeEntity;

    public void Update(World world, long currentTick)
    {
        // Time is driven externally via Advance(realDeltaTime); nothing to do here.
    }

    public void Advance(float realDeltaTime)
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);

        if (state.IsPaused || state.Speed == GameSpeed.Paused)
        {
            accumulator = 0f;
            return;
        }

        var multiplier = (int)state.Speed;
        var scaledTickDuration = BaseTickDuration / multiplier;

        accumulator += realDeltaTime;

        while (accumulator >= scaledTickDuration)
        {
            accumulator -= scaledTickDuration;
            state.CurrentTick++;

            EmitTick(state.CurrentTick, scaledTickDuration);
        }
    }

    public void SetSpeed(GameSpeed speed)
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);
        state.Speed = speed;
        // Reset accumulator so the new rate starts cleanly from the next Advance call.
        accumulator = 0f;
    }

    public void Pause()
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);
        state.IsPaused = true;
        accumulator = 0f;
    }

    public void Resume()
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);
        state.IsPaused = false;
    }

    private void EmitTick(long tick, float deltaTime)
    {
        for (int i = 0; i < PhasesInOrder.Length; i++)
        {
            var evt = new TimeTickEvent(tick, deltaTime, PhasesInOrder[i]);
            eventBus.PublishSync(in evt);
        }
    }
}
