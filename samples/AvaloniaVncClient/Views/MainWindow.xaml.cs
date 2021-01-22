using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaVncClient.ViewModels;
using AvaloniaVncClient.Views.Dialogs;
using ReactiveUI;

namespace AvaloniaVncClient.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private Button ConnectButton => this.FindControl<Button>("ConnectButton");

        private Border TopDockPanel => this.FindControl<Border>("TopDockPanel");
        private Border BottomDockPanel => this.FindControl<Border>("BottomDockPanel");
        private Border RightDockPanel => this.FindControl<Border>("RightDockPanel");

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            this.WhenActivated(disposable => {
                // Bind connect button text to connect command execution
                ConnectButton.Bind(Button.ContentProperty, ViewModel.ConnectCommand.IsExecuting.Select(executing => executing ? "Connecting..." : "Connect"))
                    .DisposeWith(disposable);

                // Handle authentication requests
                ViewModel.InteractiveAuthenticationHandler.EnterPasswordInteraction.RegisterHandler(async context => {
                    string? password = await new EnterPasswordDialog().ShowDialog<string?>(this).ConfigureAwait(true);
                    context.SetOutput(password);
                }).DisposeWith(disposable);
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
            WindowState = fullscreen ? WindowState.FullScreen : WindowState.Normal;

            TopDockPanel.IsVisible = !fullscreen;
            BottomDockPanel.IsVisible = !fullscreen;
            RightDockPanel.IsVisible = !fullscreen;
        }
    }
}
