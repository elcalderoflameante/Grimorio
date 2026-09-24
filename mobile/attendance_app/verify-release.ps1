param([string]$Apk = "$PSScriptRoot/build/app/outputs/flutter-apk/app-release.apk")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Apk))
try {
    $checked = 0
    foreach ($entry in $archive.Entries) {
        if ($entry.FullName -notmatch '^lib/[^/]+/libapp\.so$') { continue }
        $stream = $entry.Open()
        $buffer = New-Object System.IO.MemoryStream
        try {
            $stream.CopyTo($buffer)
            $text = [System.Text.Encoding]::UTF8.GetString($buffer.ToArray())
            if (!$text.Contains('https://erp.elcalderoflameante.com/api') -or $text.Contains('http://10.0.2.2:5186/api')) {
                throw "URL de producción incorrecta en $($entry.FullName). No publicar este APK."
            }
            $checked++
        } finally { $stream.Dispose(); $buffer.Dispose() }
    }
    if ($checked -eq 0) { throw 'APK sin binarios Flutter release.' }
    Write-Output "URL de producción verificada en $checked arquitecturas."
} finally { $archive.Dispose() }
Get-FileHash -Algorithm SHA256 -LiteralPath $Apk
