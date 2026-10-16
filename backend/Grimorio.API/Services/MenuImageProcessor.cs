using SkiaSharp;

namespace Grimorio.API.Services;

public sealed record ProcessedMenuImage(byte[] Detail, byte[] Thumbnail, int Width, int Height);

public static class MenuImageProcessor
{
    public const int MaxUploadBytes = 10 * 1024 * 1024;
    private const long MaxPixels = 40_000_000;
    private static readonly SemaphoreSlim ProcessingSlots = new(2);

    public static async Task<ProcessedMenuImage> ProcessAsync(Stream input, CancellationToken ct)
    {
        await ProcessingSlots.WaitAsync(ct);
        try
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > MaxUploadBytes)
                    throw new ArgumentException("La imagen no puede superar 10 MB.");
                buffer.Write(chunk, 0, read);
            }

            using var data = SKData.CreateCopy(buffer.ToArray());
            using var codec = SKCodec.Create(data);
            if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
                throw new ArgumentException("El contenido no es una imagen JPG, PNG o WEBP valida.");

            var info = codec.Info;
            if (info.Width <= 0 || info.Height <= 0 || info.Width > 12000 || info.Height > 12000
                || (long)info.Width * info.Height > MaxPixels)
                throw new ArgumentException("La imagen supera el limite de 40 megapixeles o 12000 px por lado.");
            if (codec.FrameCount > 1)
                throw new ArgumentException("Usa una imagen estatica, no una animacion.");

            using var srgb = SKColorSpace.CreateSrgb();
            using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul, srgb));
            if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
                throw new ArgumentException("La imagen esta incompleta o danada. Exportala nuevamente e intenta otra vez.");

            ct.ThrowIfCancellationRequested();
            using var image = SKImage.FromBitmap(bitmap);
            var rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var width = rotated ? info.Height : info.Width;
            var height = rotated ? info.Width : info.Height;
            var detail = Encode(image, codec.EncodedOrigin, width, height, 1600);
            ct.ThrowIfCancellationRequested();
            var thumbnail = Encode(image, codec.EncodedOrigin, width, height, 320);
            return new(detail, thumbnail, width, height);
        }
        finally
        {
            ProcessingSlots.Release();
        }
    }

    private static byte[] Encode(SKImage image, SKEncodedOrigin origin, int width, int height, int maxSide)
    {
        var scale = Math.Min(1d, (double)maxSide / Math.Max(width, height));
        var targetWidth = Math.Max(1, (int)Math.Round(width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(height * scale));
        using var colorSpace = SKColorSpace.CreateSrgb();
        using var surface = SKSurface.Create(new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)targetWidth / width, (float)targetHeight / height);
        // Normalize all EXIF orientations before stripping metadata during encoding.
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(width, height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(width, height); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, height); canvas.RotateDegrees(270); break;
        }
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Webp, 90);
        return encoded.ToArray();
    }
}
