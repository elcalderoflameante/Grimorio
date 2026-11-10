using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Grimorio.SharedKernel.Constants;

namespace Grimorio.API.Services.Help;

public sealed record HelpArticleSummary(string Id, string Title, string Summary, string Section,
    string Audience, string Kind, DateOnly VerifiedOn, string Verification);
public sealed record HelpArticleDetail(HelpArticleSummary Article, string Markdown);
public sealed record HelpIndex(string Edition, IReadOnlyList<HelpArticleSummary> Articles);

/// <summary>Only explicitly catalogued operational documents can leave the API.</summary>
public sealed class HelpCatalog
{
    private const string Prefix = "Grimorio.Help.";
    private readonly Assembly assembly = typeof(HelpCatalog).Assembly;
    private readonly Catalog catalog;
    private readonly Dictionary<string, Article> articles;
    private readonly Dictionary<string, Asset> assets;
    private readonly Dictionary<string, string> bodies;
    private static readonly Regex IdPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    public HelpCatalog()
    {
        using var stream = Resource("catalog.json");
        catalog = JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Missing help catalogue.");
        var permissions = AppConstants.Permissions.All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        articles = new(StringComparer.Ordinal);
        assets = new(StringComparer.Ordinal);
        bodies = new(StringComparer.Ordinal);
        foreach (var article in catalog.Articles)
        {
            if (!IdPattern.IsMatch(article.Id) || article.File != $"articles/{article.Id}.md"
                || string.IsNullOrWhiteSpace(article.Title) || article.VerifiedOn == default
                || article.PermissionSets.Any(set => set.Length == 0 || set.Any(p => !permissions.Contains(p)))
                || !articles.TryAdd(article.Id, article))
                throw new InvalidOperationException("Invalid help article metadata.");
            using var reader = new StreamReader(Resource(article.File), Encoding.UTF8);
            bodies.Add(article.Id, reader.ReadToEnd());
        }
        foreach (var asset in catalog.Assets)
        {
            if (!IdPattern.IsMatch(asset.Id) || asset.File != $"assets/{asset.Id}.png"
                || asset.ArticleIds.Length == 0 || asset.ArticleIds.Any(id => !articles.ContainsKey(id))
                || !assets.TryAdd(asset.Id, asset))
                throw new InvalidOperationException("Invalid help asset metadata.");
        }
    }

    public HelpIndex List(ClaimsPrincipal user, string? query = null)
    {
        var terms = Normalize(query ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var visible = articles.Values.Where(a => CanRead(user, a));
        if (terms.Length > 0)
            visible = visible.Where(a => {
                var text = Normalize($"{a.Title} {a.Summary} {string.Join(' ', a.Tags)} {bodies[a.Id]}");
                return terms.All(text.Contains);
            });
        return new(catalog.Edition, visible.OrderBy(a => a.Order).Select(Summary).ToArray());
    }

    public HelpArticleDetail? Find(ClaimsPrincipal user, string id) =>
        articles.TryGetValue(id, out var article) && CanRead(user, article)
            ? new(Summary(article), bodies[id]) : null;

    public byte[]? Image(ClaimsPrincipal user, string id)
    {
        if (!assets.TryGetValue(id, out var asset) || !asset.ArticleIds.Any(a => CanRead(user, articles[a]))) return null;
        using var stream = Resource(asset.File);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static HelpArticleSummary Summary(Article a) => new(a.Id, a.Title, a.Summary, a.Section,
        a.Audience, a.Kind, a.VerifiedOn, a.Verification);

    private static bool CanRead(ClaimsPrincipal user, Article article) => user.Identity?.IsAuthenticated == true
        && (user.IsInRole(AppConstants.Roles.Admin) || article.PermissionSets.Length == 0
            || article.PermissionSets.Any(set => set.All(p => user.HasClaim(AppConstants.Claims.Permissions, p))));

    private Stream Resource(string file) => assembly.GetManifestResourceStream(Prefix + file)
        ?? throw new InvalidOperationException("A catalogued help resource is missing.");

    private static string Normalize(string text)
    {
        var result = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record Catalog(string Edition, Article[] Articles, Asset[] Assets);
    private sealed record Article(string Id, string Title, string Summary, string Section, string Audience,
        string Kind, int Order, DateOnly VerifiedOn, string Verification, string File, string[] Tags, string[][] PermissionSets);
    private sealed record Asset(string Id, string File, string[] ArticleIds);
}
