using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Infrastructure.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");
        builder.HasKey(w => w.Id);

        builder.HasMany(w => w.Todos)
               .WithOne()
               .HasForeignKey(t => t.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Todos).AutoInclude();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
    }   
}