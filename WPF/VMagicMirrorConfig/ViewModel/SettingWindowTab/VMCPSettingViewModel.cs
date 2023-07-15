using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class VMCPSettingViewModel : SettingViewModelBase
    {
        public VMCPSettingViewModel() 
        {
            _isDirty = new RProperty<bool>(false, _ => UpdateCanApply());
        }

        public VMCPSourceItemViewModel Source1 { get; private set; }
        public VMCPSourceItemViewModel Source2 { get; private set; }
        public VMCPSourceItemViewModel Source3 { get; private set; }

        public RProperty<bool> CanApply = new (false);

        private readonly RProperty<bool> _isDirty;

        public void SetDirty() => _isDirty.Value = true;
        
        public ActionCommand ApplyChangeCommand { get; }
        public ActionCommand RevertChangeCommand { get; }

        private void UpdateCanApply()
        {
            CanApply.Value = _isDirty.Value &&
                !Source1.PortNumberIsInvalid.Value &&
                !Source2.PortNumberIsInvalid.Value &&
                !Source3.PortNumberIsInvalid.Value;
        }

        private void LoadCurrentSettings()
        {
            //TODO: Modelから何か持ってくる
            _isDirty.Value = false;
        }

        private void SaveCurrentSettings()
        {
            //TODO: Modelに何かぶつける
            _isDirty.Value = false;
        }
    }
}
