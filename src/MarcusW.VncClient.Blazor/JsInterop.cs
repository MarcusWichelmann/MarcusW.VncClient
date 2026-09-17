using Microsoft.JSInterop;

namespace MarcusW.VncClient.Blazor
{
    public class CanvasDimensions
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double CanvasWidth { get; set; }
        public double CanvasHeight { get; set; }
    }

    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class JsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
        private readonly IJSRuntime _jsRuntime;

        public JsInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Community.MarcusW.VncClient.Blazor/JsInterop.js").AsTask());
        }

        public async ValueTask DrawRectangle(string canvasId, byte[] imageData, int x, int y, int width, int height)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("drawRectangle", canvasId, imageData, x, y, width, height);
        }

        public async ValueTask<(int, int)> GetDimensions()
        {
            var module = await moduleTask.Value;
            var result = await module.InvokeAsync<int[]>("getDimensions");
            return (result[0], result[1]);
        }

            public async ValueTask<CanvasDimensions> GetCanvasDimensions(string canvasId)
    {
        var module = await moduleTask.Value;
        var result = await module.InvokeAsync<CanvasDimensions>("getCanvasDimensions", canvasId);
        return result;
    }

    // Fullscreen API methods
    public async ValueTask<bool> EnterFullscreen(string elementId)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("enterFullscreen", elementId);
    }

    public async ValueTask<bool> ExitFullscreen()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("exitFullscreen");
    }

    public async ValueTask<bool> IsFullscreen()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<bool>("isFullscreen");
    }

    public async ValueTask AddFullscreenChangeListener(DotNetObjectReference<object> dotNetObjectRef, string methodName)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addFullscreenChangeListener", dotNetObjectRef, methodName);
    }

    public async ValueTask RemoveFullscreenChangeListener()
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("removeFullscreenChangeListener");
    }

        public async ValueTask SetCanvasSize(string canvasId, int width, int height)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("setCanvasSize", canvasId, width, height);
        }

        public async ValueTask ClearCanvas(string canvasId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("clearCanvas", canvasId);
        }

        public async ValueTask<string> Prompt(string message)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("showPrompt", message);
        }

        // Clipboard API methods - using global window helper
        public async ValueTask<bool> CopyToClipboard(string text)
        {
            return await _jsRuntime.InvokeAsync<bool>("vncClipboardHelper.copyToClipboard", text);
        }

        public async ValueTask<bool> SetupClipboardMonitoring(string canvasId, DotNetObjectReference<object> dotNetObjectRef)
        {
            return await _jsRuntime.InvokeAsync<bool>("vncClipboardHelper.setupClipboardMonitoring", canvasId, dotNetObjectRef);
        }

        public async ValueTask CleanupClipboardMonitoring(string canvasId)
        {
            await _jsRuntime.InvokeVoidAsync("vncClipboardHelper.cleanupClipboardMonitoring", canvasId);
        }

        // Special Keys Menu drag functionality
        public async ValueTask SetupMenuDrag(string menuId, DotNetObjectReference<object> dotNetObjectRef)
        {
            await _jsRuntime.InvokeVoidAsync("vncMenuDrag.setup", menuId, dotNetObjectRef);
        }

        public async ValueTask CleanupMenuDrag(string menuId)
        {
            await _jsRuntime.InvokeVoidAsync("vncMenuDrag.cleanup", menuId);
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
