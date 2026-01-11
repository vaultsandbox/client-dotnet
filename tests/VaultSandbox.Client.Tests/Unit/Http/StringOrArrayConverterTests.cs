using System.Text.Json;
using FluentAssertions;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Http;

public class StringOrArrayConverterTests
{
    private readonly JsonSerializerOptions _options;

    public StringOrArrayConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new StringOrArrayConverter());
    }

    [Fact]
    public void Read_SingleString_ShouldReturnArrayWithOneElement()
    {
        // Arrange
        var json = "\"test@example.com\"";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "test@example.com" });
    }

    [Fact]
    public void Read_EmptyString_ShouldReturnArrayWithEmptyString()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "" });
    }

    [Fact]
    public void Read_Array_ShouldReturnArrayWithAllElements()
    {
        // Arrange
        var json = "[\"alice@example.com\", \"bob@example.com\", \"charlie@example.com\"]";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[]
        {
            "alice@example.com",
            "bob@example.com",
            "charlie@example.com"
        });
    }

    [Fact]
    public void Read_EmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        var json = "[]";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Read_ArrayWithSingleElement_ShouldReturnArrayWithOneElement()
    {
        // Arrange
        var json = "[\"single@example.com\"]";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "single@example.com" });
    }

    [Fact]
    public void Read_ArrayWithNullElement_ShouldSkipNullElement()
    {
        // Arrange
        var json = "[\"valid@example.com\", null, \"another@example.com\"]";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "valid@example.com", "another@example.com" });
    }

    [Theory]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("{}")]
    public void Read_InvalidTokenType_ShouldThrowJsonException(string json)
    {
        // Act
        Action act = () => JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Read_NullToken_ShouldReturnNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert - null JSON value returns null (handled by nullable deserialization)
        result.Should().BeNull();
    }

    [Fact]
    public void Write_ArrayWithMultipleElements_ShouldWriteAsArray()
    {
        // Arrange
        var value = new[] { "alice@example.com", "bob@example.com" };

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.Should().Be("[\"alice@example.com\",\"bob@example.com\"]");
    }

    [Fact]
    public void Write_ArrayWithSingleElement_ShouldWriteAsArray()
    {
        // Arrange
        var value = new[] { "single@example.com" };

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.Should().Be("[\"single@example.com\"]");
    }

    [Fact]
    public void Write_EmptyArray_ShouldWriteEmptyArray()
    {
        // Arrange
        var value = Array.Empty<string>();

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        json.Should().Be("[]");
    }

    [Fact]
    public void RoundTrip_SingleString_ShouldPreserveValue()
    {
        // Arrange
        var original = "\"test@example.com\"";

        // Act
        var deserialized = JsonSerializer.Deserialize<string[]>(original, _options);
        var serialized = JsonSerializer.Serialize(deserialized, _options);
        var final = JsonSerializer.Deserialize<string[]>(serialized, _options);

        // Assert
        final.Should().BeEquivalentTo(new[] { "test@example.com" });
    }

    [Fact]
    public void RoundTrip_Array_ShouldPreserveValues()
    {
        // Arrange
        var value = new[] { "alice@example.com", "bob@example.com" };

        // Act
        var serialized = JsonSerializer.Serialize(value, _options);
        var deserialized = JsonSerializer.Deserialize<string[]>(serialized, _options);

        // Assert
        deserialized.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void Read_ArrayWithMixedValidAndNullElements_ShouldOnlyIncludeValidStrings()
    {
        // Arrange
        var json = "[null, \"first@example.com\", null, \"second@example.com\", null]";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "first@example.com", "second@example.com" });
    }

    [Fact]
    public void Read_StringWithSpecialCharacters_ShouldPreserveContent()
    {
        // Arrange
        var json = "\"test+special.chars@example.com\"";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "test+special.chars@example.com" });
    }

    [Fact]
    public void Read_StringWithUnicodeCharacters_ShouldPreserveContent()
    {
        // Arrange
        var json = "\"tëst@examplé.com\"";

        // Act
        var result = JsonSerializer.Deserialize<string[]>(json, _options);

        // Assert
        result.Should().BeEquivalentTo(new[] { "tëst@examplé.com" });
    }
}
