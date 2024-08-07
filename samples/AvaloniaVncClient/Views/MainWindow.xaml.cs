using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaVncClient.ViewModels;
using AvaloniaVncClient.Views.Dialogs;
using ReactiveUI;

// ReSharper disable once RedundantUsingDirective -- Required for AttachDevTools
using Avalonia;

namespace AvaloniaVncClient.Views;

public class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private Button ConnectButton => this.FindControl<Button>("ConnectButton")!;

    private Border TopDockPanel => this.FindControl<Border>("TopDockPanel")!;
    private Border BottomDockPanel => this.FindControl<Border>("BottomDockPanel")!;
    private Border RightDockPanel => this.FindControl<Border>("RightDockPanel")!;

    private void InitializeComponent()
    {
        this.WhenActivated(disposable => {
            // Bind connect button text to connect command execution
            ConnectButton
                .Bind(ContentProperty,
                    ViewModel!.ConnectCommand.IsExecuting.Select(executing => executing ? "Connecting..." : "Connect"))
                .DisposeWith(disposable);

            // Handle authentication requests
            ViewModel.InteractiveAuthenticationHandler.EnterPasswordInteraction.RegisterHandler(async context => {
                string? password = await new EnterPasswordDialog().ShowDialog<string?>(this).ConfigureAwait(true);
                context.SetOutput(password);
            }).DisposeWith(disposable);
        });

        // Register keybinding for exiting fullscreen
        KeyBindings.Add(new() {
            Gesture = new(Key.Escape, KeyModifiers.Control),
            Command = ReactiveCommand.Create(() => SetFullscreenMode(false)),
        });

        AvaloniaXamlLoader.Load(this);
    }

    private void OnEnableFullscreenButtonClicked(object? _1, RoutedEventArgs _2) => SetFullscreenMode(true);

    private void SetFullscreenMode(bool fullscreen)
    {
        WindowState = fullscreen ? WindowState.FullScreen : WindowState.Normal;

        TopDockPanel.IsVisible = !fullscreen;
        BottomDockPanel.IsVisible = !fullscreen;
        RightDockPanel.IsVisible = !fullscreen;
    }
}
