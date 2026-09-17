# 🎨 Convert SVG Icons to PNG

I've created professional SVG icon templates for your NuGet packages. Convert them to 128x128 PNG files.

## ✅ **Quick Online Conversion** (Easiest)

1. **Visit**: https://cloudconvert.com/svg-to-png
2. **Upload each SVG file**:
   - `vnc-client-core.svg` → `vnc-client-icon.png`
   - `vnc-client-avalonia.svg` → `vnc-client-avalonia-icon.png` 
   - `vnc-client-blazor.svg` → `vnc-client-blazor-icon.png`
3. **Set size**: 128x128 pixels
4. **Download** and place in correct folders:
   ```
   src/MarcusW.VncClient/vnc-client-icon.png
   src/MarcusW.VncClient.Avalonia/vnc-client-avalonia-icon.png
   src/MarcusW.VncClient.Blazor/vnc-client-blazor-icon.png
   ```

## 🛠️ **Using Command Line Tools**

### ImageMagick (if installed):
```bash
magick vnc-client-core.svg -resize 128x128 vnc-client-icon.png
magick vnc-client-avalonia.svg -resize 128x128 vnc-client-avalonia-icon.png  
magick vnc-client-blazor.svg -resize 128x128 vnc-client-blazor-icon.png
```

### Inkscape (if installed):
```bash
inkscape --export-type=png --export-width=128 --export-height=128 vnc-client-core.svg
inkscape --export-type=png --export-width=128 --export-height=128 vnc-client-avalonia.svg
inkscape --export-type=png --export-width=128 --export-height=128 vnc-client-blazor.svg
```

## 🎨 **Icon Descriptions**

- **Core Package**: Blue circle with monitor and connection dots (classic VNC theme)
- **Avalonia Package**: Blue-to-purple gradient with "A" symbol and UI elements
- **Blazor Server Package**: Blue-to-purple with browser window and "B" symbol

## 📁 **Final File Structure**
```
src/
├── MarcusW.VncClient/
│   └── vnc-client-icon.png (128x128)
├── MarcusW.VncClient.Avalonia/
│   └── vnc-client-avalonia-icon.png (128x128)
└── MarcusW.VncClient.Blazor/
    └── vnc-client-blazor-icon.png (128x128)
```

**After conversion, run the pre-publish test script to validate everything! 🚀**
