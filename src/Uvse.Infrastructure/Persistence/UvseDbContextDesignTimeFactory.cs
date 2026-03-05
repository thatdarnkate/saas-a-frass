using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Uvse.Application.Common.Interfaces;

namespace Uvse.Infrastructure.Persistence;

/// <summary>
/// Used by dotnet-ef CLI tools (migrations add, migrations script, dbcontext scaffold) to
/// create a UvseDbContext without running the full application host. Not used at runtime.
/// </summary>
public sealed class UvseDbContextDesignTimeFactory : IDesignTimeDbContextFactory<UvseDbContext>
{
    public UvseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UvseDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=uvse;Username=uvse;Password=uvse")
            .Options;

        return new UvseDbContext(options, new DesignTimeTenantService());
    }

    /// <summary>Fixed tenant ID used only during design-time migration generation.</summary>
    private sealed class DesignTimeTenantService : ITenantService
    {
        public Guid TenantId => Guid.Empty;
    }
}
