using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaAPI.Logic.Entities;
using SirenaAPI.Models;

namespace SirenaAPI.Infrastructure.Configurations;

public class DayStatsConfiguration : IEntityTypeConfiguration<DayStats>
{
    public void Configure(EntityTypeBuilder<DayStats> builder)
    {
        builder.HasKey(c => c.Id);

        // IMPORTANT: this was Cascade. Logout calls _userService.Delete(), which deletes
        // the local User row — with Cascade, that silently wiped every local DayStats
        // row for the user, including anything not yet synced to the server. Local
        // tracking data must survive logout so the background worker can still push it
        // on next login. Restrict means a User row can't be deleted while DayStats rows
        // still reference it, which forces logout to stop deleting the User row (see fix below).
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(c => c.Tags).ToJson();

        // A user can only have one stats row per calendar day. This is the DB-level
        // backstop for the upsert logic in DayStatsRepository — even if a future code
        // path forgets to check for an existing row, the insert will fail loudly
        // instead of silently creating a duplicate day.
        builder.HasIndex(c => new { c.UserId, c.Date }).IsUnique();
    }
}