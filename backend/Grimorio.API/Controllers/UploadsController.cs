using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/uploads")]
[AllowAnonymous]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public UploadsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("{**path}")]
    public IActionResult GetUpload(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest();

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        var normalizedPath = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, normalizedPath));
        var safeUploadsRoot = uploadsRoot.EndsWith(Path.DirectorySeparatorChar)
            ? uploadsRoot
            : uploadsRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(safeUploadsRoot, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(fullPath, contentType);
    }
}
