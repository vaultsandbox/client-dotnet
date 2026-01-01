using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for parsing auth results using the test email API.
/// These tests verify that SPF, DKIM, DMARC, and ReverseDNS results are correctly
/// deserialized from the wire format using the Result property name.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class AuthResultsParsingTests : IntegrationTestBase
{
    private HttpClient? _httpClient;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (Settings.IsConfigured)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Settings.BaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", Settings.ApiKey);
        }
    }

    public override async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        await base.DisposeAsync();
    }

    [SkippableFact]
    public async Task TestEmail_AllAuthPass_ShouldParseResultProperties()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Create test email with all auth passing via the test email API
        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"Auth Pass Test {Guid.NewGuid():N}",
            text = "Testing auth results parsing",
            auth = new
            {
                spf = "pass",
                dkim = "pass",
                dmarc = "pass",
                reverseDns = true
            }
        });

        // Skip if endpoint not available (non-dev environment)
        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available (requires VSB_DEVELOPMENT=true)");

        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert - Verify auth results are parsed correctly with Result property
        email.AuthResults.Should().NotBeNull("email should have authentication results");

        var auth = email.AuthResults!;

        auth.Spf.Should().NotBeNull("SPF result should be present");
        auth.Spf!.Result.Should().Be(SpfStatus.Pass, "SPF Result property should deserialize correctly");

        auth.Dkim.Should().NotBeNullOrEmpty("DKIM results should be present");
        auth.Dkim![0].Result.Should().Be(DkimStatus.Pass, "DKIM Result property should deserialize correctly");

        auth.Dmarc.Should().NotBeNull("DMARC result should be present");
        auth.Dmarc!.Result.Should().Be(DmarcStatus.Pass, "DMARC Result property should deserialize correctly");

        auth.ReverseDns.Should().NotBeNull("ReverseDNS result should be present");
        auth.ReverseDns!.Verified.Should().BeTrue("ReverseDNS Verified should be true");

        // Validate using the Validate() method
        var validation = auth.Validate();
        validation.Passed.Should().BeTrue("all auth checks should pass");
        validation.SpfPassed.Should().BeTrue();
        validation.DkimPassed.Should().BeTrue();
        validation.DmarcPassed.Should().BeTrue();
        validation.ReverseDnsPassed.Should().BeTrue();
        validation.Failures.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task TestEmail_SpfFail_ShouldParseResultAsFailStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"SPF Fail Test {Guid.NewGuid():N}",
            auth = new { spf = "fail" }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.AuthResults.Should().NotBeNull();
        email.AuthResults!.Spf.Should().NotBeNull();
        email.AuthResults.Spf!.Result.Should().Be(SpfStatus.Fail);

        var validation = email.AuthResults.Validate();
        validation.SpfPassed.Should().BeFalse();
        validation.Failures.Should().Contain(f => f.Contains("SPF"));
    }

    [SkippableFact]
    public async Task TestEmail_SpfSoftFail_ShouldParseResultAsSoftFailStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"SPF SoftFail Test {Guid.NewGuid():N}",
            auth = new { spf = "softfail" }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.AuthResults.Should().NotBeNull();
        email.AuthResults!.Spf.Should().NotBeNull();
        email.AuthResults.Spf!.Result.Should().Be(SpfStatus.SoftFail);
    }

    [SkippableFact]
    public async Task TestEmail_DkimFail_ShouldParseResultAsFailStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"DKIM Fail Test {Guid.NewGuid():N}",
            auth = new { dkim = "fail" }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.AuthResults.Should().NotBeNull();
        email.AuthResults!.Dkim.Should().NotBeNullOrEmpty();
        email.AuthResults.Dkim![0].Result.Should().Be(DkimStatus.Fail);

        var validation = email.AuthResults.Validate();
        validation.DkimPassed.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TestEmail_DmarcFail_ShouldParseResultAsFailStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"DMARC Fail Test {Guid.NewGuid():N}",
            auth = new { dmarc = "fail" }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.AuthResults.Should().NotBeNull();
        email.AuthResults!.Dmarc.Should().NotBeNull();
        email.AuthResults.Dmarc!.Result.Should().Be(DmarcStatus.Fail);

        var validation = email.AuthResults.Validate();
        validation.DmarcPassed.Should().BeFalse();
        validation.Failures.Should().Contain(f => f.Contains("DMARC"));
    }

    [SkippableFact]
    public async Task TestEmail_ReverseDnsFail_ShouldParseVerifiedAsFalse()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"ReverseDNS Fail Test {Guid.NewGuid():N}",
            auth = new { reverseDns = false }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.AuthResults.Should().NotBeNull();
        email.AuthResults!.ReverseDns.Should().NotBeNull();
        email.AuthResults.ReverseDns!.Verified.Should().BeFalse();

        var validation = email.AuthResults.Validate();
        validation.ReverseDnsPassed.Should().BeFalse();
        validation.Failures.Should().Contain(f => f.Contains("Reverse DNS"));
    }

    [SkippableFact]
    public async Task TestEmail_AllAuthFailing_ShouldReportAllFailures()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"All Auth Fail Test {Guid.NewGuid():N}",
            auth = new
            {
                spf = "fail",
                dkim = "fail",
                dmarc = "fail",
                reverseDns = false
            }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert - All Result properties should be Fail
        var auth = email.AuthResults!;
        auth.Spf!.Result.Should().Be(SpfStatus.Fail);
        auth.Dkim![0].Result.Should().Be(DkimStatus.Fail);
        auth.Dmarc!.Result.Should().Be(DmarcStatus.Fail);
        auth.ReverseDns!.Verified.Should().BeFalse();

        // Validate() should report 4 failures
        var validation = auth.Validate();
        validation.Passed.Should().BeFalse();
        validation.SpfPassed.Should().BeFalse();
        validation.DkimPassed.Should().BeFalse();
        validation.DmarcPassed.Should().BeFalse();
        validation.ReverseDnsPassed.Should().BeFalse();
        validation.Failures.Should().HaveCount(4);
    }

    [SkippableFact]
    public async Task TestEmail_MixedResults_ShouldParseAllCorrectly()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        var response = await _httpClient!.PostAsJsonAsync("/api/test/emails", new
        {
            to = inbox.EmailAddress,
            subject = $"Mixed Auth Test {Guid.NewGuid():N}",
            auth = new
            {
                spf = "softfail",
                dkim = "pass",
                dmarc = "fail",
                reverseDns = true
            }
        });

        Skip.If(!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Test email API not available");
        response.EnsureSuccessStatusCode();

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        var auth = email.AuthResults!;
        auth.Spf!.Result.Should().Be(SpfStatus.SoftFail);
        auth.Dkim![0].Result.Should().Be(DkimStatus.Pass);
        auth.Dmarc!.Result.Should().Be(DmarcStatus.Fail);
        auth.ReverseDns!.Verified.Should().BeTrue();

        var validation = auth.Validate();
        validation.Passed.Should().BeFalse("SPF softfail and DMARC fail should cause overall failure");
        validation.SpfPassed.Should().BeFalse();
        validation.DkimPassed.Should().BeTrue();
        validation.DmarcPassed.Should().BeFalse();
        validation.ReverseDnsPassed.Should().BeTrue();
    }
}
