using FluentAssertions;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Api;

public class AuthValidationTests
{
    [Fact]
    public void Validate_ReturnsAllPassed_WhenAllChecksPass()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass, Domain = "example.com" },
            Dkim = [new DkimResult { Status = DkimStatus.Pass, Domain = "example.com" }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass },
            ReverseDns = new ReverseDnsResult { Verified = true }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeTrue();
        validation.SpfPassed.Should().BeTrue();
        validation.DkimPassed.Should().BeTrue();
        validation.DmarcPassed.Should().BeTrue();
        validation.ReverseDnsPassed.Should().BeTrue();
        validation.Failures.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsFailed_WhenSpfFails()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Fail, Domain = "example.com" },
            Dkim = [new DkimResult { Status = DkimStatus.Pass }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeFalse();
        validation.SpfPassed.Should().BeFalse();
        validation.DkimPassed.Should().BeTrue();
        validation.DmarcPassed.Should().BeTrue();
        validation.Failures.Should().HaveCount(1);
        validation.Failures[0].Should().Contain("SPF");
        validation.Failures[0].Should().Contain("example.com");
    }

    [Fact]
    public void Validate_ReturnsFailed_WhenDkimFails()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass },
            Dkim = [new DkimResult { Status = DkimStatus.Fail, Domain = "bad.com" }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeFalse();
        validation.SpfPassed.Should().BeTrue();
        validation.DkimPassed.Should().BeFalse();
        validation.DmarcPassed.Should().BeTrue();
        validation.Failures.Should().HaveCount(1);
        validation.Failures[0].Should().Contain("DKIM");
        validation.Failures[0].Should().Contain("bad.com");
    }

    [Fact]
    public void Validate_ReturnsFailed_WhenDmarcFails()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass },
            Dkim = [new DkimResult { Status = DkimStatus.Pass }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Fail, Policy = DmarcPolicy.Reject }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeFalse();
        validation.SpfPassed.Should().BeTrue();
        validation.DkimPassed.Should().BeTrue();
        validation.DmarcPassed.Should().BeFalse();
        validation.Failures.Should().HaveCount(1);
        validation.Failures[0].Should().Contain("DMARC");
        validation.Failures[0].Should().Contain("Reject");
    }

    [Fact]
    public void Validate_DkimPasses_WhenAtLeastOneSignaturePasses()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass },
            Dkim =
            [
                new DkimResult { Status = DkimStatus.Fail, Domain = "bad.com" },
                new DkimResult { Status = DkimStatus.Pass, Domain = "good.com" }
            ],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeTrue();
        validation.DkimPassed.Should().BeTrue();
        validation.Failures.Should().BeEmpty();
    }

    [Fact]
    public void Validate_HandlesNullResults()
    {
        // Arrange
        var authResults = new AuthenticationResults();

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeFalse(); // No passing checks
        validation.SpfPassed.Should().BeFalse();
        validation.DkimPassed.Should().BeFalse();
        validation.DmarcPassed.Should().BeFalse();
        validation.ReverseDnsPassed.Should().BeFalse();
        validation.Failures.Should().BeEmpty(); // No failures to report either
    }

    [Fact]
    public void Validate_HandlesEmptyDkimArray()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass },
            Dkim = [],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.DkimPassed.Should().BeFalse();
        validation.Failures.Should().BeEmpty(); // Empty array doesn't add failure message
    }

    [Fact]
    public void Validate_ReverseDns_DoesNotAffectOverallPassed()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass },
            Dkim = [new DkimResult { Status = DkimStatus.Pass }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass },
            ReverseDns = new ReverseDnsResult { Verified = false, Hostname = "mail.example.com" }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeTrue(); // Reverse DNS doesn't affect overall pass
        validation.ReverseDnsPassed.Should().BeFalse();
        validation.Failures.Should().HaveCount(1);
        validation.Failures[0].Should().Contain("Reverse DNS");
        validation.Failures[0].Should().Contain("mail.example.com");
    }

    [Fact]
    public void Validate_ReportsMultipleFailures()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Fail, Domain = "example.com" },
            Dkim = [new DkimResult { Status = DkimStatus.Fail, Domain = "example.com" }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Fail },
            ReverseDns = new ReverseDnsResult { Verified = false }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeFalse();
        validation.Failures.Should().HaveCount(4);
    }

    [Fact]
    public void Validate_SpfSoftFail_IsNotPassing()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.SoftFail },
            Dkim = [new DkimResult { Status = DkimStatus.Pass }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.Passed.Should().BeFalse();
        validation.SpfPassed.Should().BeFalse();
        validation.Failures.Should().HaveCount(1);
        validation.Failures[0].Should().Contain("SoftFail");
    }

    [Fact]
    public void Validate_SpfNeutral_IsNotPassing()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Neutral },
            Dkim = [new DkimResult { Status = DkimStatus.Pass }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.SpfPassed.Should().BeFalse();
    }

    [Fact]
    public void Validate_DkimNone_IsNotPassing()
    {
        // Arrange
        var authResults = new AuthenticationResults
        {
            Spf = new SpfResult { Status = SpfStatus.Pass },
            Dkim = [new DkimResult { Status = DkimStatus.None }],
            Dmarc = new DmarcResult { Status = DmarcStatus.Pass }
        };

        // Act
        var validation = authResults.Validate();

        // Assert
        validation.DkimPassed.Should().BeFalse();
    }
}
