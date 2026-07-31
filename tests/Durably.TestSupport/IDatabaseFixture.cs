using System.Data.Common;
using Xunit;

namespace Durably.TestSupport;

public interface IDatabaseFixture : IAsyncLifetime
{
    string ProviderName { get; }

    string ConnectionString { get; }

    DbConnection CreateConnection();

    IDurablyBuilder ConfigureDurably(IDurablyBuilder builder, Action<EfStoreOptions>? configure = null);

    Task ResetAsync();
}
