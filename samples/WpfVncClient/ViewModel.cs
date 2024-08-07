using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using Microsoft.Extensions.Logging;
using WpfVncClient.Logging;
using WpfVncClient.Services;

namespace WpfVncClient;

[PublicAPI]
public class ViewModel : INotifyPropertyChanged
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<ViewModel> _logger;
    private readonly ReactiveLog _reactiveLog;
    private readonly SynchronizationContext? _sync;
    private bool _autoResize;
    private string? _errorMessage;
    private string _host = "localhost";
    private int _port = 5900;
    private RfbConnection? _rfbConnection;

    public ViewModel(ConnectionManager connectionManager, ILogger<ViewModel> logger, ReactiveLog reactiveLog)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _reactiveLog = reactiveLog;
        ConnectCommand = new(_ => ConnectAsync());
        _sync = SynchronizationContext.Current;
        reactiveLog.Subject.Subscribe(AddLog);
    }

    public DelegateCommand ConnectCommand { get; set; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (value == _errorMessage)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> LogList { get; set; } = [];

    public RfbConnection? RfbConnection
    {
        get => _rfbConnection;
        set
        {
            if (Equals(value, _rfbConnection))
            {
                return;
            }

            _rfbConnection = value;
            OnPropertyChanged();
        }
    }

    public bool AutoResize
    {
        get => _autoResize;
        set
        {
            if (value == _autoResize)
            {
                return;
            }

            _autoResize = value;
            OnPropertyChanged();
        }
    }

    public string Host
    {
        get => _host;
        set
        {
            if (value == _host)
            {
                return;
            }

            _host = value;
            OnPropertyChanged();
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (value == _port)
            {
                return;
            }

            _port = value;
            OnPropertyChanged();
        }
    }

#pragma warning disable CA1822

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public bool IsTightAvailable => DefaultImplementation.IsTightAvailable;
#pragma warning restore CA1822

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    private void AddLog(string s)
    {
        _sync?.Post(_ => {
            LogList.Insert(0, s);
        }, null);
    }

    private async Task ConnectAsync()
    {
        try
        {
            // TODO: Configure connect parameters
            var authenticationHandler = new InteractiveAuthenticationHandler(SynchronizationContext.Current);

            var parameters = new ConnectParameters {
                TransportParameters = new TcpTransportParameters {
                    Host = Host,
                    Port = Port,
                },
                AuthenticationHandler = authenticationHandler,
            };

            _logger.LogInformation("Connecting");

            // Try to connect and set the connection
            RfbConnection = await _connectionManager.ConnectAsync(parameters).ConfigureAwait(true);

            ErrorMessage = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }
}
