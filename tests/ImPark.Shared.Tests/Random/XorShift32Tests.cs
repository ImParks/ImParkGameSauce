using FluentAssertions;
using ImPark.Shared.Random;
using Xunit;

namespace ImPark.Shared.Tests.Random;

public class XorShift32Tests
{
    [Fact]
    public void SameSeed_ProducesIdenticalSequence()
    {
        var rng1 = new XorShift32(42);
        var rng2 = new XorShift32(42);

        for (int i = 0; i < 1000; i++)
        {
            rng1.Next(100).Should().Be(rng2.Next(100));
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new XorShift32(42);
        var rng2 = new XorShift32(99);

        var hasDifference = false;
        for (int i = 0; i < 100; i++)
        {
            if (rng1.Next(1000) != rng2.Next(1000))
            {
                hasDifference = true;
                break;
            }
        }

        hasDifference.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Next_Max_AlwaysReturnsWithinRange(int max)
    {
        var rng = new XorShift32(123);

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.Next(max);
            value.Should().BeGreaterThanOrEqualTo(0);
            value.Should().BeLessThan(max);
        }
    }

    [Theory]
    [InlineData(-5, 5)]
    [InlineData(0, 100)]
    [InlineData(50, 60)]
    [InlineData(-100, -10)]
    public void Next_MinMax_AlwaysReturnsWithinRange(int min, int max)
    {
        var rng = new XorShift32(456);

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.Next(min, max);
            value.Should().BeGreaterThanOrEqualTo(min);
            value.Should().BeLessThan(max);
        }
    }

    [Fact]
    public void NextFloat_AlwaysReturnsWithinZeroToOne()
    {
        var rng = new XorShift32(789);

        for (int i = 0; i < 10_000; i++)
        {
            float value = rng.NextFloat();
            value.Should().BeGreaterThanOrEqualTo(0f);
            value.Should().BeLessThan(1f);
        }
    }

    [Fact]
    public void NextFloat_MinMax_AlwaysReturnsWithinRange()
    {
        var rng = new XorShift32(321);
        float min = -5.0f;
        float max = 10.0f;

        for (int i = 0; i < 10_000; i++)
        {
            float value = rng.NextFloat(min, max);
            value.Should().BeGreaterThanOrEqualTo(min);
            value.Should().BeLessThan(max);
        }
    }

    [Fact]
    public void SetSeed_GetSeed_RoundTrip()
    {
        var rng = new XorShift32(1);

        rng.SetSeed(12345);
        rng.GetSeed().Should().Be(12345u);

        rng.Next(100);
        uint stateAfterAdvance = rng.GetSeed();
        stateAfterAdvance.Should().NotBe(12345u);
    }

    [Fact]
    public void SetSeed_ResetsSequence()
    {
        var rng = new XorShift32(42);

        var firstRun = new int[50];
        for (int i = 0; i < firstRun.Length; i++)
            firstRun[i] = rng.Next(1000);

        rng.SetSeed(42);

        for (int i = 0; i < firstRun.Length; i++)
            rng.Next(1000).Should().Be(firstRun[i]);
    }

    [Fact]
    public void ZeroSeed_IsHandledGracefully()
    {
        var rng = new XorShift32(0);
        rng.GetSeed().Should().NotBe(0u, "zero state would trap XorShift in all-zeros");

        var rng2 = new XorShift32(1);
        rng2.SetSeed(0);
        rng2.GetSeed().Should().NotBe(0u);
    }

    [Fact]
    public void Next_MaxZeroOrNegative_Throws()
    {
        var rng = new XorShift32(1);

        var act0 = () => rng.Next(0);
        act0.Should().Throw<ArgumentOutOfRangeException>();

        var actNeg = () => rng.Next(-1);
        actNeg.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Next_MinGreaterOrEqualMax_Throws()
    {
        var rng = new XorShift32(1);

        var actEqual = () => rng.Next(5, 5);
        actEqual.Should().Throw<ArgumentOutOfRangeException>();

        var actGreater = () => rng.Next(10, 5);
        actGreater.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Distribution_Next_IsReasonablyUniform()
    {
        var rng = new XorShift32(777);
        const int bucketCount = 10;
        const int sampleCount = 10_000;
        var buckets = new int[bucketCount];

        for (int i = 0; i < sampleCount; i++)
            buckets[rng.Next(bucketCount)]++;

        float expected = sampleCount / (float)bucketCount;

        // Chi-square test: sum of (observed - expected)^2 / expected
        double chiSquare = 0;
        for (int i = 0; i < bucketCount; i++)
        {
            double diff = buckets[i] - expected;
            chiSquare += diff * diff / expected;
        }

        // Critical value for chi-square with 9 df at p=0.01 is ~21.67
        chiSquare.Should().BeLessThan(21.67, "distribution should be reasonably uniform");
    }

    [Fact]
    public void Distribution_NextFloat_IsReasonablyUniform()
    {
        var rng = new XorShift32(888);
        const int bucketCount = 10;
        const int sampleCount = 10_000;
        var buckets = new int[bucketCount];

        for (int i = 0; i < sampleCount; i++)
        {
            int bucket = (int)(rng.NextFloat() * bucketCount);
            if (bucket >= bucketCount) bucket = bucketCount - 1;
            buckets[bucket]++;
        }

        float expected = sampleCount / (float)bucketCount;

        double chiSquare = 0;
        for (int i = 0; i < bucketCount; i++)
        {
            double diff = buckets[i] - expected;
            chiSquare += diff * diff / expected;
        }

        chiSquare.Should().BeLessThan(21.67, "float distribution should be reasonably uniform");
    }
}
