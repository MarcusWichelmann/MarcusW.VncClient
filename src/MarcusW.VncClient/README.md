# MarcusW.VncClient

High-performance, cross-platform VNC client library for .NET.

## 🌟 Features

- **High Performance**: Efficient encoding types like `Tight`, `ZRLE`, and `Raw` for smooth remote desktop experience
- **Cross-Platform**: Works on Windows, Linux, macOS - anywhere .NET runs
- **Server Compatibility**: Tested with TigerVNC, LibVNCServer, RealVNC, Vino-Server, and UltraVNC
- **Modular Architecture**: Clean, extensible design with dependency injection support
- **Multiple Security Types**: Support for VNC Auth, VeNCrypt, TLS, and more
- **Zero Dependencies**: Core library has no external dependencies outside .NET

## 🚀 Quick Start

```csharp
using MarcusW.VncClient;

// Create VNC client
var vncClient = new VncClient(loggerFactory);

// Configure connection parameters
var parameters = new ConnectParameters 
{
    TransportParameters = new TcpTransportParameters 
    {
        Host = "your-vnc-server.com",
        Port = 5900
    },
    AuthenticationHandler = new YourAuthenticationHandler()
};

// Connect
var connection = await vncClient.ConnectAsync(parameters, cancellationToken);
```

## 📦 Package Ecosystem

- **Community.MarcusW.VncClient** (this package) - Core protocol implementation
- **Community.MarcusW.VncClient.Avalonia** - Ready-to-use Avalonia UI controls
- **Community.MarcusW.VncClient.Blazor** - Blazor Server components (WebAssembly NOT supported)

## 🔧 Advanced Features

- Continuous framebuffer updates with flow control
- Dynamic session resizing (client-side and server-side)
- Clipboard sharing (server to client)
- Headless operation support
- Comprehensive logging for debugging
- Observable connection state (`INotifyPropertyChanged`)

## 📚 Documentation

This is a community-maintained fork of the original MarcusW.VncClient library. For documentation and usage examples, see our [GitHub repository](https://github.com/karbonbaron/MarcusW.VncClient).

## 🤝 Contributing

Contributions welcome! Please see our [GitHub repository](https://github.com/karbonbaron/MarcusW.VncClient) for guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/karbonbaron/MarcusW.VncClient/blob/master/LICENSE) file for details.
