using Baku.VMagicMirrorConfig.ViewModel;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.Test.ViewModel.SettingWindowTab
{
    public class VMCPSettingViewModelTests
    {
        [Test]
        public void EnableVMCPTab_確認ダイアログが呼ばれる()
        {
            var vmcpModel = new VMCPSettingModel(new FakeMessageSender(), new FakeMessageReceiver());
            var preferenceModel = new PreferenceSettingModel();
            var dialog = new FakeVMCPDialogInteraction { EnableResult = false };
            var vm = new VMCPSettingViewModel(vmcpModel, preferenceModel, dialog);

            vm.EnableVMCPTabOnControlPanelCommand.Execute(null);

            Assert.That(dialog.EnableRequestedCount, Is.EqualTo(1));
            Assert.That(preferenceModel.ShowVMCPTabOnControlPanel.Value, Is.False);
        }

        [Test]
        public void DisableVMCPTab_確認OKで関連フラグをOFFにする()
        {
            var vmcpModel = new VMCPSettingModel(new FakeMessageSender(), new FakeMessageReceiver());
            vmcpModel.VMCPEnabled.Value = true;
            vmcpModel.VMCPSendEnabled.Value = true;

            var preferenceModel = new PreferenceSettingModel();
            preferenceModel.ShowVMCPTabOnControlPanel.Value = true;

            var dialog = new FakeVMCPDialogInteraction { DisableResult = true };
            var vm = new VMCPSettingViewModel(vmcpModel, preferenceModel, dialog);

            vm.DisableVMCPTabOnControlPanelCommand.Execute(null);

            Assert.That(dialog.DisableRequestedCount, Is.EqualTo(1));
            Assert.That(preferenceModel.ShowVMCPTabOnControlPanel.Value, Is.False);
            Assert.That(vmcpModel.VMCPEnabled.Value, Is.False);
            Assert.That(vmcpModel.VMCPSendEnabled.Value, Is.False);
        }

        private sealed class FakeVMCPDialogInteraction : IVMCPDialogInteraction
        {
            public bool EnableResult { get; set; } = true;
            public bool DisableResult { get; set; } = true;
            public int EnableRequestedCount { get; private set; }
            public int DisableRequestedCount { get; private set; }

            public Task<bool> ConfirmEnableVMCPTabAsync()
            {
                EnableRequestedCount++;
                return Task.FromResult(EnableResult);
            }

            public Task<bool> ConfirmDisableVMCPTabAsync()
            {
                DisableRequestedCount++;
                return Task.FromResult(DisableResult);
            }
        }

        private sealed class FakeMessageSender : IMessageSender
        {
            public void SendMessage(Message message)
            {
            }

            public Task<string> QueryMessageAsync(Message message) => Task.FromResult(string.Empty);
        }

        private sealed class FakeMessageReceiver : IMessageReceiver
        {
#pragma warning disable CS0067
            public event Action<CommandReceivedData> ReceivedCommand;
            public event EventHandler<QueryReceivedEventArgs> ReceivedQuery;
#pragma warning restore CS0067

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }
    }
}
