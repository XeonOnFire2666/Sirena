using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaAPI.Models;

namespace SirenaAPI.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.HasIndex(c => c.GoogleId).IsUnique();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
