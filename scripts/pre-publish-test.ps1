# Pre-Publishing Test Script
# Run this before pushing to verify everything is ready

param(
    [switch]$SkipBuild,
    [switch]$SkipPack,
    [switch]$SkipTest
)

Write-Host "🚀 Pre-Publishing Test for MarcusW.VncClient Community Fork" -ForegroundColor Cyan
Write-Host "=" * 60

$ErrorActionPreference = "Stop"
$success = $true

# Function to check if a file exists
function Test-FileExists {
    param($Path, $Description)
    if (Test-Path $Path) {
        Write-Host "  ✅ $Description" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  ❌ $Description - MISSING!" -ForegroundColor Red
        return $false
    }
}

# Check required files
Write-Host "`n📁 Checking Required Files..." -ForegroundColor Yellow
$filesOk = $true

# Check icons (currently placeholders)
$iconFiles = @(
    @{Path="src\MarcusW.VncClient\vnc-client-icon.png"; Desc="Core package icon"},
    @{Path="src\MarcusW.VncClient.Avalonia\vnc-client-avalonia-icon.png"; Desc="Avalonia package icon"},
    @{Path="src\MarcusW.VncClient.Blazor\vnc-client-blazor-icon.png"; Desc="Blazor package icon"}
)

foreach ($icon in $iconFiles) {
    if (!(Test-FileExists $icon.Path $icon.Desc)) {
        $filesOk = $false
    } elseif ((Get-Item $icon.Path).Length -lt 1000) {
        Write-Host "  ⚠️  $($icon.Desc) appears to be placeholder text (not PNG)" -ForegroundColor Yellow
        $filesOk = $false
    }
}

# Check README files
$readmeFiles = @(
    "src\MarcusW.VncClient\README.md",
    "src\MarcusW.VncClient.Avalonia\README.md", 
    "src\MarcusW.VncClient.Blazor\README.md"
)

foreach ($readme in $readmeFiles) {
    if (!(Test-FileExists $readme "README: $readme")) {
        $filesOk = $false
    }
}

if (!$filesOk) {
    Write-Host "`n❌ File check failed! Fix missing files before publishing." -ForegroundColor Red
    $success = $false
}

# Restore dependencies
if (!$SkipBuild) {
    Write-Host "`n📦 Restoring Dependencies..." -ForegroundColor Yellow
    try {
        dotnet restore
        Write-Host "  ✅ Dependencies restored" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ Restore failed: $_" -ForegroundColor Red
        $success = $false
    }

    # Build solution
    Write-Host "`n🔨 Building Solution..." -ForegroundColor Yellow
    try {
        dotnet build -c Release --no-restore
        Write-Host "  ✅ Build successful" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ Build failed: $_" -ForegroundColor Red
        $success = $false
    }
}

# Run tests
if (!$SkipTest) {
    Write-Host "`n🧪 Running Tests..." -ForegroundColor Yellow
    try {
        dotnet test -c Release --no-build --verbosity quiet
        Write-Host "  ✅ All tests passed" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ Tests failed: $_" -ForegroundColor Red
        $success = $false
    }
}

# Test sample applications
Write-Host "`n🎯 Testing Sample Applications..." -ForegroundColor Yellow
$samples = @("samples\AvaloniaVncClient", "samples\BlazorVncClient")

foreach ($sample in $samples) {
    if (Test-Path $sample) {
        try {
            dotnet build $sample -c Release --no-restore
            Write-Host "  ✅ $sample builds successfully" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ $sample build failed: $_" -ForegroundColor Red
            $success = $false
        }
    } else {
        Write-Host "  ⚠️  $sample not found (skipping)" -ForegroundColor Yellow
    }
}

# Create packages
if (!$SkipPack) {
    Write-Host "`n📦 Creating Test Packages..." -ForegroundColor Yellow
    $packageDir = ".\test-packages"
    
    if (Test-Path $packageDir) {
        Remove-Item $packageDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    
    $projects = @(
        "src\MarcusW.VncClient\MarcusW.VncClient.csproj",
        "src\MarcusW.VncClient.Avalonia\MarcusW.VncClient.Avalonia.csproj",
        "src\MarcusW.VncClient.Blazor\MarcusW.VncClient.Blazor.csproj"
    )
    
    foreach ($project in $projects) {
        try {
            dotnet pack $project -c Release --no-build -o $packageDir
            $projectName = (Split-Path $project -Leaf) -replace '\.csproj$', ''
            Write-Host "  ✅ Packed $projectName" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ Pack failed for $project: $_" -ForegroundColor Red
            $success = $false
        }
    }
    
    if ($success) {
        Write-Host "`n📋 Created Packages:" -ForegroundColor Cyan
        Get-ChildItem $packageDir -Filter "*.nupkg" | ForEach-Object {
            Write-Host "  • $($_.Name)" -ForegroundColor Gray
        }
    }
}

# Final result
Write-Host "`n" + "=" * 60
if ($success) {
    Write-Host "🎉 PRE-PUBLISHING TEST SUCCESSFUL!" -ForegroundColor Green
    Write-Host "✅ Ready to commit and push for GitHub Packages" -ForegroundColor Green
    Write-Host "✅ Ready to create release tag for NuGet.org publishing" -ForegroundColor Green
    
    Write-Host "`n📝 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Ensure GitHub repository has NUGET_API_KEY secret configured" -ForegroundColor White
    Write-Host "2. git add . && git commit -m 'Ready for publishing'" -ForegroundColor White  
    Write-Host "3. git push origin master  # Creates pre-release packages" -ForegroundColor White
    Write-Host "4. git tag v2.0.0-alpha1 && git push origin v2.0.0-alpha1  # Publishes to NuGet.org" -ForegroundColor White
    
} else {
    Write-Host "❌ PRE-PUBLISHING TEST FAILED!" -ForegroundColor Red
    Write-Host "⚠️  Fix the issues above before publishing" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n💡 Run with -SkipBuild, -SkipPack, or -SkipTest to skip specific steps" -ForegroundColor Gray
