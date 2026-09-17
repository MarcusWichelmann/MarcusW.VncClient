# 📦 NuGet Publishing Checklist

Complete checklist to publish the community-maintained MarcusW.VncClient packages.

## 🎯 Pre-Publishing Setup

### ☐ 1. Create Package Icons
**Current**: Placeholder text files exist  
**Action**: Create actual 128x128 PNG icons
- `src/MarcusW.VncClient/vnc-client-icon.png` 
- `src/MarcusW.VncClient.Avalonia/vnc-client-avalonia-icon.png`
- `src/MarcusW.VncClient.Blazor/vnc-client-blazor-icon.png`

**Design Guidelines**: 
- Modern, clean design with VNC/remote desktop theme
- Transparent background, professional appearance suitable for NuGet gallery

### ☐ 2. Local Testing
```bash
# Test compilation
dotnet build

# Test package creation
dotnet pack -o ./packages

# Run tests
dotnet test

# Test sample apps
dotnet build samples/AvaloniaVncClient
dotnet build samples/BlazorVncClient
```

### ☐ 3. GitHub Repository Setup
1. **Enable GitHub Actions**: Repository Settings → Actions → Allow all actions
2. **Add NuGet API Key**: 
   - Go to [nuget.org](https://www.nuget.org) → Account → API Keys
   - Create new API key with "Push new packages and package versions" permission
   - Add as repository secret: Settings → Secrets → Actions → New secret
   - Name: `NUGET_API_KEY`, Value: your API key

### ☐ 4. Version Review
Check `src/Directory.Build.props` - currently set to `2.0.0-alpha1`
- Pre-release: Keep alpha/beta suffix  
- Stable release: Remove suffix (e.g., `2.0.0`)

## 🚀 Publishing Process

### ☐ 5. Push for Pre-Release
```bash
git add .
git commit -m "Prepare community fork for NuGet publishing"
git push origin master
```
**Result**: Creates pre-release packages in GitHub Packages

### ☐ 6. Verify GitHub Packages
- Check: https://github.com/karbonbaron/MarcusW.VncClient/packages
- Ensure all 3 packages appear: Core, Avalonia, Blazor

### ☐ 7. Test Pre-Release Packages
```bash
# Add GitHub source
dotnet nuget add source https://nuget.pkg.github.com/karbonbaron/index.json -n github-karbonbaron

# Test install
dotnet add package MarcusW.VncClient --source github-karbonbaron --prerelease
```

### ☐ 8. Create Release Tag (for NuGet.org)
```bash
git tag v2.0.0-alpha1
git push origin v2.0.0-alpha1
```
**Result**: Triggers stable package publishing to NuGet.org

### ☐ 9. Verify NuGet.org Publishing
- Check packages appear on nuget.org:
  - https://www.nuget.org/packages/MarcusW.VncClient
  - https://www.nuget.org/packages/MarcusW.VncClient.Avalonia  
  - https://www.nuget.org/packages/MarcusW.VncClient.Blazor

## ✅ Post-Publishing Validation

### ☐ 10. End-to-End Test
```bash
# Create test project
dotnet new console -n VncTest
cd VncTest

# Install from nuget.org
dotnet add package MarcusW.VncClient
dotnet add package MarcusW.VncClient.Avalonia

# Verify it builds
dotnet build
```

### ☐ 11. Update Documentation
- Update NuGet badge URLs in README.md
- Verify all package links work correctly

### ☐ 12. Community Announcement
Consider notifying the community about the maintained fork:
- GitHub Discussions on original repository
- .NET community forums
- Twitter/social media

## 🚨 Troubleshooting

### Build Failures
- Check GitHub Actions logs
- Ensure all icon files exist and are valid PNG
- Verify project references are correct

### Publishing Failures  
- Verify NUGET_API_KEY secret is correctly set
- Check API key permissions on nuget.org
- Ensure package names don't conflict

### Package Not Found
- NuGet indexing can take 10-15 minutes
- Check package status on nuget.org
- Verify version numbers match expectations

## 📁 File Summary

**Created/Modified Files:**
- ✅ All `.csproj` files with NuGet metadata
- ✅ Package-specific README.md files  
- ✅ Updated GitHub Actions workflow
- ✅ Modern Directory.Build.props
- ⏳ Package icons (placeholders created, need real icons)

**Ready for Publishing**: Almost! Just need real icons and NuGet API key setup.
