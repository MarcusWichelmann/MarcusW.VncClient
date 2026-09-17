# MarcusW.VncClient.Avalonia

Avalonia UI adapter for the MarcusW.VncClient library. Provides ready-to-use VNC viewer controls for Avalonia applications.

## 🌟 Features

- **Drop-in Control**: Ready-to-use `VncView` control for immediate integration
- **Full Interaction**: Mouse, keyboard, and scroll wheel support out of the box
- **Reactive UI**: Built with ReactiveUI for responsive, observable interfaces
- **High Performance**: Optimized rendering with dirty rectangle updates
- **Cross-Platform**: Works on Windows, Linux, macOS with Avalonia
- **MVVM Ready**: Perfect integration with view models and data binding

## 🚀 Quick Start

### 1. Install Package

```xml
<PackageReference Include="Community.MarcusW.VncClient.Avalonia" Version="2.0.0" />
```

### 2. Add Control to XAML

```xml
<Window xmlns:vnc="https://github.com/MarcusWichelmann/MarcusW.VncClient.Avalonia">
    <vnc:VncView Connection="{Binding VncConnection}" />
</Window>
```

### 3. Configure in View Model

```csharp
public class MainViewModel : ReactiveObject
{
    private RfbConnection? _vncConnection;
    
    public RfbConnection? VncConnection
    {
        get => _vncConnection;
        set => this.RaiseAndSetIfChanged(ref _vncConnection, value);
    }
    
    public async Task ConnectAsync()
    {
        var vncClient = new VncClient(loggerFactory);
        var parameters = new ConnectParameters 
        {
            TransportParameters = new TcpTransportParameters 
            {
                Host = "your-server.com",
                Port = 5900
            },
            AuthenticationHandler = new YourAuthHandler()
        };
        
        VncConnection = await vncClient.ConnectAsync(parameters);
    }
}
```

## 🎮 Control Features

- **Auto-scaling**: Automatic scaling to fit available space
- **Input Handling**: Complete mouse and keyboard event processing  
- **Clipboard Integration**: Seamless clipboard sharing (server to client)
- **Connection State**: Visual indicators for connection status
- **Error Handling**: Graceful error display and recovery

## 🔧 Advanced Usage

### Custom Render Settings

```csharp
vncView.EnableDirtyRectangleVisualization = true; // Debug rectangles
vncView.ScaleMode = VncViewScaleMode.FitToWindow;  // Scaling behavior
```

### Event Handling

```csharp
vncView.ConnectionStateChanged += (sender, state) => 
{
    // Handle connection state changes
};

vncView.PointerPositionChanged += (sender, position) => 
{
    // Track remote pointer position
};
```

## 📚 Dependencies

This package depends on:
- [Community.MarcusW.VncClient](https://www.nuget.org/packages/Community.MarcusW.VncClient) - Core VNC client
- [Avalonia](https://www.nuget.org/packages/Avalonia) - Cross-platform UI framework
- [System.Reactive](https://www.nuget.org/packages/System.Reactive) - Reactive extensions

## 📖 Complete Example

See our [Avalonia sample application](https://github.com/karbonbaron/MarcusW.VncClient/tree/master/samples/AvaloniaVncClient) for a complete implementation example.

## 🤝 Contributing

Contributions welcome! Please see our [GitHub repository](https://github.com/karbonbaron/MarcusW.VncClient) for guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/karbonbaron/MarcusW.VncClient/blob/master/LICENSE) file for details.
