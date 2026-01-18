using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// JSON source generator for AOT compatibility and better performance.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(CheckKeyResponse))]
[JsonSerializable(typeof(ServerInfoResponse))]
[JsonSerializable(typeof(AlgorithmInfo))]
[JsonSerializable(typeof(Api.EncryptionPolicy))]
[JsonSerializable(typeof(CreateInboxRequest))]
[JsonSerializable(typeof(CreateInboxResponse))]
[JsonSerializable(typeof(DeleteAllInboxesResponse))]
[JsonSerializable(typeof(InboxSyncResponse))]
[JsonSerializable(typeof(EmailResponse))]
[JsonSerializable(typeof(EmailResponse[]))]
[JsonSerializable(typeof(RawEmailResponse))]
[JsonSerializable(typeof(SseEmailEvent))]
[JsonSerializable(typeof(EncryptedPayload))]
[JsonSerializable(typeof(AlgorithmSuite))]
[JsonSerializable(typeof(DecryptedMetadata))]
[JsonSerializable(typeof(DecryptedParsed))]
[JsonSerializable(typeof(AttachmentData))]
// Auth types from Api namespace (unified - no duplicate DTOs)
[JsonSerializable(typeof(Api.AuthenticationResults))]
[JsonSerializable(typeof(Api.SpfResult))]
[JsonSerializable(typeof(Api.DkimResult))]
[JsonSerializable(typeof(Api.DkimResult[]))]
[JsonSerializable(typeof(Api.DmarcResult))]
[JsonSerializable(typeof(Api.ReverseDnsResult))]
// Public types for file export/import
[JsonSerializable(typeof(InboxExport))]
[JsonSerializable(typeof(InboxSyncStatus))]
[JsonSerializable(typeof(AuthValidation))]
[JsonSerializable(typeof(string[]))]
// Webhook types
[JsonSerializable(typeof(CreateWebhookRequest))]
[JsonSerializable(typeof(UpdateWebhookRequest))]
[JsonSerializable(typeof(WebhookResponse))]
[JsonSerializable(typeof(WebhookResponse[]))]
[JsonSerializable(typeof(WebhookListResponse))]
[JsonSerializable(typeof(TestWebhookResponse))]
[JsonSerializable(typeof(RotateSecretResponse))]
[JsonSerializable(typeof(WebhookStatsResponse))]
[JsonSerializable(typeof(FilterConfigRequest))]
[JsonSerializable(typeof(FilterConfigResponse))]
[JsonSerializable(typeof(FilterRuleRequest))]
[JsonSerializable(typeof(FilterRuleRequest[]))]
[JsonSerializable(typeof(FilterRuleResponse))]
[JsonSerializable(typeof(FilterRuleResponse[]))]
[JsonSerializable(typeof(CustomTemplateRequest))]
[JsonSerializable(typeof(Api.WebhookEventType))]
[JsonSerializable(typeof(Api.WebhookEventType[]))]
[JsonSerializable(typeof(Api.FilterableField))]
[JsonSerializable(typeof(Api.FilterOperator))]
[JsonSerializable(typeof(Api.FilterMode))]
[JsonSerializable(typeof(Api.WebhookScope))]
[JsonSerializable(typeof(Api.WebhookDeliveryStatus))]
[ExcludeFromCodeCoverage]
internal partial class VaultSandboxJsonContext : JsonSerializerContext
{
}
