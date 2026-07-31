using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Durably;

/// <summary>Design-time factory for EF tooling. SQL Server is the primary authoring provider.</summary>
internal sealed class DurablyDbContextDesignTimeFactory : IDesignTimeDbContextFactory<DurablyDbContext>
{
    public DurablyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DurablyDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=DurablyDesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new DurablyDbContext(optionsBuilder.Options, new EfStoreOptions());
    }
}
