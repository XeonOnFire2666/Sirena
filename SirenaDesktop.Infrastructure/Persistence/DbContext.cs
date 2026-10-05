using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.ExternalServices;
using SirenaDesktop.Infrastructure.Persistence.Configurations;
using System.IO;

namespace SirenaDesktop;

public class SirenaDbContext(DbContextOptions<SirenaDbContext> options) : DbContext(options)
{
    public DbSet<User> User { get; set; }
    public DbSet<DayStats> DayStats { get; set; }
    public DbSet<PendingChanges> StatsExistsFlag { get; set; }
    public DbSet<HydratedUser> HydratedUsers { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<AppTagMap> AppTagMaps { get; set; }
    public DbSet<PendingChange> PendingChanges { get; set; }
    public DbSet<SyncOutboxItem> SyncOutbox { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new DayStatsConfiguration());
        modelBuilder.ApplyConfiguration(new StatsExistsFlagConfiguration());
        modelBuilder.ApplyConfiguration(new AppTagMapConfiguration());
        modelBuilder.ApplyConfiguration(new WorkspaceConfiguration());
        modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
        modelBuilder.ApplyConfiguration(new PendingChangesConfiguration());

        modelBuilder.Entity<SyncOutboxItem>(e =>
        {
            e.ToTable("SyncOutbox");
            e.HasKey(x => new { x.EntityKind, x.EntityId });
        });

        modelBuilder.Entity<HydratedUser>().HasKey(c => c.UserId);
    }
}

public class SirenaDbContextFactory : IDesignTimeDbContextFactory<SirenaDbContext>
{
    public SirenaDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .Build();

        var connectionString = AppPaths.BuildConnectionString(configuration.GetConnectionString("MainString"));

        var options = new DbContextOptionsBuilder<SirenaDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new SirenaDbContext(options);
    }
}