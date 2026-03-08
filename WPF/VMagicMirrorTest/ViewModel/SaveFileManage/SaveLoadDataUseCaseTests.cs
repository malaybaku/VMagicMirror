using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Baku.VMagicMirrorConfig.ViewModel;

namespace Baku.VMagicMirrorConfig.Test.ViewModel.SaveFileManage
{
    public class SaveLoadDataUseCaseTests
    {
        [Test]
        public async Task ExecuteLoadAsync_存在しないファイルでは何もしない()
        {
            var service = new FakeSaveLoadDataService { FileExists = false };
            var interaction = new FakeSaveLoadDataInteraction();
            var useCase = new SaveLoadDataUseCase(null, service, interaction);

            bool dialogClosed = false;
            await useCase.ExecuteLoadAsync(1, true, true, () => dialogClosed = true);

            Assert.That(interaction.LoadConfirmRequestedCount, Is.EqualTo(0));
            Assert.That(service.LoadCallCount, Is.EqualTo(0));
            Assert.That(dialogClosed, Is.False);
        }

        [Test]
        public async Task ExecuteLoadAsync_確認キャンセル時はロードしない()
        {
            var service = new FakeSaveLoadDataService { FileExists = true };
            var interaction = new FakeSaveLoadDataInteraction { LoadConfirmResult = false };
            var useCase = new SaveLoadDataUseCase(null, service, interaction);

            bool dialogClosed = false;
            await useCase.ExecuteLoadAsync(1, true, false, () => dialogClosed = true);

            Assert.That(interaction.LoadConfirmRequestedCount, Is.EqualTo(1));
            Assert.That(service.LoadCallCount, Is.EqualTo(0));
            Assert.That(dialogClosed, Is.False);
        }

        [Test]
        public async Task ExecuteLoadAsync_確認OK時はロードしてダイアログを閉じる()
        {
            var service = new FakeSaveLoadDataService { FileExists = true };
            var interaction = new FakeSaveLoadDataInteraction { LoadConfirmResult = true };
            var useCase = new SaveLoadDataUseCase(null, service, interaction);

            bool dialogClosed = false;
            await useCase.ExecuteLoadAsync(2, true, false, () => dialogClosed = true);

            Assert.That(service.LoadCallCount, Is.EqualTo(1));
            Assert.That(service.LastLoadIndex, Is.EqualTo(2));
            Assert.That(service.LastLoadCharacter, Is.True);
            Assert.That(service.LastLoadNonCharacter, Is.False);
            Assert.That(interaction.LoadNotifiedCount, Is.EqualTo(1));
            Assert.That(dialogClosed, Is.True);
        }

        [Test]
        public async Task ExecuteSaveAsync_不正インデックスでは何もしない()
        {
            var service = new FakeSaveLoadDataService();
            var interaction = new FakeSaveLoadDataInteraction();
            var useCase = new SaveLoadDataUseCase(null, service, interaction);

            bool refreshed = false;
            await useCase.ExecuteSaveAsync(0, () => refreshed = true);

            Assert.That(interaction.SaveConfirmRequestedCount, Is.EqualTo(0));
            Assert.That(service.SaveCallCount, Is.EqualTo(0));
            Assert.That(refreshed, Is.False);
        }

        [Test]
        public async Task ExecuteSaveAsync_確認キャンセル時は保存しない()
        {
            var service = new FakeSaveLoadDataService();
            var interaction = new FakeSaveLoadDataInteraction { SaveConfirmResult = false };
            var useCase = new SaveLoadDataUseCase(null, service, interaction);

            bool refreshed = false;
            await useCase.ExecuteSaveAsync(1, () => refreshed = true);

            Assert.That(interaction.SaveConfirmRequestedCount, Is.EqualTo(1));
            Assert.That(service.SaveCallCount, Is.EqualTo(0));
            Assert.That(refreshed, Is.False);
        }

        [Test]
        public async Task ExecuteSaveAsync_確認OK時は保存してリフレッシュを開始する()
        {
            var service = new FakeSaveLoadDataService();
            var interaction = new FakeSaveLoadDataInteraction { SaveConfirmResult = true };
            var useCase = new SaveLoadDataUseCase(null, service, interaction);

            bool refreshed = false;
            await useCase.ExecuteSaveAsync(3, () => refreshed = true);

            Assert.That(service.SaveCallCount, Is.EqualTo(1));
            Assert.That(service.LastSaveIndex, Is.EqualTo(3));
            Assert.That(interaction.SaveNotifiedCount, Is.EqualTo(1));
            Assert.That(interaction.BeginRefreshCount, Is.EqualTo(1));
            Assert.That(refreshed, Is.True);
        }

        private sealed class FakeSaveLoadDataService : ISaveLoadDataService
        {
            public int FileCount { get; set; } = 15;
            public int FocusedFileIndex { get; set; }
            public bool FileExists { get; set; } = true;

            public int LoadCallCount { get; private set; }
            public int SaveCallCount { get; private set; }
            public int LastLoadIndex { get; private set; } = -1;
            public bool LastLoadCharacter { get; private set; }
            public bool LastLoadNonCharacter { get; private set; }
            public int LastSaveIndex { get; private set; } = -1;

            public bool CheckFileExist(int index) => FileExists;

            public void LoadSetting(int index, bool loadCharacter, bool loadNonCharacter, bool fromAutomation)
            {
                LoadCallCount++;
                LastLoadIndex = index;
                LastLoadCharacter = loadCharacter;
                LastLoadNonCharacter = loadNonCharacter;
            }

            public void SaveCurrentSetting(int index)
            {
                SaveCallCount++;
                LastSaveIndex = index;
            }
        }

        private sealed class FakeSaveLoadDataInteraction : ISaveLoadDataInteraction
        {
            public bool LoadConfirmResult { get; set; } = true;
            public bool SaveConfirmResult { get; set; } = true;

            public int LoadConfirmRequestedCount { get; private set; }
            public int SaveConfirmRequestedCount { get; private set; }
            public int LoadNotifiedCount { get; private set; }
            public int SaveNotifiedCount { get; private set; }
            public int BeginRefreshCount { get; private set; }

            public Task<bool> ConfirmLoadAsync(int index)
            {
                LoadConfirmRequestedCount++;
                return Task.FromResult(LoadConfirmResult);
            }

            public Task<bool> ConfirmSaveAsync(int index)
            {
                SaveConfirmRequestedCount++;
                return Task.FromResult(SaveConfirmResult);
            }

            public void NotifyLoadCompleted(int index) => LoadNotifiedCount++;
            public void NotifySaveCompleted(int index) => SaveNotifiedCount++;

            public void BeginRefresh(Action refreshAction)
            {
                BeginRefreshCount++;
                refreshAction();
            }
        }
    }
}
