using System;
using System.Linq;
using System.Net;
using System.Reactive;
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

        private string _host = "fedora-vm";
        private int _port = 5901;
        private RfbConnection? _rfbConnection;
        private string? _errorMessage;

        private readonly ObservableAsPropertyHelper<bool> _parametersValidProperty;

        public string Host
        {
            get => _host;
            set => this.RaiseAndSetIfChanged(ref _host, value);
        }

        public int Port
        {
            get => _port;
            set => this.RaiseAndSetIfChanged(ref _port, value);
        }

        // TODO: Add a way to close existing connections. Maybe a list of multiple connections (shown as tabs)?
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

        public bool ParametersValid => _parametersValidProperty.Value;

        public MainWindowViewModel(ConnectionManager? connectionManager = null)
        {
            _connectionManager = connectionManager ?? Locator.Current.GetService<ConnectionManager>() ?? throw new ArgumentNullException(nameof(connectionManager));

            IObservable<bool> parametersValid = this.WhenAnyValue(vm => vm.Host, vm => vm.Port, (host, port) => {
                // Is it an IP Address or a valid DNS/hostname?
                if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
                    return false;

                // Is the port valid?
                return port >= 0 && port <= 65535;
            });
            _parametersValidProperty = parametersValid.ToProperty(this, nameof(ParametersValid));

            ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync, parametersValid);
        }

        private async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Resolve hostname or IP address
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(Host).ConfigureAwait(true);

                // TODO: Configure connect parameters
                var parameters = new ConnectParameters {
                    // TODO: Pass multiple addresses, because the first one might not be the reachable one (e.g. no dual stack)
                    // TODO: Maybe move the resolve logic to the TransportConnector and just pass the host string and port?
                    Endpoint = new IPEndPoint(hostEntry.AddressList.First(), Port)
                };

                // Try to connect and set the connection
                RfbConnection = await _connectionManager.ConnectAsync(parameters, cancellationToken).ConfigureAwait(true);

                ErrorMessage = null;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
        }
    }
}
