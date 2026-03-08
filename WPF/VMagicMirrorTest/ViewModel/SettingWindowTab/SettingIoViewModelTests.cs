using Baku.VMagicMirrorConfig.ViewModel;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.Test.ViewModel.SettingWindowTab
{
    public class SettingIoViewModelTests
    {
        [Test]
        public void RequestEnableAutomationCommand_確認ダイアログを呼ぶ()
        {
            using var model = new AutomationSettingModel(new FakeMessageSender());
            var preference = new PreferenceSettingModel();
            var dialog = new FakeDialogInteraction { EnableResult = false };
            var vm = new SettingIoViewModel(model, preference, dialog);

            vm.RequestEnableAutomationCommand.Execute(null);

            Assert.That(dialog.EnableRequestedCount, Is.EqualTo(1));
            Assert.That(model.IsAutomationEnabled.Value, Is.False);
        }

        [Test]
        public void RequestDisableAutomationCommand_確認ダイアログを呼ぶ()
        {
            using var model = new AutomationSettingModel(new FakeMessageSender());
            var preference = new PreferenceSettingModel();
            var dialog = new FakeDialogInteraction { DisableResult = true };
            var vm = new SettingIoViewModel(model, preference, dialog);

            vm.RequestDisableAutomationCommand.Execute(null);

            Assert.That(dialog.DisableRequestedCount, Is.EqualTo(1));
            Assert.That(model.IsAutomationEnabled.Value, Is.False);
        }

        [Test]
        public void ToggleSkipLocalVrmLicenseCheckCommand_確認OKなら値がトグルされる()
        {
            using var model = new AutomationSettingModel(new FakeMessageSender());
            var preference = new PreferenceSettingModel();
            var dialog = new FakeDialogInteraction { ToggleResult = true };
            var vm = new SettingIoViewModel(model, preference, dialog);

            Assert.That(preference.SkipLocalVrmLicenseCheck.Value, Is.False);
            vm.ToggleSkipLocalVrmLicenseCheckCommand.Execute(null);

            Assert.That(dialog.ToggleRequestedCount, Is.EqualTo(1));
            Assert.That(dialog.LastToggleCurrentValue, Is.False);
            Assert.That(preference.SkipLocalVrmLicenseCheck.Value, Is.True);
        }

        private sealed class FakeDialogInteraction : ISettingIoDialogInteraction
        {
            public bool EnableResult { get; set; } = true;
            public bool DisableResult { get; set; } = true;
            public bool ToggleResult { get; set; } = true;

            public int EnableRequestedCount { get; private set; }
            public int DisableRequestedCount { get; private set; }
            public int ToggleRequestedCount { get; private set; }
            public bool LastToggleCurrentValue { get; private set; }

            public Task<bool> ConfirmEnableAutomationAsync()
            {
                EnableRequestedCount++;
                return Task.FromResult(EnableResult);
            }

            public Task<bool> ConfirmDisableAutomationAsync()
            {
                DisableRequestedCount++;
                return Task.FromResult(DisableResult);
            }

            public Task<bool> ConfirmToggleSkipLocalVrmLicenseCheckAsync(bool currentValue)
            {
                ToggleRequestedCount++;
                LastToggleCurrentValue = currentValue;
                return Task.FromResult(ToggleResult);
            }
        }

        private sealed class FakeMessageSender : IMessageSender
        {
            public void SendMessage(Message message)
            {
            }

            public Task<string> QueryMessageAsync(Message message) => Task.FromResult(string.Empty);
        }
    }
}
