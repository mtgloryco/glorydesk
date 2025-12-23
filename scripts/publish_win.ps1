# Configuration
$ProjectName = "InventoryManagementSystem"
$Runtime = "win-x64"
$OutputDir = "Releases\Windows"
$ArchiveName = "InventoryManagementSystem_Windows.zip"

Write-Host "🚀 Starting Windows build for $ProjectName..." -ForegroundColor Cyan

# Create directory
if (!(Test-Path -Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir
}

# Publish
dotnet publish -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host "📦 Archiving..." -ForegroundColor Cyan
    
    if (Test-Path -Path "Releases\$ArchiveName") {
        Remove-Item -Path "Releases\$ArchiveName"
    }
    
    Compress-Archive -Path $OutputDir -DestinationPath "Releases\$ArchiveName"
    Write-Host "🎉 Done! Release at: Releases\$ArchiveName (Folder: Releases\Windows)" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
