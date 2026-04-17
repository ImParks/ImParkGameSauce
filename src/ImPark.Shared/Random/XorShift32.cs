namespace ImPark.Shared.Random;

public sealed class XorShift32 : IRandomService
{
    private uint _state;

    public XorShift32(uint seed = 1)
    {
        // XorShift cannot have a zero state; it would produce only zeros
        _state = seed == 0 ? 1u : seed;
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be positive.");

        return (int)(NextUInt32() % (uint)maxExclusive);
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(minInclusive), "min must be less than max.");

        uint range = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt32() % range);
    }

    public float NextFloat()
    {
        // Divide by uint.MaxValue to get [0, 1) — the divisor is (2^32 - 1),
        // so the result is strictly less than 1.0f for any non-max input,
        // and exactly 1.0f only for uint.MaxValue which we map to the
        // closest float below 1. In practice IEEE 754 float32 rounds
        // uint.MaxValue / uint.MaxValue to 1.0f, so we use (2^32) as divisor.
        return NextUInt32() / 4_294_967_296.0f; // 2^32
    }

    public float NextFloat(float min, float max)
    {
        if (min >= max)
            throw new ArgumentOutOfRangeException(nameof(min), "min must be less than max.");

        return min + NextFloat() * (max - min);
    }

    public void SetSeed(uint seed)
    {
        _state = seed == 0 ? 1u : seed;
    }

    public uint GetSeed()
    {
        return _state;
    }

    private uint NextUInt32()
    {
        uint s = _state;
        s ^= s << 13;
        s ^= s >> 17;
        s ^= s << 5;
        _state = s;
        return s;
    }
}
