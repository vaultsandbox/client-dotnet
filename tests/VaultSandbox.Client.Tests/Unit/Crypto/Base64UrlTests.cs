using FluentAssertions;
using VaultSandbox.Client.Crypto;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Crypto;

public class Base64UrlTests
{
    [Fact]
    public void Encode_RoundTrip_ShouldReturnOriginalData()
    {
        // Arrange
        byte[] original = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        // Act
        string encoded = Base64Url.Encode(original);
        byte[] decoded = Base64Url.Decode(encoded);

        // Assert
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Encode_ShouldReplaceUrlUnsafeCharacters()
    {
        // Arrange - bytes that produce '+' and '/' in standard base64
        byte[] data = [0xfb, 0xff, 0xfe]; // produces +//+ in standard base64

        // Act
        string encoded = Base64Url.Encode(data);

        // Assert
        encoded.Should().NotContain("+");
        encoded.Should().NotContain("/");
        encoded.Should().NotContain("=");
    }

    [Fact]
    public void Encode_ShouldNotIncludePadding()
    {
        // Arrange - various lengths to test padding scenarios
        byte[] oneByteData = [0x01];
        byte[] twoBytesData = [0x01, 0x02];
        byte[] threeBytesData = [0x01, 0x02, 0x03];

        // Act & Assert
        Base64Url.Encode(oneByteData).Should().NotEndWith("=");
        Base64Url.Encode(twoBytesData).Should().NotEndWith("=");
        Base64Url.Encode(threeBytesData).Should().NotEndWith("=");
    }

    [Fact]
    public void Decode_ShouldHandleUrlSafeCharacters()
    {
        // Arrange - URL-safe base64 string
        string urlSafe = "AQ-_"; // '-' and '_' instead of '+' and '/'

        // Act
        byte[] decoded = Base64Url.Decode(urlSafe);

        // Assert
        decoded.Should().NotBeEmpty();
    }

    [Fact]
    public void Decode_ShouldHandleMissingPadding()
    {
        // Arrange - various padding scenarios
        string noPadding = "AQ"; // would need "==" padding

        // Act
        byte[] decoded = Base64Url.Decode(noPadding);

        // Assert
        decoded.Should().HaveCount(1);
        decoded[0].Should().Be(0x01);
    }

    [Fact]
    public void Encode_EmptyInput_ShouldReturnEmptyString()
    {
        // Arrange
        byte[] empty = [];

        // Act
        string encoded = Base64Url.Encode(empty);

        // Assert
        encoded.Should().BeEmpty();
    }

    [Fact]
    public void Decode_EmptyInput_ShouldReturnEmptyArray()
    {
        // Arrange
        string empty = "";

        // Act
        byte[] decoded = Base64Url.Decode(empty);

        // Assert
        decoded.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_LargeData_ShouldBeIdentical()
    {
        // Arrange
        byte[] largeData = new byte[10000];
        Random.Shared.NextBytes(largeData);

        // Act
        string encoded = Base64Url.Encode(largeData);
        byte[] decoded = Base64Url.Decode(encoded);

        // Assert
        decoded.Should().BeEquivalentTo(largeData);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(100)]
    public void RoundTrip_VariousLengths_ShouldBeIdentical(int length)
    {
        // Arrange
        byte[] data = new byte[length];
        Random.Shared.NextBytes(data);

        // Act
        string encoded = Base64Url.Encode(data);
        byte[] decoded = Base64Url.Decode(encoded);

        // Assert
        decoded.Should().BeEquivalentTo(data);
    }
}
