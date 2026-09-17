using Microsoft.JSInterop;
using MarcusW.VncClient.Blazor;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Implementation of fullscreen management service
    /// </summary>
    public class FullscreenService : IFullscreenService
    {
        private readonly JsInterop _jsInterop;
        private bool _isFullscreen = false;
        private bool _disposed = false;

        public bool IsFullscreen => _isFullscreen;
        public event EventHandler<bool>? FullscreenChanged;

        public FullscreenService(JsInterop jsInterop)
        {
            _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
        }

        public async Task<bool> EnterFullscreenAsync(string elementId)
        {
            if (_disposed) return false;
            
            try
            {
                return await _jsInterop.EnterFullscreen(elementId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error entering fullscreen: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExitFullscreenAsync()
        {
            if (_disposed) return false;
            
            try
            {
                return await _jsInterop.ExitFullscreen();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exiting fullscreen: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleFullscreenAsync(string elementId)
        {
            if (_isFullscreen)
            {
                return await ExitFullscreenAsync();
            }
            else
            {
                return await EnterFullscreenAsync(elementId);
            }
        }

        public async Task InitializeAsync(DotNetObjectReference<IFullscreenService> dotNetObjectRef)
        {
            if (_disposed) return;
            
            try
            {
                await _jsInterop.AddFullscreenChangeListener(DotNetObjectReference.Create((object)this), nameof(OnFullscreenChanged));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing fullscreen listener: {ex.Message}");
            }
        }

        public async Task CleanupAsync()
        {
            if (_disposed) return;
            
            try
            {
                await _jsInterop.RemoveFullscreenChangeListener();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up fullscreen listener: {ex.Message}");
            }
        }

        [JSInvokable]
        public Task OnFullscreenChanged(bool isFullscreen)
        {
            _isFullscreen = isFullscreen;
            FullscreenChanged?.Invoke(this, isFullscreen);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            // Fire-and-forget cleanup
            _ = Task.Run(async () =>
            {
                try
                {
                    await CleanupAsync();
                }
                catch
                {
                    // Ignore cleanup errors during disposal
                }
            });
        }
    }
}
