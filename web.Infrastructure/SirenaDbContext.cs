using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SirenaAPI.Models;
using SirenaAPI.Logic.Entities;
using SirenaAPI.Configurations;
using SirenaAPI.Infrastructure.Configurations;

namespace SirenaAPI.Infrastructure;

public class SirenaDbContext(IConfiguration configuration) : DbContext
{
    private readonly IConfiguration _configuration = configuration;

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<DayStats> DayStats { get; set; }

    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("MainString"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new DayStatsConfiguration());
        modelBuilder.ApplyConfiguration(new WorkspaceConfiguration());
        modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
    }
}