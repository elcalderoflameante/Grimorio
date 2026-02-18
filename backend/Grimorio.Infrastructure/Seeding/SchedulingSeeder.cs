using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Seeding;

public class SchedulingSeeder
{
    public static async Task SeedSchedulingDataAsync(GrimorioDbContext context)
    {
        // Obtener una rama (la primera disponible)
        var branch = context.Branches.FirstOrDefault();
        if (branch == null)
            return; // No hay rama, salir

        var branchId = branch.Id;

        // Verificar si ya existen áreas
        var existingAreas = context.WorkAreas.Where(a => a.BranchId == branchId).ToList();
        if (existingAreas.Any())
            return; // Ya hay datos, no duplicar

        // Crear áreas de trabajo
        var areas = new List<WorkArea>
        {
            new WorkArea
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = "Cocina",
                Description = "Área de preparación de alimentos",
                Color = "#2ecc71",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new WorkArea
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = "Caja",
                Description = "Área de cajas registradoras",
                Color = "#3498db",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new WorkArea
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = "Bar",
                Description = "Área de bebidas",
                Color = "#e74c3c",
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new WorkArea
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = "Mesas",
                Description = "Área de servicio de mesas",
                Color = "#f39c12",
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            }
        };

        context.WorkAreas.AddRange(areas);
        await context.SaveChangesAsync();

        // Crear roles por área
        var roles = new List<WorkRole>();

        // Roles en Cocina
        var cocinaId = areas.First(a => a.Name == "Cocina").Id;
        roles.AddRange(new List<WorkRole>
        {
            new WorkRole
            {
                Id = Guid.NewGuid(),
                WorkAreaId = cocinaId,
                Name = "Parrillero",
                Description = "Responsable de la parrilla",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new WorkRole
            {
                Id = Guid.NewGuid(),
                WorkAreaId = cocinaId,
                Name = "Ayudante de Cocina",
                Description = "Ayudante en preparación de alimentos",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            }
        });

        // Roles en Caja
        var cajaId = areas.First(a => a.Name == "Caja").Id;
        roles.Add(new WorkRole
        {
            Id = Guid.NewGuid(),
            WorkAreaId = cajaId,
            Name = "Cajero",
            Description = "Responsable de caja",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        });

        // Roles en Bar
        var barId = areas.First(a => a.Name == "Bar").Id;
        roles.Add(new WorkRole
        {
            Id = Guid.NewGuid(),
            WorkAreaId = barId,
            Name = "Barman",
            Description = "Preparador de bebidas",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        });

        // Roles en Mesas
        var mesasId = areas.First(a => a.Name == "Mesas").Id;
        roles.Add(new WorkRole
        {
            Id = Guid.NewGuid(),
            WorkAreaId = mesasId,
            Name = "Mesero/Mesera",
            Description = "Servicio al cliente en mesas",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        });

        context.WorkRoles.AddRange(roles);
        await context.SaveChangesAsync();
    }
}
