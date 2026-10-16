namespace Grimorio.Application.Features.Menu;

public static class MenuImageUrls
{
    public const string DetailSuffix = "-optimized.webp";
    public const string ThumbnailSuffix = "-thumbnail.webp";

    public static string? Thumbnail(string? imageUrl) =>
        imageUrl?.StartsWith("/uploads/menu-items/", StringComparison.Ordinal) == true
        && imageUrl.EndsWith(DetailSuffix, StringComparison.Ordinal)
            ? imageUrl[..^DetailSuffix.Length] + ThumbnailSuffix
            : imageUrl;
}
