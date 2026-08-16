using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.UnitTests;

public sealed class EfStoreOptionsAndModelTests
{
    private const string DefaultSchema = "durable";
    private const int ExpectedFlowNameMaxLength = 200;
    private const int ExpectedRunIdMaxLength = 200;
    private const int DefaultCommandTimeoutSeconds = 30;

    [Fact]
    public void EfStoreOptions_defaults()
    {
        // Arrange / Act
        var options = new EfStoreOptions();

        // Assert
        Assert.Equal(DefaultSchema, options.Schema);
        Assert.False(options.AutoMigrate);
        Assert.Equal(DefaultCommandTimeoutSeconds, options.CommandTimeoutSeconds);
    }

    [Fact]
    public void DurablyDbContext_model_has_composite_key_and_concurrency_token()
    {
        // Arrange
        var storeOptions = new EfStoreOptions { Schema = DefaultSchema };
        var dbOptions = new DbContextOptionsBuilder<DurablyDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new DurablyDbContext(dbOptions, storeOptions);
        var entityType = context.Model.FindEntityType(typeof(ExecutionEntity));

        // Act
        Assert.NotNull(entityType);
        var primaryKey = entityType!.FindPrimaryKey();
        var flowName = entityType.FindProperty(nameof(ExecutionEntity.FlowName));
        var runId = entityType.FindProperty(nameof(ExecutionEntity.RunId));
        var version = entityType.FindProperty(nameof(ExecutionEntity.Version));

        // Assert — identity is (FlowName, RunId); InstanceId is the business key, not part of the PK.
        Assert.NotNull(primaryKey);
        Assert.Equal(2, primaryKey!.Properties.Count);
        Assert.Contains(primaryKey.Properties, p => p.Name == nameof(ExecutionEntity.FlowName));
        Assert.Contains(primaryKey.Properties, p => p.Name == nameof(ExecutionEntity.RunId));
        Assert.Equal(ExpectedFlowNameMaxLength, flowName!.GetMaxLength());
        Assert.Equal(ExpectedRunIdMaxLength, runId!.GetMaxLength());
        Assert.True(version!.IsConcurrencyToken);
    }
}
