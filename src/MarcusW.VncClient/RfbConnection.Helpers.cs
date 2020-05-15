using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        private void LockedSetAndRaiseNotifyWhenChanged<T>(ref T backingField, T newValue, object lockObject,
            [CallerMemberName] string propertyName = "")
        {
            lock (lockObject)
            {
                if (backingField == null && newValue == null)
                    return;
                if (backingField?.Equals(newValue) == true)
                    return;
                backingField = newValue;
            }

            // Raise event outside of the lock to ensure that synchronous handlers don't deadlock when calling methods in this class.
            NotifyPropertyChanged(propertyName);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
