using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Infrastructure.Persistence.Configurations;

public class StatsExistsFlagConfiguration : IEntityTypeConfiguration<PendingChanges>
{
    public void Configure(EntityTypeBuilder<PendingChanges> builder)
    {
        builder.HasKey(c => new { c.UserId, c.DateOnly });
    }
}