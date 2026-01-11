using FluentAssertions;
using VaultSandbox.Client.Delivery;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Delivery;

public class BackoffCalculatorTests
{
    [Fact]
    public void Calculate_FirstAttempt_ShouldReturnBaseDelayWithJitter()
    {
        // Arrange
        const int baseDelay = 1000;
        const int attempt = 1;

        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt);

        // Assert - First attempt should be close to base delay (plus up to 30% jitter)
        result.Should().BeGreaterThanOrEqualTo(baseDelay);
        result.Should().BeLessThanOrEqualTo((int)(baseDelay * 1.3));
    }

    [Theory]
    [InlineData(1000, 2, 2000)]  // 2^1 = 2x
    [InlineData(1000, 3, 4000)]  // 2^2 = 4x
    [InlineData(1000, 4, 8000)]  // 2^3 = 8x
    public void Calculate_ExponentialGrowth_ShouldDoubleEachAttempt(int baseDelay, int attempt, int expectedBase)
    {
        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt, maxMultiplier: 100, jitterFactor: 0);

        // Assert - With zero jitter, should be exactly exponential
        result.Should().Be(expectedBase);
    }

    [Fact]
    public void Calculate_ExceedsMaxMultiplier_ShouldCapAtMaxDelay()
    {
        // Arrange
        const int baseDelay = 1000;
        const int attempt = 10;  // 2^9 = 512x, way above max
        const int maxMultiplier = 10;

        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt, maxMultiplier, jitterFactor: 0);

        // Assert - Should be capped at base * maxMultiplier
        result.Should().Be(baseDelay * maxMultiplier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Calculate_ZeroOrNegativeAttempt_ShouldTreatAsFirstAttempt(int attempt)
    {
        // Arrange
        const int baseDelay = 1000;

        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt, jitterFactor: 0);

        // Assert - Should return base delay (2^0 = 1)
        result.Should().Be(baseDelay);
    }

    [Fact]
    public void Calculate_ZeroBaseDelay_ShouldReturnZero()
    {
        // Arrange
        const int baseDelay = 0;
        const int attempt = 5;

        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Calculate_WithJitter_ShouldAddPositiveJitter()
    {
        // Arrange
        const int baseDelay = 1000;
        const int attempt = 1;
        const double jitterFactor = 0.5;

        // Act - Run multiple times to check jitter range
        var results = Enumerable.Range(0, 100)
            .Select(_ => BackoffCalculator.Calculate(baseDelay, attempt, jitterFactor: jitterFactor))
            .ToList();

        // Assert - All results should be within expected range
        results.Should().AllSatisfy(r =>
        {
            r.Should().BeGreaterThanOrEqualTo(baseDelay);
            r.Should().BeLessThanOrEqualTo((int)(baseDelay * (1 + jitterFactor)));
        });
    }

    [Fact]
    public void Calculate_ZeroJitter_ShouldReturnExactDelay()
    {
        // Arrange
        const int baseDelay = 1000;
        const int attempt = 3;

        // Act
        var result1 = BackoffCalculator.Calculate(baseDelay, attempt, jitterFactor: 0);
        var result2 = BackoffCalculator.Calculate(baseDelay, attempt, jitterFactor: 0);

        // Assert - Both should be identical with no jitter
        result1.Should().Be(result2);
    }

    [Fact]
    public void CalculateLinear_ShouldMultiplyCurrentDelay()
    {
        // Arrange
        const int currentDelay = 1000;
        const double multiplier = 2.0;
        const int maxDelay = 10000;

        // Act
        var result = BackoffCalculator.CalculateLinear(currentDelay, multiplier, maxDelay, jitterFactor: 0);

        // Assert
        result.Should().Be(2000);
    }

    [Fact]
    public void CalculateLinear_ExceedsMax_ShouldCapAtMaxDelay()
    {
        // Arrange
        const int currentDelay = 5000;
        const double multiplier = 3.0;
        const int maxDelay = 10000;

        // Act
        var result = BackoffCalculator.CalculateLinear(currentDelay, multiplier, maxDelay, jitterFactor: 0);

        // Assert - Should be capped at max
        result.Should().Be(maxDelay);
    }

    [Theory]
    [InlineData(1000, 1.5, 10000, 1500)]
    [InlineData(2000, 1.5, 10000, 3000)]
    [InlineData(5000, 1.5, 10000, 7500)]
    [InlineData(8000, 1.5, 10000, 10000)]  // Capped at max
    public void CalculateLinear_VariousInputs_ShouldCalculateCorrectly(
        int currentDelay, double multiplier, int maxDelay, int expected)
    {
        // Act
        var result = BackoffCalculator.CalculateLinear(currentDelay, multiplier, maxDelay, jitterFactor: 0);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateLinear_WithJitter_ShouldAddPositiveJitter()
    {
        // Arrange
        const int currentDelay = 1000;
        const double multiplier = 2.0;
        const int maxDelay = 10000;
        const double jitterFactor = 0.3;

        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => BackoffCalculator.CalculateLinear(currentDelay, multiplier, maxDelay, jitterFactor))
            .ToList();

        // Assert
        var expectedBase = 2000;
        results.Should().AllSatisfy(r =>
        {
            r.Should().BeGreaterThanOrEqualTo(expectedBase);
            r.Should().BeLessThanOrEqualTo((int)(expectedBase * (1 + jitterFactor)));
        });
    }

    [Fact]
    public void CalculateLinear_ZeroCurrentDelay_ShouldReturnZero()
    {
        // Arrange
        const int currentDelay = 0;
        const double multiplier = 2.0;
        const int maxDelay = 10000;

        // Act
        var result = BackoffCalculator.CalculateLinear(currentDelay, multiplier, maxDelay);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateLinear_MultiplierLessThanOne_ShouldDecreaseDelay()
    {
        // Arrange
        const int currentDelay = 2000;
        const double multiplier = 0.5;
        const int maxDelay = 10000;

        // Act
        var result = BackoffCalculator.CalculateLinear(currentDelay, multiplier, maxDelay, jitterFactor: 0);

        // Assert
        result.Should().Be(1000);
    }

    [Fact]
    public void AddJitter_ShouldAddPositiveJitter()
    {
        // Arrange
        const int delay = 1000;
        const double jitterFactor = 0.3;

        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => BackoffCalculator.AddJitter(delay, jitterFactor))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Should().BeGreaterThanOrEqualTo(delay);
            r.Should().BeLessThanOrEqualTo((int)(delay * (1 + jitterFactor)));
        });
    }

    [Fact]
    public void AddJitter_ZeroJitterFactor_ShouldReturnOriginalDelay()
    {
        // Arrange
        const int delay = 1000;

        // Act
        var result = BackoffCalculator.AddJitter(delay, jitterFactor: 0);

        // Assert
        result.Should().Be(delay);
    }

    [Fact]
    public void AddJitter_ZeroDelay_ShouldReturnZero()
    {
        // Arrange
        const int delay = 0;
        const double jitterFactor = 0.3;

        // Act
        var result = BackoffCalculator.AddJitter(delay, jitterFactor);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void AddJitter_LargeJitterFactor_ShouldStayWithinBounds()
    {
        // Arrange
        const int delay = 1000;
        const double jitterFactor = 1.0;  // 100% jitter

        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => BackoffCalculator.AddJitter(delay, jitterFactor))
            .ToList();

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Should().BeGreaterThanOrEqualTo(delay);
            r.Should().BeLessThanOrEqualTo(delay * 2);
        });
    }

    [Fact]
    public void AddJitter_DefaultJitterFactor_ShouldUse30Percent()
    {
        // Arrange
        const int delay = 1000;

        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => BackoffCalculator.AddJitter(delay))
            .ToList();

        // Assert - Default jitter is 0.3 (30%)
        results.Should().AllSatisfy(r =>
        {
            r.Should().BeGreaterThanOrEqualTo(delay);
            r.Should().BeLessThanOrEqualTo((int)(delay * 1.3));
        });
    }

    [Fact]
    public void Calculate_DefaultMaxMultiplier_ShouldBe10()
    {
        // Arrange
        const int baseDelay = 1000;
        const int attempt = 20;  // 2^19 would be huge without capping

        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt, jitterFactor: 0);

        // Assert - Should be capped at 10x base delay
        result.Should().Be(10000);
    }

    [Fact]
    public void Calculate_LargeAttemptNumber_ShouldCapAtMaxMultiplier()
    {
        // Arrange
        const int baseDelay = 1000;
        const int attempt = 5;  // 2^4 = 16x, will be capped at 10x

        // Act
        var result = BackoffCalculator.Calculate(baseDelay, attempt, maxMultiplier: 10, jitterFactor: 0);

        // Assert - Should be capped at base * maxMultiplier
        result.Should().Be(10000);
    }
}
