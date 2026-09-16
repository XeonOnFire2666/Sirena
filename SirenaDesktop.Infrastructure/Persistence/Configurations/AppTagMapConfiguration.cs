using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaDesktop.Core.Models;

public class AppTagMapConfiguration : IEntityTypeConfiguration<AppTagMap>
{
    public void Configure(EntityTypeBuilder<AppTagMap> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TrackKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.ColorHex)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(m => new { m.UserId, m.TrackKey }).IsUnique();
    }
}