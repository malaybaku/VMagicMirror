using Baku.VMagicMirrorConfig.ViewModel;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.Test.ViewModel.ControlPanel
{
    public class HomeResetUseCaseTests
    {
        [Test]
        public async Task ExecuteAsync_確認キャンセル時は何もしない()
        {
            var appQuitSetting = new AppQuitSetting();
            var storage = new FakeHomeResetStorage();
            var interaction = new FakeHomeResetInteraction { ConfirmResult = false };
            var useCase = new HomeResetUseCase(appQuitSetting, storage, interaction);

            await useCase.ExecuteAsync();

            Assert.That(appQuitSetting.SkipAutoSaveAndRestart, Is.False);
            Assert.That(storage.DeleteAutoSaveCalled, Is.False);
            Assert.That(storage.DeletePreferenceCalled, Is.False);
            Assert.That(interaction.CloseMainWindowCalled, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_確認OK時は設定削除して終了する()
        {
            var appQuitSetting = new AppQuitSetting();
            var storage = new FakeHomeResetStorage();
            var interaction = new FakeHomeResetInteraction { ConfirmResult = true };
            var useCase = new HomeResetUseCase(appQuitSetting, storage, interaction);

            await useCase.ExecuteAsync();

            Assert.That(appQuitSetting.SkipAutoSaveAndRestart, Is.True);
            Assert.That(storage.DeleteAutoSaveCalled, Is.True);
            Assert.That(storage.DeletePreferenceCalled, Is.True);
            Assert.That(interaction.CloseMainWindowCalled, Is.True);
        }

        private sealed class FakeHomeResetStorage : IHomeResetStorage
        {
            public bool DeleteAutoSaveCalled { get; private set; }
            public bool DeletePreferenceCalled { get; private set; }

            public void DeleteAutoSaveFile() => DeleteAutoSaveCalled = true;
            public void DeletePreferenceSaveFile() => DeletePreferenceCalled = true;
        }

        private sealed class FakeHomeResetInteraction : IHomeResetInteraction
        {
            public bool ConfirmResult { get; set; }
            public bool CloseMainWindowCalled { get; private set; }

            public Task<bool> ConfirmResetToDefaultAsync() => Task.FromResult(ConfirmResult);
            public void CloseMainWindow() => CloseMainWindowCalled = true;
        }
    }
}
