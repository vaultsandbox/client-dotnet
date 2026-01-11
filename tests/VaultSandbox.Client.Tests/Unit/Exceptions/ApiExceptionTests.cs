using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class ApiExceptionTests
{
    [Fact]
    public void Constructor_WithStatusCodeAndMessage_ShouldSetProperties()
    {
        // Arrange
        const int statusCode = 404;
        const string message = "Resource not found";

        // Act
        var exception = new ApiException(statusCode, message);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(message);
        exception.ResponseBody.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithStatusCodeMessageAndResponseBody_ShouldSetAllProperties()
    {
        // Arrange
        const int statusCode = 400;
        const string message = "Bad request";
        const string responseBody = "{\"error\": \"Invalid input\"}";

        // Act
        var exception = new ApiException(statusCode, message, responseBody);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(message);
        exception.ResponseBody.Should().Be(responseBody);
    }

    [Fact]
    public void Constructor_WithNullResponseBody_ShouldAllowNull()
    {
        // Arrange
        const int statusCode = 500;
        const string message = "Internal server error";

        // Act
        var exception = new ApiException(statusCode, message, responseBody: null);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(message);
        exception.ResponseBody.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        const int statusCode = 503;
        const string message = "Service unavailable";
        var innerException = new InvalidOperationException("Connection failed");

        // Act
        var exception = new ApiException(statusCode, message, innerException);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ResponseBody.Should().BeNull();
    }

    [Theory]
    [InlineData(200, "OK")]
    [InlineData(201, "Created")]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(502, "Bad Gateway")]
    [InlineData(503, "Service Unavailable")]
    public void Constructor_VariousStatusCodes_ShouldHandleAllCodes(int statusCode, string message)
    {
        // Act
        var exception = new ApiException(statusCode, message);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAllowEmpty()
    {
        // Arrange
        const int statusCode = 400;
        const string message = "";

        // Act
        var exception = new ApiException(statusCode, message);

        // Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyResponseBody_ShouldAllowEmpty()
    {
        // Arrange
        const int statusCode = 400;
        const string message = "Bad request";
        const string responseBody = "";

        // Act
        var exception = new ApiException(statusCode, message, responseBody);

        // Assert
        exception.ResponseBody.Should().BeEmpty();
    }

    [Fact]
    public void ApiException_ShouldInheritFromVaultSandboxException()
    {
        // Arrange & Act
        var exception = new ApiException(400, "Test");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithJsonResponseBody_ShouldPreserveJsonFormat()
    {
        // Arrange
        const int statusCode = 422;
        const string message = "Validation failed";
        const string responseBody = @"{
            ""errors"": [
                {""field"": ""email"", ""message"": ""Invalid format""},
                {""field"": ""name"", ""message"": ""Required""}
            ]
        }";

        // Act
        var exception = new ApiException(statusCode, message, responseBody);

        // Assert
        exception.ResponseBody.Should().Contain("errors");
        exception.ResponseBody.Should().Contain("Invalid format");
    }

    [Fact]
    public void Constructor_WithLargeResponseBody_ShouldHandleLargeContent()
    {
        // Arrange
        const int statusCode = 500;
        const string message = "Server error";
        var responseBody = new string('x', 10000);

        // Act
        var exception = new ApiException(statusCode, message, responseBody);

        // Assert
        exception.ResponseBody.Should().HaveLength(10000);
    }

    [Fact]
    public void Constructor_WithUnicodeMessage_ShouldPreserveUnicode()
    {
        // Arrange
        const int statusCode = 400;
        const string message = "错误: 无效输入 🚫";
        const string responseBody = "Détails de l'erreur: données invalides";

        // Act
        var exception = new ApiException(statusCode, message, responseBody);

        // Assert
        exception.Message.Should().Be(message);
        exception.ResponseBody.Should().Be(responseBody);
    }

    [Fact]
    public void Constructor_WithNestedInnerException_ShouldPreserveChain()
    {
        // Arrange
        const int statusCode = 500;
        const string message = "API call failed";
        var rootCause = new ArgumentException("Invalid argument");
        var innerException = new InvalidOperationException("Operation failed", rootCause);

        // Act
        var exception = new ApiException(statusCode, message, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootCause);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(600)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNonStandardStatusCodes_ShouldAcceptAnyInteger(int statusCode)
    {
        // Act
        var exception = new ApiException(statusCode, "Test");

        // Assert
        exception.StatusCode.Should().Be(statusCode);
    }
}
