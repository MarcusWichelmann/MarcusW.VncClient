using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using AvaloniaVncClient.ViewModels;
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
            this.WhenActivated(disposables => {
                // Bind connect button text to connect command execution
                ConnectButton
                    .Bind(Button.ContentProperty,
                        ViewModel.ConnectCommand.IsExecuting.Select(executing
                            => executing ? "Connecting..." : "Connect")).DisposeWith(disposables);
            });

            AvaloniaXamlLoader.Load(this);
        }
    }
}
