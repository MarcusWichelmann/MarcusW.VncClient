using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaVncClient.Services;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using ReactiveUI;
using Splat;

namespace AvaloniaVncClient.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ConnectionManager _connectionManager;

    private readonly ObservableAsPropertyHelper<bool> _parametersValidProperty;
    private string? _errorMessage;

    private string _host = "fedora-vm";
    private int _port = 5901;
    private RfbConnection? _rfbConnection;

    public MainWindowViewModel(ConnectionManager? connectionManager = null,
        InteractiveAuthenticationHandler? interactiveAuthenticationHandler = null)
    {
        _connectionManager = connectionManager ?? Locator.Current.GetService<ConnectionManager>()
            ?? throw new ArgumentNullException(nameof(connectionManager));
        InteractiveAuthenticationHandler = interactiveAuthenticationHandler
            ?? Locator.Current.GetService<InteractiveAuthenticationHandler>()
            ?? throw new ArgumentNullException(nameof(interactiveAuthenticationHandler));

        IObservable<bool> parametersValid = this.WhenAnyValue(vm => vm.Host, vm => vm.Port, (host, port) => {
            // Is it an IP Address or a valid DNS/hostname?
            if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                return false;
            }

            // Is the port valid?
            return port is >= 0 and <= 65535;
        });
        _parametersValidProperty = parametersValid.ToProperty(this, nameof(ParametersValid));

        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync, parametersValid);
    }

    public InteractiveAuthenticationHandler InteractiveAuthenticationHandler { get; }

#pragma warning disable CA1822

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public bool IsTightAvailable => DefaultImplementation.IsTightAvailable;
#pragma warning restore CA1822

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

    private async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Configure connect parameters
            var parameters = new ConnectParameters {
                TransportParameters = new TcpTransportParameters {
                    Host = Host,
                    Port = Port,
                },
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
