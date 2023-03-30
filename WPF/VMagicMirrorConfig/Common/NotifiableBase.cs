using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Baku.VMagicMirrorConfig
{
    public abstract class NotifiableBase : INotifyPropertyChanged
    {
        protected bool SetValue<T>(ref T target, T value, [CallerMemberName] string pname = "")
        {
            if (!EqualityComparer<T>.Default.Equals(target, value))
            {
                target = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pname));
                return true;
            }
            return false;
        }

        protected void RaisePropertyChanged([CallerMemberName] string pname = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pname));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
