$ErrorActionPreference = 'Stop'
$appRoot = $PSScriptRoot
$apkPath = Join-Path $appRoot 'build/app/outputs/flutter-apk/app-release.apk'
if (-not (Test-Path -LiteralPath $apkPath)) { throw 'Primero compila la APK de release.' }
$versionLine = Get-Content -LiteralPath (Join-Path $appRoot 'pubspec.yaml') | Where-Object { $_ -match '^version:\s*' } | Select-Object -First 1
if ($versionLine -notmatch '^version:\s*(\d+\.\d+\.\d+)\+(\d+)\s*$') { throw 'La version debe tener formato x.y.z+numero.' }
$versionName = $Matches[1]
$versionCode = [int]$Matches[2]
$downloads = [IO.Path]::GetFullPath((Join-Path $appRoot '../../frontend/public/downloads'))
New-Item -ItemType Directory -Force -Path $downloads | Out-Null
$destination = Join-Path $downloads 'grimorio-inventario.apk'
Copy-Item -LiteralPath $apkPath -Destination $destination -Force
$hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
$manifest = [ordered]@{ versionCode = $versionCode; versionName = $versionName; required = $false; apkUrl = '/downloads/grimorio-inventario.apk'; sha256 = $hash; message = 'Mejoras de inventario y actualizaciones desde la app.' }
[IO.File]::WriteAllText((Join-Path $downloads 'inventory-version.json'), ($manifest | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
Write-Output "APK de inventario $versionName+$versionCode preparada en frontend/public/downloads. SHA-256: $hash"
