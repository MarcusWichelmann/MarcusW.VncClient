using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient;

namespace BlazorVncClient.Services
{
    /// <summary>
    /// Wrapper around RfbConnection that handles disposal more gracefully
    /// </summary>
    public class VncConnectionWrapper : INotifyPropertyChanged, IDisposable
    {
        private RfbConnection? _connection;
        private bool _disposed;
        private readonly object _lock = new object();

        public event PropertyChangedEventHandler? PropertyChanged;

        public RfbConnection? Connection
        {
            get
            {
                lock (_lock)
                {
                    return _disposed ? null : _connection;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_disposed) return;
                    
                    if (_connection != value)
                    {
                        var oldConnection = _connection;
                        _connection = value;
                        
                        if (oldConnection != null)
                        {
                            oldConnection.PropertyChanged -= OnConnectionPropertyChanged;
                            // Dispose old connection in isolated task
                            _ = Task.Run(() => SafeDisposeConnection(oldConnection));
                        }
                        
                        if (_connection != null)
                        {
                            _connection.PropertyChanged += OnConnectionPropertyChanged;
                        }
                        
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Connection)));
                    }
                }
            }
        }

        private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public async Task DisconnectAsync()
        {
            RfbConnection? connectionToClose = null;
            
            lock (_lock)
            {
                if (_disposed || _connection == null) return;
                connectionToClose = _connection;
                _connection = null;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Connection)));

            if (connectionToClose != null)
            {
                // Use completely isolated task to prevent exceptions from bubbling up
                await Task.Run(async () =>
                {
                    try
                    {
                        await SafeDisposeConnection(connectionToClose);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception in disconnect task (isolated): {ex.Message}");
                        // Exception is isolated in this task and won't affect the main app
                    }
                });
            }
        }

        private static async Task SafeDisposeConnection(RfbConnection connection)
        {
            try
            {
                Console.WriteLine("Starting isolated connection disposal...");
                
                // Try graceful close first
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await connection.CloseAsync();
                    Console.WriteLine("Connection closed gracefully");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Close operation was cancelled (expected)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Close failed: {ex.Message}");
                }

                // Give background threads time to settle
                await Task.Delay(200);

                // Final disposal
                try
                {
                    connection.Dispose();
                    Console.WriteLine("Connection disposed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Dispose failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SafeDisposeConnection failed: {ex.Message}");
                // This exception is isolated and won't propagate
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                if (_connection != null)
                {
                    _connection.PropertyChanged -= OnConnectionPropertyChanged;
                    // Fire and forget - let it dispose in background
                    _ = Task.Run(() => SafeDisposeConnection(_connection));
                    _connection = null;
                }
            }
        }
    }
}
