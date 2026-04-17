namespace ImPark.Shared.Random;

public interface IRandomService
{
    int Next(int maxExclusive);
    int Next(int minInclusive, int maxExclusive);
    float NextFloat();
    float NextFloat(float min, float max);
    void SetSeed(uint seed);
    uint GetSeed();
}
