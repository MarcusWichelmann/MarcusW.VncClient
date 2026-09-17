using Microsoft.Extensions.DependencyInjection;
using MarcusW.VncClient.Blazor.Services;

namespace MarcusW.VncClient.Blazor.Extensions
{
    /// <summary>
    /// Extension methods for configuring VNC services in the DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all VNC client services to the service collection
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddVncClientServices(this IServiceCollection services)
        {
            // Add core VNC client
            services.AddScoped<VncClient>();
            
            // Add JavaScript interop
            services.AddScoped<JsInterop>();
            
            // Add VNC-specific services
            services.AddScoped<IFramebufferService, FramebufferService>();
            services.AddScoped<IInputService, InputService>();
            services.AddScoped<IRenderingService, RenderingService>();
            services.AddScoped<IFullscreenService, FullscreenService>();
            
            return services;
        }
        
        /// <summary>
        /// Adds VNC client services with custom configuration
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddVncClientServices(this IServiceCollection services, Action<VncClientOptions> configure)
        {
            // Configure options
            services.Configure(configure);
            
            // Add core services
            return services.AddVncClientServices();
        }
    }
    
    /// <summary>
    /// Configuration options for VNC client services
    /// </summary>
    public class VncClientOptions
    {
        /// <summary>
        /// Default pixel format to use when none is specified
        /// </summary>
        public PixelFormat? DefaultPixelFormat { get; set; }
        
        /// <summary>
        /// Whether to enable dirty rectangle rendering optimization
        /// </summary>
        public bool EnableDirtyRectangleRendering { get; set; } = true;
        
        /// <summary>
        /// Maximum number of dirty rectangles to track before falling back to full framebuffer rendering
        /// </summary>
        public int MaxDirtyRectangles { get; set; } = 50;
        
        /// <summary>
        /// Whether to enable framebuffer caching for better performance
        /// </summary>
        public bool EnableFramebufferCaching { get; set; } = true;
        
        /// <summary>
        /// Default canvas size when none is specified
        /// </summary>
        public Size DefaultCanvasSize { get; set; } = new Size(800, 600);
        
        /// <summary>
        /// Default DPI settings for rendering
        /// </summary>
        public double DefaultDpi { get; set; } = 96.0;
    }
}
