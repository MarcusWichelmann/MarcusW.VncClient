using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaVncClient.Services;
using AvaloniaVncClient.ViewModels;
using AvaloniaVncClient.Views;
using Splat;

namespace AvaloniaVncClient
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // Register dependencies
            Locator.CurrentMutable.RegisterLazySingleton(() => new VncConnectionManager());
            Locator.CurrentMutable.RegisterLazySingleton(() => new InteractiveAuthenticationHandler());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
