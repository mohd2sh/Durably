using Microsoft.EntityFrameworkCore;

namespace Durably;

internal sealed class DurablyDbContext : DbContext
{
    private readonly EfStoreOptions _storeOptions;

    public DurablyDbContext(DbContextOptions<DurablyDbContext> options, EfStoreOptions storeOptions)
        : base(options)
    {
        _storeOptions = storeOptions ?? throw new ArgumentNullException(nameof(storeOptions));
    }

    public DbSet<ExecutionEntity> Executions => Set<ExecutionEntity>();

    public DbSet<TraceEntity> Traces => Set<TraceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = string.IsNullOrWhiteSpace(_storeOptions.Schema) ? "durable" : _storeOptions.Schema;
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.Entity<ExecutionEntity>(entity =>
        {
            entity.ToTable("Executions");
            entity.HasKey(e => new { e.FlowName, e.InstanceId });
            entity.Property(e => e.FlowName).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.InstanceId).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.ContextJson).IsRequired();
            entity.Property(e => e.StepPathHash).HasMaxLength(64);
            entity.Property(e => e.FailedStep).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.LockedBy).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(e => new { e.Status, e.LockedUntil, e.CreatedAt })
                .HasDatabaseName("IX_durable_Executions_Status_LockedUntil_CreatedAt");
        });

        modelBuilder.Entity<TraceEntity>(entity =>
        {
            entity.ToTable("Traces");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FlowName).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.InstanceId).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.StepKey).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.HasIndex(e => new { e.FlowName, e.InstanceId, e.Timestamp })
                .HasDatabaseName("IX_durable_Traces_Flow_Instance");
        });
    }
}
