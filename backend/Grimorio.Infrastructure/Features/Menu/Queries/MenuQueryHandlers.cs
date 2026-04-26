using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Menu.Queries;

public class GetMenuCategoriesHandler : IRequestHandler<GetMenuCategoriesQuery, List<MenuCategoryDto>>
{
    private readonly GrimorioDbContext _db;
    public GetMenuCategoriesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuCategoryDto>> Handle(GetMenuCategoriesQuery req, CancellationToken ct) =>
        await _db.MenuCategories
            .Where(x => x.BranchId == req.BranchId)
            .OrderBy(x => x.Order).ThenBy(x => x.Name)
            .Select(x => new MenuCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Color = x.Color,
                Order = x.Order,
                IsActive = x.IsActive,
                TotalItems = x.Items.Count(i => !i.IsDeleted),
            })
            .ToListAsync(ct);
}

public class GetItemsMenuHandler : IRequestHandler<GetMenuItemsQuery, List<MenuItemDto>>
{
    private readonly GrimorioDbContext _db;
    public GetItemsMenuHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuItemDto>> Handle(GetMenuItemsQuery req, CancellationToken ct)
    {
        var query = _db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Station)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
            .Where(x => x.BranchId == req.BranchId);

        if (req.CategoryId.HasValue) query = query.Where(x => x.MenuCategoryId == req.CategoryId.Value);
        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);
        if (req.AvailableOnly == true) query = query.Where(x => x.AvailableForSale);

        return await query
            .OrderBy(x => x.Category!.Order).ThenBy(x => x.Name)
            .Select(x => new MenuItemDto
            {
                Id = x.Id,
                MenuCategoryId = x.MenuCategoryId,
                CategoryName = x.Category!.Name,
                CategoryColor = x.Category.Color,
                Name = x.Name,
                Description = x.Description,
                InternalCode = x.InternalCode,
                Price = x.Price,
                IsActive = x.IsActive,
                AvailableForSale = x.AvailableForSale,
                TotalIngredients = x.Recipe.Count(r => !r.IsDeleted),
                StationId = x.StationId,
                StationName = x.Station != null ? x.Station.Name : null,
            })
            .ToListAsync(ct);
    }
}

public class GetItemMenuDetalleHandler : IRequestHandler<GetMenuItemDetailQuery, MenuItemDetailDto?>
{
    private readonly GrimorioDbContext _db;
    public GetItemMenuDetalleHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuItemDetailDto?> Handle(GetMenuItemDetailQuery req, CancellationToken ct)
    {
        var item = await _db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Station)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct);

        if (item is null) return null;

        return new MenuItemDetailDto
        {
            Id = item.Id,
            MenuCategoryId = item.MenuCategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            CategoryColor = item.Category?.Color,
            Name = item.Name,
            Description = item.Description,
            InternalCode = item.InternalCode,
            Price = item.Price,
            IsActive = item.IsActive,
            AvailableForSale = item.AvailableForSale,
            TotalIngredients = item.Recipe.Count,
            StationId = item.StationId,
            StationName = item.Station?.Name,
            Recipe = item.Recipe.Select(r => new RecipeIngredientDto
            {
                Id = r.Id,
                ArticleId = r.ArticleId,
                ArticleName = r.Article?.Name ?? string.Empty,
                InternalCode = r.Article?.InternalCode,
                UnitId = r.UnitId,
                UnitName = r.Unit?.Name ?? string.Empty,
                UnitSymbol = r.Unit?.Symbol ?? string.Empty,
                Quantity = r.Quantity,
                Notes = r.Notes,
            }).ToList(),
        };
    }
}

