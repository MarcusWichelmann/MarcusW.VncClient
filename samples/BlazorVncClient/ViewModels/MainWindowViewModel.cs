using BlazorVncClient.Services;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using MarcusW.VncClient.Rendering;
using ReactiveUI;
using System;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorVncClient.ViewModels
{
    public class MainWindowViewModel: ViewModelBase
    {
        private readonly ConnectionManager _connectionManager;

        private string _host = "10.203.152.154";
        private int _port = 5900;
        private VncConnectionWrapper? _rfbConnection;
        private string? _errorMessage;

        private readonly ObservableAsPropertyHelper<bool> _parametersValidProperty;

        public InteractiveAuthenticationHandler InteractiveAuthenticationHandler { get; }

        public bool IsTightAvailable => DefaultImplementation.IsTightAvailable;

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
        public VncConnectionWrapper? RfbConnection
        {
            get => _rfbConnection;
            set => this.RaiseAndSetIfChanged(ref _rfbConnection, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        public bool ParametersValid => _parametersValidProperty.Value;

        public MainWindowViewModel(ConnectionManager connectionManager, InteractiveAuthenticationHandler interactiveAuthenticationHandler)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            InteractiveAuthenticationHandler = interactiveAuthenticationHandler ?? throw new ArgumentNullException(nameof(interactiveAuthenticationHandler));

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
                // TODO: Configure connect parameters
                var parameters = new ConnectParameters {
                    TransportParameters = new TcpTransportParameters {
                        Host = Host,
                        Port = Port
                    }
                };

                // Try to connect and wrap the connection
                var rawConnection = await _connectionManager.ConnectAsync(parameters, cancellationToken).ConfigureAwait(true);
                RfbConnection = new VncConnectionWrapper { Connection = rawConnection };

                ErrorMessage = null;
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
        }
    }
}
