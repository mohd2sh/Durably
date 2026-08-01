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
            entity.HasKey(e => new { e.FlowName, e.RunId });
            entity.Property(e => e.FlowName).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.RunId).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.InstanceId).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.ContextJson).IsRequired();
            entity.Property(e => e.StepPathHash).HasMaxLength(64);
            entity.Property(e => e.FailedStep).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.LockedBy).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(e => new { e.Status, e.LockedUntil, e.CreatedAt })
                .HasDatabaseName("IX_durable_Executions_Status_LockedUntil_CreatedAt");
            entity.HasIndex(e => new { e.FlowName, e.InstanceId })
                .HasDatabaseName("IX_durable_Executions_Flow_Instance");

            // At most one open run (Running=0, Pending=3) per business instance.
            // Filter syntax is provider-specific; SQLite relies on engine FindOpen + Create race handling.
            if (Database.IsSqlServer())
            {
                entity.HasIndex(e => new { e.FlowName, e.InstanceId })
                    .IsUnique()
                    .HasFilter("[Status] IN (0, 3)")
                    .HasDatabaseName("IX_durable_Executions_Open_Flow_Instance");
            }
            else if (Database.IsNpgsql())
            {
                entity.HasIndex(e => new { e.FlowName, e.InstanceId })
                    .IsUnique()
                    .HasFilter("\"Status\" IN (0, 3)")
                    .HasDatabaseName("IX_durable_Executions_Open_Flow_Instance");
            }
        });

        modelBuilder.Entity<TraceEntity>(entity =>
        {
            entity.ToTable("Traces");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FlowName).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.RunId).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.InstanceId).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.Property(e => e.StepKey).HasMaxLength(DurablyLimits.IdentifierMaxLength);
            entity.HasIndex(e => new { e.FlowName, e.RunId, e.Timestamp })
                .HasDatabaseName("IX_durable_Traces_Flow_Run");
        });
    }
}
