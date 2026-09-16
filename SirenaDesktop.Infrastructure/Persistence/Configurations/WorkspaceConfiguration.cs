using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Infrastructure.Persistence.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WorkspaceTypeId).IsRequired();

        builder.HasMany(w => w.Todos)
            .WithOne()
            .HasForeignKey(t => t.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Todos).AutoInclude();
    }
}