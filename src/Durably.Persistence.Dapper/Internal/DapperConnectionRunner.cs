using System.Data;
using System.Data.Common;
using Dapper;

namespace Durably;

internal sealed class DapperConnectionRunner
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISqlDialect _dialect;
    private readonly DapperStoreOptions _options;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private bool _schemaEnsured;

    public DapperConnectionRunner(IDbConnectionFactory connectionFactory, ISqlDialect dialect, DapperStoreOptions options)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ISqlDialect Dialect => _dialect;

    public DapperStoreOptions Options => _options;

    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = _connectionFactory.Create();
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            await EnsureSchemaIfNeededAsync(connection, cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private async Task EnsureSchemaIfNeededAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (!_options.EnsureSchema || _schemaEnsured)
        {
            return;
        }

        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_schemaEnsured)
            {
                return;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(_dialect.EnsureSchemaSql, cancellationToken: cancellationToken,
                    commandTimeout: _options.CommandTimeoutSeconds)).ConfigureAwait(false);
            _schemaEnsured = true;
        }
        finally
        {
            _initGate.Release();
        }
    }
}
