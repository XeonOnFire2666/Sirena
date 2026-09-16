using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SirenaDesktop.Core.Models;
using SirenaDesktop.Infrastructure.Persistence.Configurations;

namespace SirenaDesktop;

public class SirenaDbContext(IConfiguration configuration) : DbContext
{
    private readonly IConfiguration _configuration = configuration;

    public DbSet<User> User { get; set; }
    public DbSet<DayStats> DayStats { get; set; }
    public DbSet<PendingChanges> StatsExistsFlag { get; set; }
    public DbSet<HydratedUser> HydratedUsers { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<AppTagMap> AppTagMaps { get; set; }
    public DbSet<PendingChange> PendingChanges { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_configuration.GetConnectionString("MainString"));
    }

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

        return new SirenaDbContext(configuration);
    }
}