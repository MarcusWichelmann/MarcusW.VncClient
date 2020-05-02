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
        private readonly VncConnectionManager _vncConnectionManager;

        private VncConnection? _vncConnection;

        public VncConnection? VncConnection
        {
            get => _vncConnection;
            private set => this.RaiseAndSetIfChanged(ref _vncConnection, value);
        }

        public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

        public MainWindowViewModel(VncConnectionManager? vncConnectionManager = null)
        {
            _vncConnectionManager = vncConnectionManager ?? Locator.Current.GetService<VncConnectionManager>()
                ?? throw new ArgumentNullException(nameof(vncConnectionManager));

            ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
        }

        private async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // Try to connect and set the connection
            VncConnection = await _vncConnectionManager.ConnectAsync(cancellationToken).ConfigureAwait(true);
        }
    }
}
