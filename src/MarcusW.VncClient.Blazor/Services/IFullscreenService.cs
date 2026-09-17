using Microsoft.JSInterop;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Service for managing fullscreen functionality
    /// </summary>
    public interface IFullscreenService : IDisposable
    {
        /// <summary>
        /// Gets whether the element is currently in fullscreen mode
        /// </summary>
        bool IsFullscreen { get; }
        
        /// <summary>
        /// Event raised when fullscreen status changes
        /// </summary>
        event EventHandler<bool>? FullscreenChanged;
        
        /// <summary>
        /// Enter fullscreen mode for the specified element
        /// </summary>
        Task<bool> EnterFullscreenAsync(string elementId);
        
        /// <summary>
        /// Exit fullscreen mode
        /// </summary>
        Task<bool> ExitFullscreenAsync();
        
        /// <summary>
        /// Toggle fullscreen mode for the specified element
        /// </summary>
        Task<bool> ToggleFullscreenAsync(string elementId);
        
        /// <summary>
        /// Initialize fullscreen change listener
        /// </summary>
        Task InitializeAsync(DotNetObjectReference<IFullscreenService> dotNetObjectRef);
        
        /// <summary>
        /// Cleanup fullscreen change listener
        /// </summary>
        Task CleanupAsync();
    }
}
