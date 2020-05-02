using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaVncClient.ViewModels;

namespace AvaloniaVncClient.Views
{
    public class MainWindow : Window
    {
        private Button _connectButton = null!;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _connectButton = this.FindControl<Button>("connectButton");
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            var viewModel = (MainWindowViewModel)DataContext;

            // Change connect button text
            // TODO: Move to XAML: https://github.com/AvaloniaUI/Avalonia/issues/1362
            _connectButton.Bind(Button.ContentProperty,
                viewModel.ConnectCommand.IsExecuting.Select(executing => executing ? "Connecting..." : "Connect"));
        }
    }
}
