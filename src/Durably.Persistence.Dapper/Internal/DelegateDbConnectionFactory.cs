using System.Data.Common;

namespace Durably;

/// <summary>An <see cref="IDbConnectionFactory"/> backed by a delegate (e.g. <c>() =&gt; new SqlConnection(cs)</c>).</summary>
internal sealed class DelegateDbConnectionFactory : IDbConnectionFactory
{
    private readonly Func<DbConnection> _factory;

    public DelegateDbConnectionFactory(Func<DbConnection> factory)
        => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public DbConnection Create() => _factory();
}
