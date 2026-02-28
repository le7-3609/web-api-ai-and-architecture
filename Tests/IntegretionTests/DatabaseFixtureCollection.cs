using Xunit;

namespace Tests.IntegrationTests
{
    [CollectionDefinition("Database collection")]
    public class DatabaseFixtureCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, just used to define collection fixture
    }
}
