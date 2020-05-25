using System;
using System.Net;
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
        private string? _errorMessage;

        public RfbConnection? RfbConnection
        {
            get => _rfbConnection;
            private set => this.RaiseAndSetIfChanged(ref _rfbConnection, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
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
            // TODO: Configure connect parameters
            var parameters = new ConnectParameters {
                Endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 5901)
            };

            try
            {
                // Try to connect and set the connection
                RfbConnection = await _connectionManager.ConnectAsync(parameters, cancellationToken)
                    .ConfigureAwait(true);

                ErrorMessage = null;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
        }
    }
}
