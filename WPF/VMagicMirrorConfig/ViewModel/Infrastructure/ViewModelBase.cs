using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public abstract class ViewModelBase : NotifiableBase
    {
        private static readonly DependencyObject _designModeCheckObject 
            = new DependencyObject();

        protected static bool IsInDesignMode => DesignerProperties.GetIsInDesignMode(_designModeCheckObject);
    }
}
