using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Test collection that disables parallel execution for integration tests.
/// This prevents SMTP rate limiting and test isolation issues.
/// </summary>
[CollectionDefinition("Integration", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}

/// <summary>
/// Shared fixture for integration tests.
/// </summary>
public class IntegrationTestFixture
{
}
