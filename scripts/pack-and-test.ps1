# PowerShell script to build, pack, and test NuGet packages locally

param(
    [switch]$Clean,
    [switch]$Pack,
    [switch]$Push,
    [string]$Source = "local-test",
    [string]$OutputDir = ".\packages",
    [string]$Configuration = "Release"
)

Write-Host "🚀 MarcusW.VncClient NuGet Package Builder" -ForegroundColor Cyan
Write-Host "=" * 50

# Clean previous builds
if ($Clean) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean -c $Configuration
    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }
}

# Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Green
dotnet restore

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Green
dotnet build -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "🧪 Running tests..." -ForegroundColor Green
dotnet test -c $Configuration --no-build --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit 1
}

# Create packages
if ($Pack) {
    Write-Host "📦 Creating NuGet packages..." -ForegroundColor Green
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    
    $projects = @(
        "src\MarcusW.VncClient\MarcusW.VncClient.csproj",
        "src\MarcusW.VncClient.Avalonia\MarcusW.VncClient.Avalonia.csproj",
        "src\MarcusW.VncClient.Blazor\MarcusW.VncClient.Blazor.csproj"
    )
    
    foreach ($project in $projects) {
        Write-Host "  📋 Packing $project..." -ForegroundColor Gray
        dotnet pack $project -c $Configuration --no-build -o $OutputDir
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to pack $project!" -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host "✅ All packages created successfully!" -ForegroundColor Green
    
    # List created packages
    Write-Host "📋 Created packages:" -ForegroundColor Cyan
    Get-ChildItem $OutputDir -Filter "*.nupkg" | ForEach-Object {
        Write-Host "  • $($_.Name)" -ForegroundColor Gray
    }
}

# Push to local source (for testing)
if ($Push) {
    Write-Host "🚀 Pushing packages to local source..." -ForegroundColor Green
    
    # Create local NuGet source if it doesn't exist
    $localSource = "C:\LocalNuGet"
    if (!(Test-Path $localSource)) {
        New-Item -ItemType Directory -Path $localSource -Force | Out-Null
        Write-Host "  📁 Created local NuGet source: $localSource" -ForegroundColor Gray
    }
    
    Get-ChildItem $OutputDir -Filter "*.nupkg" | ForEach-Object {
        Write-Host "  📤 Pushing $($_.Name)..." -ForegroundColor Gray
        dotnet nuget push $_.FullName -s $localSource --skip-duplicate
    }
    
    Write-Host "✅ Packages pushed to local source!" -ForegroundColor Green
    Write-Host "💡 To use locally: dotnet nuget add source $localSource" -ForegroundColor Cyan
}

Write-Host "🎉 Script completed successfully!" -ForegroundColor Green
