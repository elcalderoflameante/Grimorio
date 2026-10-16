using System.Reflection;
using System.Security.Claims;
using Grimorio.API.Controllers;
using Grimorio.API.Services;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using QuestPDF.Fluent;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
byte[] Encode(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
{
    using var bitmap = new SKBitmap(width, height);
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.Transparent);
    using var paint = new SKPaint();
    var colors = new[] { SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow };
    for (var i = 0; i < 4; i++)
    {
        paint.Color = colors[i];
        canvas.DrawRect(i % 2 * width / 2f, i / 2 * height / 2f, width / 2f, height / 2f, paint);
    }
    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(format, 95);
    return data.ToArray();
}
async Task<ProcessedMenuImage> Process(byte[] data, CancellationToken ct = default) =>
    await MenuImageProcessor.ProcessAsync(new MemoryStream(data), ct);
async Task Rejected(byte[] data, string name)
{
    try { await Process(data); throw new Exception("Accepted: " + name); }
    catch (ArgumentException) { checks++; }
}
void Dimensions(byte[] data, int width, int height, string name)
{
    using var codec = SKCodec.Create(SKData.CreateCopy(data));
    Check(codec.EncodedFormat == SKEncodedImageFormat.Webp, name + " WebP");
    Check(codec.Info.Width == width && codec.Info.Height == height, name + " dimensions");
    Check(codec.EncodedOrigin == SKEncodedOrigin.TopLeft, name + " normalized orientation");
}

var large = await Process(Encode(2400, 1800));
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var pdf = Document.Create(document => document.Page(page => page.Content().Image(large.Detail))).GeneratePdf();
Check(pdf.Length > 0, "WebP compatible with operational sheet PDFs");
Dimensions(large.Detail, 1600, 1200, "Detail");
Dimensions(large.Thumbnail, 320, 240, "Thumbnail");
var small = await Process(Encode(180, 120));
Dimensions(small.Detail, 180, 120, "Small detail not enlarged");
Dimensions(small.Thumbnail, 180, 120, "Small thumbnail not enlarged");
Dimensions((await Process(Encode(800, 1600))).Thumbnail, 160, 320, "Portrait");
Dimensions((await Process(Encode(1, 800))).Thumbnail, 1, 320, "Extreme aspect ratio");
await Rejected([], "empty");
await Rejected("not a photo"u8.ToArray(), "fake jpg");
await Rejected(Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"), "unsupported format");
await Rejected(Encode(12001, 1), "dimension limit");
await Rejected(new byte[MenuImageProcessor.MaxUploadBytes + 1], "file limit");
var truncated = Encode(640, 480, SKEncodedImageFormat.Jpeg);
await Rejected(truncated[..(truncated.Length / 2)], "truncated jpg");
using (var cancelled = new CancellationTokenSource())
{
    cancelled.Cancel();
    try { await Process(Encode(16, 16), cancelled.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { checks++; }
}

// Add an EXIF orientation tag to a valid JPEG and check all eight transforms.
var jpeg = Encode(120, 80, SKEncodedImageFormat.Jpeg);
int[][] expectedCorners = [[0, 1, 2, 3], [1, 0, 3, 2], [3, 2, 1, 0], [2, 3, 0, 1],
    [0, 2, 1, 3], [2, 0, 3, 1], [3, 1, 2, 0], [1, 3, 0, 2]];
for (byte orientation = 1; orientation <= 8; orientation++)
{
    byte[] exif = [0xFF, 0xE1, 0, 34, 0x45, 0x78, 0x69, 0x66, 0, 0, 0x49, 0x49, 42, 0, 8, 0, 0, 0,
        1, 0, 0x12, 1, 3, 0, 1, 0, 0, 0, orientation, 0, 0, 0, 0, 0, 0, 0];
    var oriented = jpeg[..2].Concat(exif).Concat(jpeg[2..]).ToArray();
    var result = await Process(oriented);
    using var decoded = SKBitmap.Decode(result.Detail);
    Check(decoded.Width == (orientation >= 5 ? 80 : 120) && decoded.Height == (orientation >= 5 ? 120 : 80), "EXIF dimensions " + orientation);
    var palette = new[] { SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow };
    for (var corner = 0; corner < 4; corner++)
    {
        var color = decoded.GetPixel((corner % 2 * 2 + 1) * decoded.Width / 4, (corner / 2 * 2 + 1) * decoded.Height / 4);
        var target = palette[expectedCorners[orientation - 1][corner]];
        Check(Math.Abs(color.Red - target.Red) < 18 && Math.Abs(color.Green - target.Green) < 18 && Math.Abs(color.Blue - target.Blue) < 18, "EXIF pixels " + orientation);
    }
}

using (var transparent = new SKBitmap(800, 600))
{
    transparent.Erase(SKColors.Transparent);
    using var image = SKImage.FromBitmap(transparent);
    using var png = image.Encode(SKEncodedImageFormat.Png, 100);
    using var output = SKBitmap.Decode((await Process(png.ToArray())).Detail);
    Check(output.GetPixel(0, 0).Alpha == 0, "Transparency preserved");
}

var optimizedUrl = "/uploads/menu-items/branch/item" + MenuImageUrls.DetailSuffix;
Check(new MenuItemDto { ImageUrl = optimizedUrl }.ThumbnailUrl == "/uploads/menu-items/branch/item-thumbnail.webp", "Menu DTO thumbnail");
Check(new PublicMenuItemDto { ImageUrl = optimizedUrl }.ThumbnailUrl == "/uploads/menu-items/branch/item-thumbnail.webp", "Public DTO thumbnail");
foreach (var legacy in new string?[] { null, "/uploads/menu-items/branch/old.jpg", "https://example.com/photo.webp" })
    Check(MenuImageUrls.Thumbnail(legacy) == legacy, "Legacy URL fallback");

if (args.Length > 0)
{
    var original = await File.ReadAllBytesAsync(args[0]);
    var photo = await Process(original);
    using var source = SKBitmap.Decode(original);
    using var detail = SKBitmap.Decode(photo.Detail);
    if (source.Width == detail.Width && source.Height == detail.Height)
    {
        double squaredError = 0;
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var a = source.GetPixel(x, y);
                var b = detail.GetPixel(x, y);
                squaredError += Math.Pow(a.Red - b.Red, 2) + Math.Pow(a.Green - b.Green, 2) + Math.Pow(a.Blue - b.Blue, 2);
            }
        var psnr = 10 * Math.Log10(255d * 255 / (squaredError / (source.Width * source.Height * 3)));
        Check(psnr > 35, "Sample photo fidelity (PSNR > 35 dB)");
        Console.WriteLine($"Sample photo PSNR: {psnr:F2} dB");
    }
    Check(photo.Thumbnail.Length < original.Length, "Sample thumbnail lighter than original");
    Console.WriteLine($"Sample bytes: original={original.Length}, detail={photo.Detail.Length}, thumbnail={photo.Thumbnail.Length}");
    if (args.Length > 1)
    {
        Directory.CreateDirectory(args[1]);
        await File.WriteAllBytesAsync(Path.Combine(args[1], "detail.webp"), photo.Detail);
        await File.WriteAllBytesAsync(Path.Combine(args[1], "thumbnail.webp"), photo.Thumbnail);
    }
}

var branchId = Guid.NewGuid();
var itemId = Guid.NewGuid();
var root = Path.Combine(Path.GetTempPath(), "grimorio-menu-image-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var mediator = DispatchProxy.Create<IMediator, ImageMediator>();
    var fake = (ImageMediator)(object)mediator;
    fake.Item = new MenuItemDetailDto { Id = itemId, ImageUrl = optimizedUrl };
    string PathFor(string url) => Path.Combine(root, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    var oldDetail = PathFor(optimizedUrl);
    var oldThumbnail = PathFor(MenuImageUrls.Thumbnail(optimizedUrl)!);
    Directory.CreateDirectory(Path.GetDirectoryName(oldDetail)!);
    await File.WriteAllBytesAsync(oldDetail, large.Detail);
    await File.WriteAllBytesAsync(oldThumbnail, large.Thumbnail);
    var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AppConstants.Claims.BranchId, branchId.ToString())], "checks")) };
    var environment = new ImageEnvironment(root);
    var controller = new MenuController(mediator, environment, NullLogger<MenuController>.Instance) { ControllerContext = new ControllerContext { HttpContext = context } };
    IFormFile FileFor(byte[] data) => new FormFile(new MemoryStream(data), 0, data.Length, "image", "plate.png");
    fake.BeforeUpdate = url =>
    {
        Check(File.Exists(oldDetail) && File.Exists(oldThumbnail), "Keep old images until save");
        Check(File.Exists(PathFor(url!)) && File.Exists(PathFor(MenuImageUrls.Thumbnail(url)!)), "Both new variants exist before DB save");
    };
    Check(await controller.UploadItemImage(itemId, FileFor(Encode(800, 600)), default) is OkObjectResult, "Upload succeeds");
    Check(!File.Exists(oldDetail) && !File.Exists(oldThumbnail), "Old variants removed after success");
    var current = fake.Item.ImageUrl!;
    fake.BeforeUpdate = null;
    Check(await controller.UploadItemImage(itemId, FileFor("fake"u8.ToArray()), default) is BadRequestObjectResult, "Invalid upload returns 400");
    Check(File.Exists(PathFor(current)), "Invalid upload keeps existing photo");
    var beforeFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Order().ToArray();
    fake.FailUpdate = true;
    try { await controller.UploadItemImage(itemId, FileFor(Encode(800, 600)), default); throw new Exception("Expected save failure"); }
    catch (InvalidOperationException) { checks++; }
    Check(beforeFiles.SequenceEqual(Directory.GetFiles(root, "*", SearchOption.AllDirectories).Order()), "Failed update cleans both new variants and retains old files");
    fake.FailUpdate = false;
    var uploads = new UploadsController(environment) { ControllerContext = new ControllerContext { HttpContext = context } };
    Check(uploads.GetUpload(current["/uploads/".Length..]) is PhysicalFileResult file && file.ContentType == "image/webp", "Public WebP served");
    Check(context.Response.Headers.CacheControl == "public,max-age=31536000,immutable", "Versioned image caching");
    Check(await controller.DeleteItemImage(itemId, default) is OkObjectResult, "Delete image succeeds");
    Check(!Directory.GetFiles(root, "*", SearchOption.AllDirectories).Any(), "Delete removes both variants");
}
finally { Directory.Delete(root, true); }

Console.WriteLine($"PASS {checks} menu image checks: formats, limits, scaling, EXIF, alpha, DTO compatibility, storage failure cleanup, caching and deletion.");

public class ImageMediator : DispatchProxy
{
    public MenuItemDetailDto Item = new();
    public Action<string?>? BeforeUpdate;
    public bool FailUpdate;
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (args?[0] is GetMenuItemDetailQuery) return Task.FromResult(new MenuItemDetailDto { Id = Item.Id, ImageUrl = Item.ImageUrl });
        if (args?[0] is UpdateMenuItemImageCommand command)
        {
            BeforeUpdate?.Invoke(command.ImageUrl);
            if (FailUpdate) throw new InvalidOperationException("Simulated DB failure");
            Item.ImageUrl = command.ImageUrl;
            return Task.FromResult<MenuItemDto>(Item);
        }
        throw new NotSupportedException(method?.Name);
    }
}
public sealed class ImageEnvironment(string root) : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = root;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "Checks";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = root;
    public string EnvironmentName { get; set; } = "Testing";
}
