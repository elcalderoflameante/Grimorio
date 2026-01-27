using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Grimorio.SharedKernel;

namespace Grimorio.Infrastructure.Configuration;

/// <summary>
/// Configuración base para todas las entidades que heredan de BaseEntity.
/// Aplica convenciones de mapeo, soft delete, y índices automáticamente.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Mapeo de propiedades
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.BranchId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(e => e.UpdatedBy)
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(e => e.DeletedBy)
            .HasColumnType("uuid")
            .IsRequired(false);

        // Índices
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => new { e.BranchId, e.IsDeleted });

        // Soft delete global query filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
