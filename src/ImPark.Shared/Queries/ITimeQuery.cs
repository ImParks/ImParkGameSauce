namespace ImPark.Shared.Queries;

public enum GameSpeed
{
    Paused = 0,
    Normal = 1,
    Fast = 2,
    SuperFast = 3
}

public interface ITimeQuery
{
    long GetCurrentTick();
    GameSpeed GetSpeed();
    bool IsPaused();
}
