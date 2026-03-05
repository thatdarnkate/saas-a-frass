using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Uvse.Application.Common.Interfaces;
using Uvse.Domain.Common;
using Uvse.Domain.Datasources;
using Uvse.Domain.Plugins;
using Uvse.Domain.Projects;
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

    public DbSet<Datasource> Datasources => Set<Datasource>();
    public DbSet<DatasourceUser> DatasourceUsers => Set<DatasourceUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectDatasource> ProjectDatasources => Set<ProjectDatasource>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<TenantPlugin> TenantPlugins => Set<TenantPlugin>();
    public DbSet<GeneratedSummary> GeneratedSummaries => Set<GeneratedSummary>();
    public DbSet<BibliographyEntry> BibliographyEntries => Set<BibliographyEntry>();

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
            builder.HasMany(entity => entity.BibliographyEntries)
                .WithOne(entry => entry.GeneratedSummary)
                .HasForeignKey(entry => entry.GeneratedSummaryId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(entity => new
            {
                entity.TargetType,
                entity.ProjectId,
                entity.DatasourceId,
                entity.CreatedAtUtc
            });
            builder.HasIndex(entity => new
            {
                entity.TenantId,
                entity.RequestedByUserId,
                entity.TargetType,
                entity.ProjectId,
                entity.DatasourceId,
                entity.FromUtc,
                entity.ToUtc
            });
        });

        modelBuilder.Entity<BibliographyEntry>(builder =>
        {
            builder.ToTable("bibliography_entries");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Hyperlink).HasMaxLength(2048);
            builder.Property(entity => entity.SourceText).HasColumnType("text");
            builder.HasIndex(entity => new { entity.GeneratedSummaryId, entity.Position }).IsUnique();
        });

        modelBuilder.Entity<Datasource>(builder =>
        {
            builder.ToTable("datasources");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).HasMaxLength(200);
            builder.Property(entity => entity.Type).HasMaxLength(100);
            builder.Property(entity => entity.ConnectionDetailsEncryptedJson).HasColumnType("jsonb");
            builder.HasIndex(entity => new { entity.TenantId, entity.Name }).IsUnique();
        });

        modelBuilder.Entity<DatasourceUser>(builder =>
        {
            builder.ToTable("datasource_users");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.UserId).HasMaxLength(200);
            builder.HasIndex(entity => new { entity.DatasourceId, entity.UserId }).IsUnique();
            builder.HasOne(entity => entity.Datasource)
                .WithMany(parent => parent.DatasourceUsers)
                .HasForeignKey(entity => entity.DatasourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(builder =>
        {
            builder.ToTable("projects");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).HasMaxLength(200);
            builder.HasIndex(entity => new { entity.TenantId, entity.Name }).IsUnique();
        });

        modelBuilder.Entity<ProjectUser>(builder =>
        {
            builder.ToTable("project_users");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.UserId).HasMaxLength(200);
            builder.HasIndex(entity => new { entity.ProjectId, entity.UserId }).IsUnique();
            builder.HasOne(entity => entity.Project)
                .WithMany(parent => parent.ProjectUsers)
                .HasForeignKey(entity => entity.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectDatasource>(builder =>
        {
            builder.ToTable("project_datasources");
            builder.HasKey(entity => entity.Id);
            builder.HasIndex(entity => new { entity.ProjectId, entity.DatasourceId }).IsUnique();
            builder.HasOne(entity => entity.Project)
                .WithMany(parent => parent.ProjectDatasources)
                .HasForeignKey(entity => entity.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.Datasource)
                .WithMany(parent => parent.ProjectDatasources)
                .HasForeignKey(entity => entity.DatasourceId)
                .OnDelete(DeleteBehavior.Cascade);
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
