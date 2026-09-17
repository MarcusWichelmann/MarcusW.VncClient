using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaVncClient.ViewModels;
using AvaloniaVncClient.Views.Dialogs;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace AvaloniaVncClient.Views
{
    public partial class MainView : ReactiveUserControl<MainWindowViewModel>
    {
        private Button _ConnectButton => this.FindControl<Button>("ConnectButton");

        private Border _TopDockPanel => this.FindControl<Border>("TopDockPanel");
        private Border _BottomDockPanel => this.FindControl<Border>("BottomDockPanel");
        private Border _RightDockPanel => this.FindControl<Border>("RightDockPanel");

        public MainView()
        {
            InitializeComponent();

#if DEBUG
            //TopLevel.GetTopLevel(this).AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            this.WhenActivated(disposable => {
                // Bind connect button text to connect command execution
                _ConnectButton.Bind(Button.ContentProperty, ViewModel.ConnectCommand.IsExecuting.Select(executing => executing ? "Connecting..." : "Connect"))
                    .DisposeWith(disposable);

                // Handle authentication requests
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var MainWindow = desktop.MainWindow as MainWindow;
                    ViewModel.InteractiveAuthenticationHandler.EnterPasswordInteraction.RegisterHandler(async context => {
                        string? password = await new EnterPasswordDialog().ShowDialog<string?>(MainWindow).ConfigureAwait(true);
                        context.SetOutput(password);
                    }).DisposeWith(disposable);
                    ViewModel.InteractiveAuthenticationHandler.EnterCredentialsInteraction.RegisterHandler(async context => {
                        (string?, string?) credentials = await new EnterCreadentialsDialog().ShowDialog<(string?, string?)>(MainWindow).ConfigureAwait(true);
                        context.SetOutput(credentials);
                    }).DisposeWith(disposable);
                }
            });

            // Register keybinding for exiting fullscreen
            KeyBindings.Add(new KeyBinding {
                Gesture = new KeyGesture(Key.Escape, KeyModifiers.Control),
                Command = ReactiveCommand.Create(() => SetFullscreenMode(false))
            });

            AvaloniaXamlLoader.Load(this);
        }

        private void OnEnableFullscreenButtonClicked(object? sender, RoutedEventArgs e) => SetFullscreenMode(true);

        private void SetFullscreenMode(bool fullscreen)
        {
            //WindowState = fullscreen ? WindowState.FullScreen : WindowState.Normal;

            _TopDockPanel.IsVisible = !fullscreen;
            _BottomDockPanel.IsVisible = !fullscreen;
            _RightDockPanel.IsVisible = !fullscreen;
        }
    }
}
