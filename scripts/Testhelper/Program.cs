using System.Text.Json;
using VaultSandbox.Client;
using VaultSandbox.Client.Api;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

if (args.Length < 1)
{
    Fatal("usage: testhelper <command> [args]");
}

var command = args[0];

await using var client = VaultSandboxClientBuilder.Create()
    .WithBaseUrl(Environment.GetEnvironmentVariable("VAULTSANDBOX_URL")!)
    .WithApiKey(Environment.GetEnvironmentVariable("VAULTSANDBOX_API_KEY")!)
    .Build();

try
{
    switch (command)
    {
        case "create-inbox":
            await CreateInbox(client);
            break;
        case "import-inbox":
            await ImportInbox(client);
            break;
        case "read-emails":
            await ReadEmails(client);
            break;
        case "cleanup":
            if (args.Length < 2)
            {
                Fatal("usage: testhelper cleanup <address>");
            }
            await Cleanup(client, args[1]);
            break;
        default:
            Fatal($"unknown command: {command}");
            break;
    }
}
catch (Exception ex)
{
    Fatal(ex.Message);
}

void Fatal(string message)
{
    Console.Error.WriteLine(message);
    Environment.Exit(1);
}

async Task CreateInbox(IVaultSandboxClient client)
{
    var inbox = await client.CreateInboxAsync();
    var exported = await inbox.ExportAsync();

    var output = new
    {
        version = exported.Version,
        emailAddress = exported.EmailAddress,
        expiresAt = exported.ExpiresAt.ToString("O"),
        inboxHash = exported.InboxHash,
        encrypted = exported.Encrypted,
        serverSigPk = exported.ServerSigPk,
        secretKey = exported.SecretKey,
        exportedAt = exported.ExportedAt.ToString("O")
    };

    Console.WriteLine(JsonSerializer.Serialize(output, jsonOptions));
}

async Task ImportInbox(IVaultSandboxClient client)
{
    var input = await Console.In.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(input);

    var exportData = new InboxExport
    {
        Version = data.GetProperty("version").GetInt32(),
        EmailAddress = data.GetProperty("emailAddress").GetString()!,
        ExpiresAt = DateTimeOffset.Parse(data.GetProperty("expiresAt").GetString()!),
        InboxHash = data.GetProperty("inboxHash").GetString()!,
        ServerSigPk = data.GetProperty("serverSigPk").GetString()!,
        SecretKey = data.GetProperty("secretKey").GetString()!,
        ExportedAt = DateTimeOffset.Parse(data.GetProperty("exportedAt").GetString()!)
    };

    await client.ImportInboxAsync(exportData);
    Console.WriteLine(JsonSerializer.Serialize(new { success = true }, jsonOptions));
}

async Task ReadEmails(IVaultSandboxClient client)
{
    var input = await Console.In.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(input);

    var exportData = new InboxExport
    {
        Version = data.GetProperty("version").GetInt32(),
        EmailAddress = data.GetProperty("emailAddress").GetString()!,
        ExpiresAt = DateTimeOffset.Parse(data.GetProperty("expiresAt").GetString()!),
        InboxHash = data.GetProperty("inboxHash").GetString()!,
        ServerSigPk = data.GetProperty("serverSigPk").GetString()!,
        SecretKey = data.GetProperty("secretKey").GetString()!,
        ExportedAt = DateTimeOffset.Parse(data.GetProperty("exportedAt").GetString()!)
    };

    var inbox = await client.ImportInboxAsync(exportData);
    var emails = await inbox.GetEmailsAsync();

    var output = new
    {
        emails = emails.Select(email => new
        {
            id = email.Id,
            subject = email.Subject,
            from = email.From,
            to = email.To,
            text = email.Text ?? "",
            html = email.Html ?? "",
            attachments = email.Attachments?.Select(att => new
            {
                filename = att.Filename,
                contentType = att.ContentType,
                size = att.Size
            }) ?? Enumerable.Empty<object>(),
            receivedAt = email.ReceivedAt.ToString("O")
        })
    };

    Console.WriteLine(JsonSerializer.Serialize(output, jsonOptions));
}

async Task Cleanup(IVaultSandboxClient client, string address)
{
    await client.DeleteInboxAsync(address);
    Console.WriteLine(JsonSerializer.Serialize(new { success = true }, jsonOptions));
}
