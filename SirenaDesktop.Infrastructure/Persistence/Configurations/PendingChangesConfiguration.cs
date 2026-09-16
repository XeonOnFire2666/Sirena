using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Infrastructure.Persistence.Configurations;

public class PendingChangesConfiguration : IEntityTypeConfiguration<PendingChange>
{
    public void Configure(EntityTypeBuilder<PendingChange> builder)
    {
        builder.HasKey(p => p.Id);
    }
}