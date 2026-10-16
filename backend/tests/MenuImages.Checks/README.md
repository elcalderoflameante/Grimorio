# Menu Image Checks

Run from the repository root:

```powershell
dotnet run --project backend/tests/MenuImages.Checks
```

The checks use generated images and a fake mediator. They do not connect to a database
or modify existing menu images. Temporary test files are removed after each run.

Optional: pass a real photo and an output directory to inspect the optimized variants
and measure size and PSNR (when the photo does not require resizing):

```powershell
dotnet run --project backend/tests/MenuImages.Checks -- path/to/photo.jpg .tmp/image-review
```

Uploads are limited to 10 MB, 40 megapixels and 12000 pixels per side. Valid static
JPEG, PNG and WebP images are normalized to sRGB and EXIF orientation, then encoded
as WebP at quality 90. Detail images fit within 1600 pixels; thumbnails fit within
320 pixels. Neither version is cropped or enlarged. Metadata is not copied.

`ImageUrl` continues to identify the detail image. `ThumbnailUrl` is derived from the
versioned filename, with a fallback to `ImageUrl` for legacy images. Existing uploads
are not rewritten automatically. No database migration is required.

SkiaSharp native assets for Windows and dependency-free Linux are shipped with the
API. Rebuild and deploy the backend as well as the frontend to enable processing.
