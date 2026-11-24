param([string]$SourceImage)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path $PSScriptRoot -Parent
$masterPath = Join-Path $projectRoot 'assets\images\waitstaff_app_icon.png'
if (-not $SourceImage) { $SourceImage = $masterPath }

$loaded = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $SourceImage).Path)
try {
    $source = [System.Drawing.Bitmap]::new($loaded)
} finally {
    $loaded.Dispose()
}

function Export-Icon {
    param([string]$Path, [int]$Size)
    New-Item -ItemType Directory -Path (Split-Path $Path -Parent) -Force | Out-Null
    $bitmap = [System.Drawing.Bitmap]::new(
        $Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $attributes = [System.Drawing.Imaging.ImageAttributes]::new()
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $ratio = [Math]::Min($Size / $source.Width, $Size / $source.Height)
        $width = [int][Math]::Round($source.Width * $ratio)
        $height = [int][Math]::Round($source.Height * $ratio)
        $rectangle = [System.Drawing.Rectangle]::new(
            [int](($Size - $width) / 2), [int](($Size - $height) / 2), $width, $height)
        $graphics.DrawImage($source, $rectangle, 0, 0, $source.Width, $source.Height,
            [System.Drawing.GraphicsUnit]::Pixel, $attributes)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Output "$Size x $Size : $Path"
    } finally {
        $attributes.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

try {
    Export-Icon -Path $masterPath -Size 1024
    $resources = Join-Path $projectRoot 'android\app\src\main\res'
    foreach ($density in @(
        @{name = 'mdpi'; size = 48},
        @{name = 'hdpi'; size = 72},
        @{name = 'xhdpi'; size = 96},
        @{name = 'xxhdpi'; size = 144},
        @{name = 'xxxhdpi'; size = 192}
    )) {
        Export-Icon -Path (Join-Path $resources "mipmap-$($density.name)\ic_launcher.png") -Size $density.size
    }
} finally {
    $source.Dispose()
}
