using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Menu.Queries;

public class GetMenuCategoriesQuery : IRequest<List<MenuCategoryDto>>
{
    public Guid BranchId { get; set; }
}

public class GetMenuItemsQuery : IRequest<List<MenuItemDto>>
{
    public Guid BranchId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? ActiveOnly { get; set; }
    public bool? AvailableOnly { get; set; }
    public bool Lightweight { get; set; }
}

public class GetMenuItemDetailQuery : IRequest<MenuItemDetailDto?>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class GetMenuAvailabilityQuery : IRequest<List<MenuItemAvailabilityDto>>
{
    public Guid BranchId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public bool AvailableOnly { get; set; } = true;
}

public class GetMenuProfitabilityQuery : IRequest<List<MenuItemProfitabilityDto>>
{
    public Guid BranchId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public bool AvailableOnly { get; set; } = false;
}
