using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        /// <summary>
        /// Gets a value under a lock. Required for value types where reads may not be atomic
        /// (e.g., structs larger than pointer size like <see cref="PixelFormat"/> or <see cref="Size"/>).
        /// </summary>
        private T GetWithLock<T>(ref T backingField, object lockObject)
        {
            lock (lockObject)
                return backingField;
        }

        /// <summary>
        /// Sets a value under a lock and raises PropertyChanged if the value changed.
        /// Required for value types where reads/writes may not be atomic.
        /// </summary>
        private void RaiseAndSetIfChangedWithLock<T>(ref T backingField, T newValue, object lockObject, [CallerMemberName] string propertyName = "")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbConnection));

            lock (lockObject)
            {
                if (EqualityComparer<T>.Default.Equals(backingField, newValue))
                    return;
                backingField = newValue;
            }

            // Raise event outside of the lock to ensure that synchronous handlers don't deadlock when calling methods in this class.
            NotifyPropertyChanged(propertyName);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetConnectionState(ConnectionState newState, string? reason = null, Exception? exception = null, bool isManualAction = false)
        {
            // Atomically read current state and update to new state
            var previousState = (ConnectionState)Interlocked.Exchange(
                ref _connectionStateValue, 
                (int)newState);
            
            // Only raise events if the state actually changed
            if (previousState == newState)
                return;
            
            // Notify property changed
            NotifyPropertyChanged(nameof(ConnectionState));
            
            // Raise the ConnectionStateChanged event
            var args = new ConnectionStateChangedEventArgs(
                previousState,
                newState,
                reason,
                exception,
                _reconnectAttemptCount,
                isManualAction);
            ConnectionStateChanged?.Invoke(this, args);
        }
    }
}
