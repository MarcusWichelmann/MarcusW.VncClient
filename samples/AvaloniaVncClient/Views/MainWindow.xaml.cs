using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
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

            AvaloniaXamlLoader.Load(this);
        }
    }
}
