using Microsoft.AspNetCore.Components.Web;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Service for handling VNC input events (mouse and keyboard)
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// Handle mouse button down event
        /// </summary>
        Task HandleMouseDownAsync(MouseEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter);
        
        /// <summary>
        /// Handle mouse button up event
        /// </summary>
        Task HandleMouseUpAsync(MouseEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter);
        
        /// <summary>
        /// Handle mouse move event
        /// </summary>
        Task HandleMouseMoveAsync(MouseEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter);
        
        /// <summary>
        /// Handle mouse wheel event
        /// </summary>
        Task HandleMouseWheelAsync(WheelEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter);
        
        /// <summary>
        /// Handle key down event
        /// </summary>
        Task HandleKeyDownAsync(KeyboardEventArgs e, RfbConnection? connection, Func<Task> exitFullscreenAction, bool isFullscreen);
        
        /// <summary>
        /// Handle key up event
        /// </summary>
        Task HandleKeyUpAsync(KeyboardEventArgs e, RfbConnection? connection, bool isFullscreen);
    }
}
