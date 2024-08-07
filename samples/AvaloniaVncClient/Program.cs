using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.ReactiveUI;

namespace AvaloniaVncClient;

public static class Program
{
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
#if DEBUG
        var logLevel = LogEventLevel.Debug;
#else
        var logLevel = LogEventLevel.Warning;
#endif

        return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace(logLevel).UseReactiveUI();
    }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
}
