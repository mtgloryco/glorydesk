# Configuration
$ProjectName = "InventoryManagementSystem"
$Runtime = "win-x64"
$OutputDir = "Releases\Windows"
$ArchiveName = "InventoryManagementSystem_Windows.zip"
$RedistDir = "InventoryManagementSystem.Shared\redist"
$VcRedistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe"

Write-Host "🚀 Starting Windows build for $ProjectName..." -ForegroundColor Cyan

# Create directory
if (!(Test-Path -Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir
}

# Publish (target the Desktop project explicitly - publishing the whole .sln would try to
# single-file-publish the Shared/Tests library projects too, which fails (NETSDK1099/1098)).
dotnet publish "InventoryManagementSystem.Desktop\InventoryManagementSystem.Desktop.csproj" -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutputDir

# Fetch the VC++ Redistributable so the installer can bundle it (needed by Avalonia/SkiaSharp's
# native rendering libraries - without it, the app fails to start on a clean Windows machine with
# "The application has failed to start because its side-by-side configuration is incorrect").
if (!(Test-Path -Path $RedistDir)) {
    New-Item -ItemType Directory -Path $RedistDir | Out-Null
}
$RedistPath = Join-Path $RedistDir "vc_redist.x64.exe"
if (!(Test-Path -Path $RedistPath)) {
    Write-Host "⬇️  Downloading Visual C++ Redistributable (x64)..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $VcRedistUrl -OutFile $RedistPath
    } catch {
        Write-Host "⚠️  Failed to download vc_redist.x64.exe: $_" -ForegroundColor Yellow
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host "📦 Archiving..." -ForegroundColor Cyan
    
    if (Test-Path -Path "Releases\$ArchiveName") {
        Remove-Item -Path "Releases\$ArchiveName"
    }
    
    Compress-Archive -Path $OutputDir -DestinationPath "Releases\$ArchiveName"
    Write-Host "🎉 Done! Release at: Releases\$ArchiveName (Folder: Releases\Windows)" -ForegroundColor Green

    # Inno Setup Compilation (if installed)
    $IsccPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    $IssScript = "InventoryManagementSystem.Shared\IMS_Setup_Script.iss"
    if ((Test-Path -Path $IsccPath) -and (Test-Path -Path $IssScript)) {
        Write-Host "📦 Compiling Installer via Inno Setup..." -ForegroundColor Cyan
        & $IsccPath $IssScript
        if ($LASTEXITCODE -eq 0) {
            Write-Host "🎉 Installer compilation successful! See Releases\IMS_Setup_v1.2.2_Windows.exe" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Inno Setup compilation failed." -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  Inno Setup 6 not found. Skipping Installer compilation." -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
