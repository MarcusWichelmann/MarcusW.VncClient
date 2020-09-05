using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        private T GetWithLock<T>(ref T backingField, object lockObject)
        {
            lock (lockObject)
                return backingField;
        }

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
    }
}
