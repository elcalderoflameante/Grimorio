using Grimorio.API.Services.Help;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController, Route("api/help"), Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HelpController(HelpCatalog catalog, IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    private bool Enabled => environment.IsDevelopment() || configuration.GetValue<bool>("Help:Enabled");

    [HttpGet("articles")]
    public ActionResult<HelpIndex> List([FromQuery] string? q)
    {
        if (!Enabled) return NotFound();
        if (q?.Length > 120) return BadRequest(new { message = "Busca con hasta 120 caracteres." });
        return Ok(catalog.List(User, q));
    }

    [HttpGet("articles/{id}")]
    public ActionResult<HelpArticleDetail> Detail(string id)
    {
        if (!Enabled) return NotFound();
        var article = catalog.Find(User, id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpGet("assets/{id}")]
    public IActionResult Image(string id)
    {
        if (!Enabled) return NotFound();
        var bytes = catalog.Image(User, id);
        if (bytes is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(bytes, "image/png");
    }
}
