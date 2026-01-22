using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// How to identify unique senders for greylisting.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GreylistTrackBy>))]
public enum GreylistTrackBy
{
    /// <summary>
    /// Track by sender IP address only.
    /// </summary>
    [JsonStringEnumMemberName("ip")]
    Ip,

    /// <summary>
    /// Track by sender email address only.
    /// </summary>
    [JsonStringEnumMemberName("sender")]
    Sender,

    /// <summary>
    /// Track by combination of IP and sender email.
    /// </summary>
    [JsonStringEnumMemberName("ip_sender")]
    IpSender
}

/// <summary>
/// Types of random errors that can be returned.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RandomErrorType>))]
public enum RandomErrorType
{
    /// <summary>
    /// 4xx temporary errors (421, 450, 451, 452).
    /// </summary>
    [JsonStringEnumMemberName("temporary")]
    Temporary,

    /// <summary>
    /// 5xx permanent errors (550, 551, 552, 553, 554).
    /// </summary>
    [JsonStringEnumMemberName("permanent")]
    Permanent
}
