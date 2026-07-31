using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.UnitTests;

public sealed class ExecutionMapperTests
{
    private const string FlowName = "orders";
    private const string InstanceId = "ord-1";
    private const string ContextJson = "{\"value\":1}";
    private const string FailedStep = "email";
    private const string ErrorMessage = "smtp down";
    private const string RunnerId = "runner-1";
    private const string MetadataJson = "{\"customerId\":\"c1\"}";
    private const long Version = 3;
    private const int CurrentStep = 2;
    private const int Attempts = 1;

    [Fact]
    public void ToEntity_and_ToRecord_round_trip_fields()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero);
        var lockedUntil = new DateTimeOffset(2026, 1, 15, 11, 30, 0, TimeSpan.Zero);
        var record = new ExecutionRecord
        {
            FlowName = FlowName,
            InstanceId = InstanceId,
            Status = ExecutionStatus.Failed,
            CurrentStep = CurrentStep,
            ContextJson = ContextJson,
            Attempts = Attempts,
            FailedStep = FailedStep,
            ErrorMessage = ErrorMessage,
            Version = Version,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LockedBy = RunnerId,
            LockedUntil = lockedUntil,
            MetadataJson = MetadataJson
        };

        // Act
        var entity = ExecutionMapper.ToEntity(record);
        var restored = ExecutionMapper.ToRecord(entity);

        // Assert
        Assert.Equal(FlowName, restored.FlowName);
        Assert.Equal(InstanceId, restored.InstanceId);
        Assert.Equal(ExecutionStatus.Failed, restored.Status);
        Assert.Equal(CurrentStep, restored.CurrentStep);
        Assert.Equal(ContextJson, restored.ContextJson);
        Assert.Equal(Attempts, restored.Attempts);
        Assert.Equal(FailedStep, restored.FailedStep);
        Assert.Equal(ErrorMessage, restored.ErrorMessage);
        Assert.Equal(Version, restored.Version);
        Assert.Equal(createdAt, restored.CreatedAt);
        Assert.Equal(updatedAt, restored.UpdatedAt);
        Assert.Equal(RunnerId, restored.LockedBy);
        Assert.Equal(lockedUntil, restored.LockedUntil);
        Assert.Equal(MetadataJson, restored.MetadataJson);
    }

    [Fact]
    public void ToEntity_maps_null_lease_and_metadata()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var record = new ExecutionRecord
        {
            FlowName = FlowName,
            InstanceId = InstanceId,
            Status = ExecutionStatus.Pending,
            ContextJson = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Act
        var entity = ExecutionMapper.ToEntity(record);

        // Assert
        Assert.Null(entity.LockedBy);
        Assert.Null(entity.LockedUntil);
        Assert.Null(entity.MetadataJson);
    }
}
