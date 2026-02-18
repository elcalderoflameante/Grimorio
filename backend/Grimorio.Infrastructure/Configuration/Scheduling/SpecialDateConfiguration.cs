using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Grimorio.Domain.Entities.Scheduling;

namespace Grimorio.Infrastructure.Configuration.Scheduling;

public class SpecialDateConfiguration : IEntityTypeConfiguration<SpecialDate>
{
    public void Configure(EntityTypeBuilder<SpecialDate> builder)
    {
        builder.ToTable("SpecialDates", "scheduling");
        
        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Description)
            .HasMaxLength(500);
        
        builder.Property(x => x.Date)
            .IsRequired();
        
        builder.HasIndex(x => new { x.BranchId, x.Date })
            .HasFilter("\"IsDeleted\" = false")
            .IsUnique();
            
        builder.HasMany(x => x.Templates)
            .WithOne(x => x.SpecialDate)
            .HasForeignKey(x => x.SpecialDateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpecialDateTemplateConfiguration : IEntityTypeConfiguration<SpecialDateTemplate>
{
    public void Configure(EntityTypeBuilder<SpecialDateTemplate> builder)
    {
        builder.ToTable("SpecialDateTemplates", "scheduling");
        
        builder.Property(x => x.StartTime)
            .IsRequired();
            
        builder.Property(x => x.EndTime)
            .IsRequired();
            
        builder.Property(x => x.RequiredCount)
            .IsRequired();
            
        builder.Property(x => x.Notes)
            .HasMaxLength(500);
            
        builder.HasOne(x => x.WorkArea)
            .WithMany()
            .HasForeignKey(x => x.WorkAreaId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.WorkRole)
            .WithMany()
            .HasForeignKey(x => x.WorkRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
