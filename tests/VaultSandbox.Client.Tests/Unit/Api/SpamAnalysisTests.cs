using System.Text.Json;
using FluentAssertions;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Api;

public class SpamAnalysisTests
{
    #region SpamSymbol Tests

    [Fact]
    public void SpamSymbol_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "name": "DKIM_SIGNED",
            "score": -0.5,
            "description": "DKIM signature present"
        }
        """;

        // Act
        var symbol = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Name.Should().Be("DKIM_SIGNED");
        symbol.Score.Should().Be(-0.5);
        symbol.Description.Should().Be("DKIM signature present");
        symbol.Options.Should().BeNull();
    }

    [Fact]
    public void SpamSymbol_DeserializesWithOptions()
    {
        // Arrange
        var json = """
        {
            "name": "SURBL_BLOCKED",
            "score": 5.5,
            "description": "URL is in SURBL blocklist",
            "options": ["http://malicious.com", "http://spam.com"]
        }
        """;

        // Act
        var symbol = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Name.Should().Be("SURBL_BLOCKED");
        symbol.Score.Should().Be(5.5);
        symbol.Description.Should().Be("URL is in SURBL blocklist");
        symbol.Options.Should().NotBeNull();
        symbol.Options.Should().HaveCount(2);
        symbol.Options.Should().Contain("http://malicious.com");
        symbol.Options.Should().Contain("http://spam.com");
    }

    [Fact]
    public void SpamSymbol_DeserializesWithNullDescription()
    {
        // Arrange
        var json = """
        {
            "name": "CUSTOM_RULE",
            "score": 1.0,
            "description": null
        }
        """;

        // Act
        var symbol = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Name.Should().Be("CUSTOM_RULE");
        symbol.Score.Should().Be(1.0);
        symbol.Description.Should().BeNull();
    }

    [Fact]
    public void SpamSymbol_SerializesCorrectly()
    {
        // Arrange
        var symbol = new SpamSymbol
        {
            Name = "TEST_RULE",
            Score = 2.5,
            Description = "Test description",
            Options = ["option1", "option2"]
        };

        // Act
        var json = JsonSerializer.Serialize(symbol);
        var deserialized = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("TEST_RULE");
        deserialized.Score.Should().Be(2.5);
        deserialized.Description.Should().Be("Test description");
        deserialized.Options.Should().HaveCount(2);
    }

    #endregion

    #region SpamAnalysisStatus Tests

    [Theory]
    [InlineData("\"analyzed\"", SpamAnalysisStatus.Analyzed)]
    [InlineData("\"skipped\"", SpamAnalysisStatus.Skipped)]
    [InlineData("\"error\"", SpamAnalysisStatus.Error)]
    public void SpamAnalysisStatus_DeserializesCorrectly(string json, SpamAnalysisStatus expected)
    {
        // Act
        var status = JsonSerializer.Deserialize<SpamAnalysisStatus>(json);

        // Assert
        status.Should().Be(expected);
    }

    [Theory]
    [InlineData(SpamAnalysisStatus.Analyzed, "\"Analyzed\"")]
    [InlineData(SpamAnalysisStatus.Skipped, "\"Skipped\"")]
    [InlineData(SpamAnalysisStatus.Error, "\"Error\"")]
    public void SpamAnalysisStatus_SerializesCorrectly(SpamAnalysisStatus status, string expected)
    {
        // Act - JsonStringEnumConverter serializes to PascalCase by default
        var json = JsonSerializer.Serialize(status);

        // Assert
        json.Should().Be(expected);
    }

    #endregion

    #region SpamAction and SpamActionConverter Tests

    [Theory]
    [InlineData("\"no action\"", SpamAction.NoAction)]
    [InlineData("\"greylist\"", SpamAction.Greylist)]
    [InlineData("\"add header\"", SpamAction.AddHeader)]
    [InlineData("\"rewrite subject\"", SpamAction.RewriteSubject)]
    [InlineData("\"soft reject\"", SpamAction.SoftReject)]
    [InlineData("\"reject\"", SpamAction.Reject)]
    public void SpamAction_DeserializesCorrectly(string json, SpamAction expected)
    {
        // Act
        var action = JsonSerializer.Deserialize<SpamAction>(json);

        // Assert
        action.Should().Be(expected);
    }

    [Theory]
    [InlineData(SpamAction.NoAction, "\"no action\"")]
    [InlineData(SpamAction.Greylist, "\"greylist\"")]
    [InlineData(SpamAction.AddHeader, "\"add header\"")]
    [InlineData(SpamAction.RewriteSubject, "\"rewrite subject\"")]
    [InlineData(SpamAction.SoftReject, "\"soft reject\"")]
    [InlineData(SpamAction.Reject, "\"reject\"")]
    public void SpamAction_SerializesCorrectly(SpamAction action, string expected)
    {
        // Act
        var json = JsonSerializer.Serialize(action);

        // Assert
        json.Should().Be(expected);
    }

    [Fact]
    public void SpamAction_ThrowsOnUnknownValue()
    {
        // Arrange
        var json = "\"unknown action\"";

        // Act
        var action = () => JsonSerializer.Deserialize<SpamAction>(json);

        // Assert
        action.Should().Throw<JsonException>()
            .WithMessage("*Unknown SpamAction value*");
    }

    [Fact]
    public void SpamAction_RoundTripsCorrectly()
    {
        // Test all enum values round-trip correctly
        foreach (SpamAction action in Enum.GetValues<SpamAction>())
        {
            // Act
            var json = JsonSerializer.Serialize(action);
            var deserialized = JsonSerializer.Deserialize<SpamAction>(json);

            // Assert
            deserialized.Should().Be(action, $"SpamAction.{action} should round-trip correctly");
        }
    }

    #endregion

    #region SpamAnalysisResult Tests

    [Fact]
    public void SpamAnalysisResult_DeserializesAnalyzedResult()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 3.5,
            "requiredScore": 6.0,
            "action": "no action",
            "isSpam": false,
            "processingTimeMs": 125,
            "symbols": [
                {
                    "name": "DKIM_SIGNED",
                    "score": -0.5,
                    "description": "DKIM signature present"
                },
                {
                    "name": "SPF_ALLOW",
                    "score": -0.2,
                    "description": "SPF check passed"
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SpamAnalysisStatus.Analyzed);
        result.Score.Should().Be(3.5);
        result.RequiredScore.Should().Be(6.0);
        result.Action.Should().Be(SpamAction.NoAction);
        result.IsSpam.Should().BeFalse();
        result.ProcessingTimeMs.Should().Be(125);
        result.Symbols.Should().NotBeNull();
        result.Symbols.Should().HaveCount(2);
        result.Info.Should().BeNull();
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesSpamEmail()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 12.5,
            "requiredScore": 6.0,
            "action": "reject",
            "isSpam": true,
            "processingTimeMs": 89,
            "symbols": [
                {
                    "name": "FORGED_SENDER",
                    "score": 8.0,
                    "description": "Forged sender address"
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SpamAnalysisStatus.Analyzed);
        result.Score.Should().Be(12.5);
        result.IsSpam.Should().BeTrue();
        result.Action.Should().Be(SpamAction.Reject);
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesSkippedResult()
    {
        // Arrange
        var json = """
        {
            "status": "skipped",
            "info": "Spam analysis disabled for this inbox"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SpamAnalysisStatus.Skipped);
        result.Score.Should().BeNull();
        result.RequiredScore.Should().BeNull();
        result.Action.Should().BeNull();
        result.IsSpam.Should().BeNull();
        result.Symbols.Should().BeNull();
        result.Info.Should().Be("Spam analysis disabled for this inbox");
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesErrorResult()
    {
        // Arrange
        var json = """
        {
            "status": "error",
            "info": "Rspamd service unavailable"
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SpamAnalysisStatus.Error);
        result.Score.Should().BeNull();
        result.Info.Should().Be("Rspamd service unavailable");
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesWithGreylistAction()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 4.5,
            "requiredScore": 6.0,
            "action": "greylist",
            "isSpam": false,
            "processingTimeMs": 50
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Action.Should().Be(SpamAction.Greylist);
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesWithAddHeaderAction()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 7.0,
            "requiredScore": 6.0,
            "action": "add header",
            "isSpam": true
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Action.Should().Be(SpamAction.AddHeader);
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesWithRewriteSubjectAction()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 8.5,
            "requiredScore": 6.0,
            "action": "rewrite subject",
            "isSpam": true
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Action.Should().Be(SpamAction.RewriteSubject);
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesWithSoftRejectAction()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 10.0,
            "requiredScore": 6.0,
            "action": "soft reject",
            "isSpam": true
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Action.Should().Be(SpamAction.SoftReject);
    }

    [Fact]
    public void SpamAnalysisResult_SerializesCorrectly()
    {
        // Arrange
        var result = new SpamAnalysisResult
        {
            Status = SpamAnalysisStatus.Analyzed,
            Score = 5.0,
            RequiredScore = 6.0,
            Action = SpamAction.NoAction,
            IsSpam = false,
            ProcessingTimeMs = 100,
            Symbols =
            [
                new SpamSymbol
                {
                    Name = "TEST_SYMBOL",
                    Score = 1.0,
                    Description = "Test"
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Status.Should().Be(SpamAnalysisStatus.Analyzed);
        deserialized.Score.Should().Be(5.0);
        deserialized.RequiredScore.Should().Be(6.0);
        deserialized.Action.Should().Be(SpamAction.NoAction);
        deserialized.IsSpam.Should().BeFalse();
        deserialized.ProcessingTimeMs.Should().Be(100);
        deserialized.Symbols.Should().HaveCount(1);
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesWithNegativeScore()
    {
        // Arrange - Score can be negative when email has many ham indicators
        var json = """
        {
            "status": "analyzed",
            "score": -2.5,
            "requiredScore": 6.0,
            "action": "no action",
            "isSpam": false
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Score.Should().Be(-2.5);
        result.IsSpam.Should().BeFalse();
    }

    [Fact]
    public void SpamAnalysisResult_DeserializesEmptySymbolsArray()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 0,
            "requiredScore": 6.0,
            "action": "no action",
            "isSpam": false,
            "symbols": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Symbols.Should().NotBeNull();
        result.Symbols.Should().BeEmpty();
    }

    #endregion

    #region Email Spam Methods Tests

    [Fact]
    public void Email_GetIsSpam_ReturnsTrue_WhenAnalyzedAndSpam()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Spam Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Analyzed,
                Score = 12.0,
                RequiredScore = 6.0,
                IsSpam = true,
                Action = SpamAction.Reject
            }
        };

        // Act
        var isSpam = email.GetIsSpam();

        // Assert
        isSpam.Should().BeTrue();
    }

    [Fact]
    public void Email_GetIsSpam_ReturnsFalse_WhenAnalyzedAndNotSpam()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Legitimate Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Analyzed,
                Score = 2.0,
                RequiredScore = 6.0,
                IsSpam = false,
                Action = SpamAction.NoAction
            }
        };

        // Act
        var isSpam = email.GetIsSpam();

        // Assert
        isSpam.Should().BeFalse();
    }

    [Fact]
    public void Email_GetIsSpam_ReturnsNull_WhenSpamAnalysisIsNull()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = null
        };

        // Act
        var isSpam = email.GetIsSpam();

        // Assert
        isSpam.Should().BeNull();
    }

    [Fact]
    public void Email_GetIsSpam_ReturnsNull_WhenStatusIsSkipped()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Skipped,
                Info = "Spam analysis disabled"
            }
        };

        // Act
        var isSpam = email.GetIsSpam();

        // Assert
        isSpam.Should().BeNull();
    }

    [Fact]
    public void Email_GetIsSpam_ReturnsNull_WhenStatusIsError()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Error,
                Info = "Rspamd unavailable"
            }
        };

        // Act
        var isSpam = email.GetIsSpam();

        // Assert
        isSpam.Should().BeNull();
    }

    [Fact]
    public void Email_GetSpamScore_ReturnsScore_WhenAnalyzed()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Analyzed,
                Score = 4.5,
                RequiredScore = 6.0,
                IsSpam = false,
                Action = SpamAction.NoAction
            }
        };

        // Act
        var score = email.GetSpamScore();

        // Assert
        score.Should().Be(4.5);
    }

    [Fact]
    public void Email_GetSpamScore_ReturnsNegativeScore_WhenHamIndicators()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Analyzed,
                Score = -3.2,
                RequiredScore = 6.0,
                IsSpam = false,
                Action = SpamAction.NoAction
            }
        };

        // Act
        var score = email.GetSpamScore();

        // Assert
        score.Should().Be(-3.2);
    }

    [Fact]
    public void Email_GetSpamScore_ReturnsNull_WhenSpamAnalysisIsNull()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = null
        };

        // Act
        var score = email.GetSpamScore();

        // Assert
        score.Should().BeNull();
    }

    [Fact]
    public void Email_GetSpamScore_ReturnsNull_WhenStatusIsSkipped()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Skipped,
                Info = "Spam analysis disabled"
            }
        };

        // Act
        var score = email.GetSpamScore();

        // Assert
        score.Should().BeNull();
    }

    [Fact]
    public void Email_GetSpamScore_ReturnsNull_WhenStatusIsError()
    {
        // Arrange
        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = new SpamAnalysisResult
            {
                Status = SpamAnalysisStatus.Error,
                Info = "Rspamd unavailable"
            }
        };

        // Act
        var score = email.GetSpamScore();

        // Assert
        score.Should().BeNull();
    }

    [Fact]
    public void Email_SpamAnalysis_PropertyIsAccessible()
    {
        // Arrange
        var spamResult = new SpamAnalysisResult
        {
            Status = SpamAnalysisStatus.Analyzed,
            Score = 5.0,
            RequiredScore = 6.0,
            Action = SpamAction.NoAction,
            IsSpam = false,
            ProcessingTimeMs = 100,
            Symbols =
            [
                new SpamSymbol { Name = "DKIM_SIGNED", Score = -0.5, Description = "DKIM present" }
            ]
        };

        var email = new Email
        {
            Id = "test-id",
            InboxId = "inbox-id",
            From = "sender@example.com",
            To = ["recipient@example.com"],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow,
            SpamAnalysis = spamResult
        };

        // Assert - verify all properties are accessible
        email.SpamAnalysis.Should().NotBeNull();
        email.SpamAnalysis!.Status.Should().Be(SpamAnalysisStatus.Analyzed);
        email.SpamAnalysis.Score.Should().Be(5.0);
        email.SpamAnalysis.RequiredScore.Should().Be(6.0);
        email.SpamAnalysis.Action.Should().Be(SpamAction.NoAction);
        email.SpamAnalysis.IsSpam.Should().BeFalse();
        email.SpamAnalysis.ProcessingTimeMs.Should().Be(100);
        email.SpamAnalysis.Symbols.Should().HaveCount(1);
        email.SpamAnalysis.Symbols![0].Name.Should().Be("DKIM_SIGNED");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SpamSymbol_HandlesZeroScore()
    {
        // Arrange
        var json = """
        {
            "name": "NEUTRAL_RULE",
            "score": 0
        }
        """;

        // Act
        var symbol = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Score.Should().Be(0);
    }

    [Fact]
    public void SpamSymbol_HandlesLargeScore()
    {
        // Arrange
        var json = """
        {
            "name": "VERY_SPAMMY",
            "score": 999.99
        }
        """;

        // Act
        var symbol = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Score.Should().Be(999.99);
    }

    [Fact]
    public void SpamSymbol_HandlesEmptyOptionsArray()
    {
        // Arrange
        var json = """
        {
            "name": "RULE_WITH_EMPTY_OPTIONS",
            "score": 1.0,
            "options": []
        }
        """;

        // Act
        var symbol = JsonSerializer.Deserialize<SpamSymbol>(json);

        // Assert
        symbol.Should().NotBeNull();
        symbol!.Options.Should().NotBeNull();
        symbol.Options.Should().BeEmpty();
    }

    [Fact]
    public void SpamAnalysisResult_HandlesZeroProcessingTime()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 0,
            "requiredScore": 6.0,
            "action": "no action",
            "isSpam": false,
            "processingTimeMs": 0
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ProcessingTimeMs.Should().Be(0);
    }

    [Fact]
    public void SpamAnalysisResult_HandlesHighProcessingTime()
    {
        // Arrange
        var json = """
        {
            "status": "analyzed",
            "score": 5.0,
            "requiredScore": 6.0,
            "action": "no action",
            "isSpam": false,
            "processingTimeMs": 10000
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ProcessingTimeMs.Should().Be(10000);
    }

    [Fact]
    public void SpamAnalysisResult_HandlesScoreAtThreshold()
    {
        // Arrange - Score exactly at required threshold
        var json = """
        {
            "status": "analyzed",
            "score": 6.0,
            "requiredScore": 6.0,
            "action": "add header",
            "isSpam": true
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<SpamAnalysisResult>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Score.Should().Be(6.0);
        result.RequiredScore.Should().Be(6.0);
        result.IsSpam.Should().BeTrue();
    }

    #endregion
}
