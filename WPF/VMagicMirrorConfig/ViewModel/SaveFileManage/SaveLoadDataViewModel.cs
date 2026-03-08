using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary>
    /// セーブかロードを行う時のビューモデル
    /// </summary>
    public class SaveLoadDataViewModel : ViewModelBase
    {
        //NOTE: セーブとロードで必要な素材が微妙に違うのでファクトリで作ります

        internal static SaveLoadDataViewModel CreateForSave(SaveFileManager model, Action actToClose)
            => new SaveLoadDataViewModel(null, model, false, actToClose, new SaveLoadDataInteraction());

        internal static SaveLoadDataViewModel CreateForLoad(RootSettingModel rootModel, SaveFileManager model, Action actToClose)
            => new SaveLoadDataViewModel(rootModel, model, true, actToClose, new SaveLoadDataInteraction());


        private SaveLoadDataViewModel(
            RootSettingModel? rootModel,
            SaveFileManager model,
            bool isLoadMode,
            Action actToClose,
            ISaveLoadDataInteraction interaction)
        {
            _rootModel = rootModel;
            _service = new SaveLoadDataService(model);
            _actToClose = actToClose;
            _interaction = interaction;
            _useCase = new SaveLoadDataUseCase(_rootModel, _service, _interaction);
            Items = new ReadOnlyObservableCollection<SaveLoadFileItemViewModel>(_items);
            CancelCommand = new ActionCommand(CloseDialog);

            //NOTE: SaveモードではUIも出ないし何も使わない値なのでfalseのまま放置しとけばOK、という値
            LoadCharacterWhenSettingLoaded
                = new RProperty<bool>(_rootModel?.LoadCharacterWhenLoadInternalFile?.Value ?? false);
            LoadNonCharacterWhenSettingLoaded
                = new RProperty<bool>(_rootModel?.LoadNonCharacterWhenLoadInternalFile?.Value ?? false);

            IsLoadMode = isLoadMode;
            Refresh();
        }

        private readonly RootSettingModel? _rootModel;
        private readonly ISaveLoadDataService _service;
        private readonly Action _actToClose;
        private readonly ISaveLoadDataInteraction _interaction;
        private readonly SaveLoadDataUseCase _useCase;

        private readonly ObservableCollection<SaveLoadFileItemViewModel> _items
            = new ObservableCollection<SaveLoadFileItemViewModel>();
        public ReadOnlyObservableCollection<SaveLoadFileItemViewModel> Items { get; }

        public bool IsLoadMode { get; }

        public ActionCommand CancelCommand { get; }

        //デフォルトではアバターロードだけ有効にして、「同じモデルで服が違うのをパッと切り替えます」みたいなUXを重視しておく。
        public RProperty<bool> LoadCharacterWhenSettingLoaded { get; }
        public RProperty<bool> LoadNonCharacterWhenSettingLoaded { get; }

        private void Refresh()
        {
            _items.Clear();
            for (int i = 0; i <= SaveFileManager.FileCount; i++)
            {
                var meta = SettingFileOverview.CreateOverviewFromFile(SpecialFilePath.GetSaveFilePath(i), i);
                _items.Add(new SaveLoadFileItemViewModel(
                    IsLoadMode, i == _service.FocusedFileIndex, meta, this
                    ));
            }
        }

        public async Task ExecuteLoad(int index)
        {
            // 実処理はUseCaseへ委譲し、ViewModelは入力値の橋渡しに専念する。
            await _useCase.ExecuteLoadAsync(
                index,
                LoadCharacterWhenSettingLoaded.Value,
                LoadNonCharacterWhenSettingLoaded.Value,
                _actToClose
            );
        }

        public async Task ExecuteSave(int index)
        {
            // 実処理はUseCaseへ委譲し、ViewModelは入力値の橋渡しに専念する。
            await _useCase.ExecuteSaveAsync(index, Refresh);
        }

        private void CloseDialog() => _actToClose();
    }
}
