# Community Fork Maintenance Notes

This is a community-maintained fork of the original MarcusW.VncClient library, created because the original maintainer stopped publishing NuGet packages.

## Key Changes Made

### ✅ Fixed WebAssembly Issue
- **Problem**: Original claimed Blazor WebAssembly support, but VNC requires TCP connections which browsers can't provide
- **Solution**: Updated all documentation and code to clarify **Blazor Server ONLY** support
- **Technical Reason**: Web browsers (including WASM) cannot create raw TCP sockets due to security restrictions

### ✅ Updated Repository References  
- All GitHub links now point to `karbonbaron/MarcusW.VncClient` instead of original
- Package metadata updated to reflect community maintenance
- License attribution maintained for original author

### ✅ Package Structure
- **MarcusW.VncClient** - Core library (platform-agnostic)
- **MarcusW.VncClient.Avalonia** - Avalonia UI controls  
- **MarcusW.VncClient.Blazor** - Blazor Server components (NOT WebAssembly)

## Publishing Instructions

1. **Test Build**: Run `dotnet build` to verify all projects compile
2. **Create Icons**: Replace the placeholder icon files with actual 128x128 PNG icons
3. **Version**: Update version in `src/Directory.Build.props` as needed
4. **Secrets**: Ensure GitHub repo has `NUGET_API_KEY` secret configured
5. **Release**: Push to master for pre-release packages, create tags for stable releases

## Architecture Notes

### Why Blazor WebAssembly Cannot Work
```
VNC Protocol Flow (Impossible in Browser):
Browser → Direct TCP Socket → VNC Server ❌ (Security blocked)

Blazor Server Solution (Works):
Browser ← SignalR → Server ← TCP Socket → VNC Server ✅
```

### Package Dependencies
- Core has minimal dependencies (just logging abstractions)
- Avalonia adapter depends on Avalonia UI + ReactiveUI
- Blazor adapter depends on ASP.NET Core Components

## Original Credits
This library was created by Marcus Wichelmann. This fork exists solely to continue NuGet package availability and maintenance while respecting the original MIT license and attribution.
