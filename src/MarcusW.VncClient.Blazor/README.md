# MarcusW.VncClient.Blazor

Blazor Server adapter for the MarcusW.VncClient library. Enables VNC client functionality in Blazor Server applications with real-time SignalR communication.

⚠️ **Important**: This package supports **Blazor Server ONLY**. WebAssembly is NOT supported because VNC requires direct TCP socket connections, which browsers cannot provide due to security restrictions.

## 🌟 Features

- **Blazor Server**: Real-time VNC rendering via SignalR
- **JavaScript Interop**: Efficient canvas-based rendering
- **Component-Based**: Clean Razor component architecture
- **Touch Support**: Mobile and tablet friendly interactions
- **Responsive Design**: Adapts to different screen sizes
- **Circuit Safe**: Handles Blazor circuit disconnections gracefully

## 🚀 Quick Start

### 1. Install Package

```xml
<PackageReference Include="Community.MarcusW.VncClient.Blazor" Version="2.0.0" />
```

### 2. Add to Razor Component

```html
@page "/vnc"
@using MarcusW.VncClient.Blazor

<VncViewer Connection="@vncConnection" 
           Width="800" 
           Height="600" 
           OnConnectionStateChanged="HandleConnectionState" />

@code {
    private RfbConnection? vncConnection;
    
    protected override async Task OnInitializedAsync()
    {
        await ConnectToVncServer();
    }
    
    private async Task ConnectToVncServer()
    {
        var vncClient = new VncClient(loggerFactory);
        var parameters = new ConnectParameters
        {
            TransportParameters = new TcpTransportParameters
            {
                Host = "your-vnc-server.com",
                Port = 5900
            },
            AuthenticationHandler = new YourAuthHandler()
        };
        
        vncConnection = await vncClient.ConnectAsync(parameters);
    }
    
    private void HandleConnectionState(ConnectionState state)
    {
        // Handle state changes
        InvokeAsync(StateHasChanged);
    }
}
```

### 3. Configure Services

```csharp
// Program.cs (Blazor Server only)
builder.Services.AddVncClientServices();
```

## 🎮 Component Features

- **Canvas Rendering**: High-performance HTML5 canvas display
- **Input Handling**: Mouse, keyboard, and touch event processing
- **Scaling Options**: Multiple scaling modes for different screen sizes
- **Connection Management**: Built-in connection state visualization
- **Error Boundaries**: Graceful error handling and recovery
- **Clipboard Integration**: Copy/paste between browser and remote session
- **Special Key Capture**: System-level shortcuts forwarded to remote session

### ⌨️ Keyboard Input & Special Keys

The component provides comprehensive keyboard input support with a **Special Keys Menu** to overcome browser limitations.

**✅ Regular Keyboard Input:**
- All regular typing (letters, numbers, symbols)
- Function keys (`F1`-`F12`)
- Navigation keys (arrows, Home, End, Page Up/Down)
- Clipboard paste (`Ctrl+V` / `Cmd+V`)
- Most `Ctrl+` key combinations
- `Ctrl+Alt+T` - Terminal (Linux)
- `Ctrl+Alt+Arrow keys` - Virtual desktops (Linux)
- `F11` - Fullscreen toggle (sent to remote instead of browser)

**⌨️ Special Keys Menu:**

For OS-protected shortcuts that cannot be captured via keyboard, use the **"⌨️"** floating button:

**Features:**
- 🎯 **Draggable** - Click and drag the button to reposition it anywhere on the screen
- 📌 **Stays where you put it** - Position is remembered during the session
- 🔄 **Collapsible** - Small icon button expands to show menu when clicked
- ✨ **Auto-closes** - Menu automatically collapses after sending a key combo

**Windows Shortcuts:**
- `Ctrl+Alt+Delete` - Security screen
- `Ctrl+Shift+Esc` - Task Manager
- `Alt+F4` - Close window
- `Alt+Tab` - Switch window
- `Win+R` - Run dialog
- `Win+D` - Show desktop
- `Win+E` - File Explorer
- `Win+L` - Lock screen

**Linux Shortcuts:**
- `Ctrl+Alt+T` - Terminal
- `Ctrl+Alt+F1-F7` - TTY switching

These menu items programmatically send the key combinations directly to the VNC server, bypassing browser keyboard event limitations.

**💡 How It Works:**

- **Typing on keyboard**: Captured and sent in real-time (except OS-protected shortcuts)
- **Special Keys menu**: Click to send OS-protected shortcuts that can't be typed
- The menu appears when connected and automatically closes after sending a key combo

**🎯 Usage Tips:**

1. **Click the canvas** to ensure it has focus (blue border when focused)
2. **Type normally** for most operations
3. **Use the Special Keys menu** for system-level shortcuts (Ctrl+Alt+Del, Win+R, etc.)
4. Use **fullscreen mode** for optimal keyboard capture of regular keys

## 🔧 Advanced Configuration

### Component Parameters

```html
<VncViewer Connection="@connection"
           Width="1024"
           Height="768"
           ScaleMode="FitToContainer"
           EnablePointerCapture="true"
           ShowConnectionState="true"
           OnFrameReceived="HandleFrame"
           OnError="HandleError" />
```

### JavaScript Optimization

```javascript
// Custom rendering optimizations
window.vncClient = {
    optimizeCanvas: function(canvasId) {
        const canvas = document.getElementById(canvasId);
        const ctx = canvas.getContext('2d');
        ctx.imageSmoothingEnabled = false; // Pixel-perfect rendering
    }
};
```

## 🌐 Browser Compatibility

- **Chrome/Edge**: Full support with optimal performance
- **Firefox**: Full support with good performance  
- **Safari**: Full support (some limitations on older versions)
- **Mobile Browsers**: Touch input support with responsive design

## ⚠️ Why Blazor Server Only?

**Technical Limitation**: VNC protocol requires direct TCP socket connections to the VNC server. Web browsers (including WebAssembly) cannot create raw TCP sockets due to security restrictions - they can only make HTTP/WebSocket connections.

**Blazor Server Solution**: 
- VNC connection runs on the server (where TCP sockets are allowed)
- Real-time updates sent to browser via SignalR
- Client receives rendered frames and sends input back to server
- No CORS issues since server handles all VNC communication

## 🔒 Security Considerations

Since VNC connections are handled server-side, ensure proper authentication and authorization for your Blazor Server application.

## 📚 Dependencies

This package depends on:
- [Community.MarcusW.VncClient](https://www.nuget.org/packages/Community.MarcusW.VncClient) - Core VNC client
- [Microsoft.AspNetCore.Components.Web](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Web) - Blazor web components

## 📖 Complete Example

See our [Blazor sample application](https://github.com/karbonbaron/MarcusW.VncClient/tree/master/samples/BlazorVncClient) for a complete implementation example.

## 🤝 Contributing

Contributions welcome! Please see our [GitHub repository](https://github.com/karbonbaron/MarcusW.VncClient) for guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/karbonbaron/MarcusW.VncClient/blob/master/LICENSE) file for details.