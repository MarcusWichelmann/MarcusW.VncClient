using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace AvaloniaVncClient
{
    public static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
#if DEBUG
            LogEventLevel logLevel = LogEventLevel.Debug;
#else
            LogEventLevel logLevel = LogEventLevel.Warning;
#endif

            return AppBuilder.Configure<App>().UsePlatformDetect().With(new X11PlatformOptions {
                EnableMultiTouch = true
            }).With(new Win32PlatformOptions {
                EnableMultitouch = true
            }).LogToDebug(logLevel).UseReactiveUI();
        }
    }
}
