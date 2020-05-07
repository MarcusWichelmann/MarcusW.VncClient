using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaVncClient.Services;
using MarcusW.VncClient;
using ReactiveUI;
using Splat;

namespace AvaloniaVncClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ConnectionManager _connectionManager;

        private RfbConnection? _rfbConnection;

        public RfbConnection? RfbConnection
        {
            get => _rfbConnection;
            private set => this.RaiseAndSetIfChanged(ref _rfbConnection, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        public MainWindowViewModel(ConnectionManager? connectionManager = null)
        {
            _connectionManager = connectionManager ?? Locator.Current.GetService<ConnectionManager>()
                ?? throw new ArgumentNullException(nameof(connectionManager));

            ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
        }

        private async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // Try to connect and set the connection
            //RfbConnection = await _connectionManager.ConnectAsync(cancellationToken).ConfigureAwait(true);
        }
    }
}
