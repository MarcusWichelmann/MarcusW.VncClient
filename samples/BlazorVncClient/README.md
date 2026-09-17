# Blazor VNC Client Sample

This is a sample Blazor application that demonstrates how to use the MarcusW.VncClient library in a web environment using Blazor Server.

## Features

- **Multi-Protocol VNC Support**: Connect to VNC servers using RFB protocol with support for multiple security types
- **Smart Authentication System**: 
  - **VNC Authentication** (Security Type 2): Password-only authentication for traditional VNC servers
  - **VeNCrypt Authentication** (Security Type 19): Username + password authentication for modern secure VNC servers
  - **Automatic Protocol Detection**: Correctly prompts for appropriate credentials based on server security type
  - **Pre-filled Credentials**: Optional username/password fields to avoid prompts
  - **Modal Authentication Dialogs**: Professional Bootstrap-based dialogs instead of browser prompts
- **Real-time VNC Display**: HTML5 Canvas-based rendering with threading-safe updates
- **Full Input Support**: Mouse and keyboard input forwarding with comprehensive key mapping
- **Connection Management**: Status monitoring, error handling, and graceful disconnection
- **Modern Bootstrap UI**: Responsive design with real-time connection information display

## How to Run

1. Navigate to the BlazorVncClient directory:
   ```bash
   cd samples/BlazorVncClient
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to the URL shown in the console (typically `https://localhost:5001` or `http://localhost:5000`)

4. Navigate to the "Vnc" page using the navigation menu

## How to Use

### Manual Connection

1. **Connection Setup:**
   - Enter the VNC server host (IP address or hostname)
   - Enter the VNC server port (default is 5900)
   - Optionally enter a password if you know it in advance

2. **Connect:**
   - Click the "Connect" button

### URL-Based Connection

You can also connect automatically using URL parameters:

**Query String Parameters:**
```
/vnc_view?host=192.168.1.100&port=5900&user=admin&password=secret&autoconnect=true
```

**Route Parameters:**
```
/vnc_view/192.168.1.100/5900
```

**Available Parameters:**
- `host` - VNC server hostname or IP address
- `port` - VNC server port number (default: 5900)
- `user` - Username for VeNCrypt authentication (optional)
- `password` - Password for VNC/VeNCrypt authentication (optional)
- `autoconnect` - Automatically connect when page loads (true/false)

**Examples:**
```
# Connect with full authentication (VeNCrypt)
https://localhost:5001/vnc_view?host=10.0.0.100&port=5901&user=admin&password=secret&autoconnect=true

# Connect with password only (VNC Authentication)
https://localhost:5001/vnc_view?host=server.local&port=5900&password=mypassword&autoconnect=true

# Pre-fill connection form without auto-connecting
https://localhost:5001/vnc_view?host=server.local&port=5900

# Using route parameters (authentication will prompt)
https://localhost:5001/vnc_view/192.168.1.50/5900

# Complete automation with credentials
https://localhost:5001/vnc_view?host=192.168.1.100&port=5900&user=vnc&password=password123&autoconnect=true
```

### Authentication Process

3. **Authentication:**
   - **Automatic Authentication**: The app automatically detects the server's security type and handles accordingly:
     - **With URL Parameters**: Uses provided `user` and `password` parameters automatically (no dialogs)
     - **Without Parameters**: Shows appropriate authentication dialog:
       - **VNC Auth**: Shows password-only dialog
       - **VeNCrypt**: Shows username + password dialog
   - **Authentication Dialog Features**:
     - Security type identification in dialog
     - Enter key to confirm, Escape to cancel
     - Professional modal design

4. **Interact:**
   - Once connected, you can interact with the remote desktop using mouse and keyboard
   - **Click the canvas** to focus it (blue border shows when focused)
   
   **Keyboard Input:**
   - Type normally - all regular keys work (letters, numbers, symbols, function keys)
   - `Ctrl+V` / `Cmd+V` - Paste from clipboard
   - `F11` - Fullscreen toggle sent to remote
   - Most Ctrl+ combinations work
   
   **⌨️ Special Keys Menu:**
   - Look for the **⌨️** floating button (starts in top-right corner)
   - **Drag it anywhere** - click and drag to reposition if it's in the way
   - **Click to expand** - shows menu with all available special key combinations
   - Send `Ctrl+Alt+Delete`, `Alt+F4`, `Alt+Tab`, `Win+R`, and more
   - Menu **auto-closes** after sending a key combo
   - Includes Windows shortcuts (Win+R, Win+D, etc.) and Linux shortcuts (Ctrl+Alt+F1-F7)
   
   **Why the Special Keys Menu?**
   - Your local OS intercepts shortcuts like `Ctrl+Alt+Delete` and `Win+R` before the browser sees them
   - The menu programmatically sends these key combinations directly to the remote VNC server
   - Similar to RDP Manager and other professional remote desktop tools
   - Draggable design prevents blocking important screen areas
   
   **Clipboard Integration**: 
   - Use `Ctrl+V` to paste into the remote session
   - Copy operations from remote are automatically placed in your clipboard
   
   **Fullscreen Mode**: 
   - Click fullscreen button for better keyboard capture
   - Browser allows more keyboard access in fullscreen mode
   
   **Connection Information Panel:**
   - Connection status and protocol version
   - Security type used for authentication
   - Desktop name and framebuffer size
   - Active encoding types
   - Error messages if connection fails

## Architecture

The application is built using:

- **Blazor Server** for the web UI framework
- **MarcusW.VncClient.Blazor** for VNC client functionality 
- **JavaScript Interop** for canvas rendering and browser integration
- **Dependency Injection** for service management
- **ReactiveUI** for view model and command patterns

## Key Components

- `Vnc.razor` - Main VNC client page with connection UI
- `VncView.razor` - VNC display component with canvas rendering
- `ConnectionManager` - Handles VNC connection lifecycle
- `InteractiveAuthenticationHandler` - Manages VNC authentication
- `JsInterop.cs` / `JsInterop.js` - Browser integration for canvas operations

## Comparison with Avalonia Sample

This Blazor implementation provides similar functionality to the Avalonia sample but adapted for web browsers:

- **Rendering:** Uses HTML5 Canvas instead of native UI controls
- **Input:** Browser event handling instead of native input events  
- **Authentication:** Browser-based prompts instead of modal dialogs
- **Deployment:** Web-based instead of desktop application

## Limitations

- Canvas rendering is basic and may not support all advanced VNC features
- Performance may be lower than native desktop applications
- Requires continuous connection to the Blazor server
- Browser security restrictions may limit some functionality

## Development Notes

The implementation demonstrates how to:
- Integrate the VNC client library with Blazor components
- Handle asynchronous VNC operations in a web context
- Bridge between .NET and JavaScript for canvas operations
- Manage connection state and user interaction in a web UI
