using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public abstract class ViewModelBase : NotifiableBase
    {
        private static readonly DependencyObject designerModeCheckObject 
            = new DependencyObject();

        protected static bool IsInDegignMode => DesignerProperties.GetIsInDesignMode(designerModeCheckObject);
    }
}
