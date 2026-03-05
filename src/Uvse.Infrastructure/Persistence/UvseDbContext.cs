using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Interfaces;
using Uvse.Domain.Common;
using Uvse.Domain.Plugins;
using Uvse.Domain.Summaries;

namespace Uvse.Infrastructure.Persistence;

public sealed class UvseDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantService _tenantService;

    public UvseDbContext(DbContextOptions<UvseDbContext> options, ITenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<TenantPlugin> TenantPlugins => Set<TenantPlugin>();
    public DbSet<GeneratedSummary> GeneratedSummaries => Set<GeneratedSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantPlugin>(builder =>
        {
            builder.ToTable("tenant_plugins");
            builder.HasKey(entity => entity.Id);
            builder.HasIndex(entity => new { entity.TenantId, entity.ProviderKey }).IsUnique();
            builder.Property(entity => entity.ProviderKey).HasMaxLength(128);
            builder.Property(entity => entity.EncryptedSettingsJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<GeneratedSummary>(builder =>
        {
            builder.ToTable("generated_summaries");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.ProviderKey).HasMaxLength(128);
            builder.Property(entity => entity.Title).HasMaxLength(256);
            builder.HasIndex(entity => new
            {
                entity.TenantId,
                entity.ProviderKey,
                entity.RequestedByUserId,
                entity.DetailLevel,
                entity.AudienceTone,
                entity.FromUtc,
                entity.ToUtc
            }).IsUnique();
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(ITenantOwned).IsAssignableFrom(type.ClrType)))
        {
            var method = typeof(UvseDbContext)
                .GetMethod(nameof(ApplyTenantQueryFilter), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType);
            method.Invoke(this, [modelBuilder]);
        }

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
    {
        Expression<Func<TEntity, bool>> filterExpression = entity => entity.TenantId == _tenantService.TenantId;
        modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
    }
}
