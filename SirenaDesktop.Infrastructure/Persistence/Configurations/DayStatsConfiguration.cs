using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Infrastructure.Persistence.Configurations;

public class DayStatsConfiguration : IEntityTypeConfiguration<DayStats>
{
    public void Configure(EntityTypeBuilder<DayStats> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(c => c.Tags).ToJson();

        builder.Property(c => c.ScoredCommitShas)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v)
                    ? new HashSet<string>()
                    : JsonSerializer.Deserialize<HashSet<string>>(v, (JsonSerializerOptions?)null) ??
                      new HashSet<string>())
            .HasColumnName("ScoredCommitShas");

        builder.HasIndex(c => new { c.UserId, c.Date }).IsUnique();
    }
}