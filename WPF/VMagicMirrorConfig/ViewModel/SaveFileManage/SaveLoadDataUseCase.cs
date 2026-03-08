using System;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class SaveLoadDataUseCase
    {
        private readonly RootSettingModel? _rootModel;
        private readonly SaveFileManager _model;
        private readonly ISaveLoadDataInteraction _interaction;

        public SaveLoadDataUseCase(
            RootSettingModel? rootModel,
            SaveFileManager model,
            ISaveLoadDataInteraction interaction)
        {
            _rootModel = rootModel;
            _model = model;
            _interaction = interaction;
        }

        public async Task ExecuteLoadAsync(
            int index,
            bool loadCharacterWhenSettingLoaded,
            bool loadNonCharacterWhenSettingLoaded,
            Action closeDialog)
        {
            // オートセーブ(0番)もロード対象として扱う。
            if (index < 0 || index > SaveFileManager.FileCount || !_model.CheckFileExist(index))
            {
                return;
            }

            if (!await _interaction.ConfirmLoadAsync(index))
            {
                return;
            }

            // 失敗可否の判定は難しいため、現行仕様どおり完了通知を出す。
            _interaction.NotifyLoadCompleted(index);

            // 先に閉じないと、VRoidロード時にダイアログが閉じきれない場合がある。
            closeDialog();

            _model.LoadSetting(
                index,
                loadCharacterWhenSettingLoaded,
                loadNonCharacterWhenSettingLoaded,
                false
            );

            if (_rootModel != null)
            {
                // 次回以降の既定値として、ロード時オプションの選択を保持する。
                _rootModel.LoadCharacterWhenLoadInternalFile.Value = loadCharacterWhenSettingLoaded;
                _rootModel.LoadNonCharacterWhenLoadInternalFile.Value = loadNonCharacterWhenSettingLoaded;
            }
        }

        public async Task ExecuteSaveAsync(int index, Action refresh)
        {
            // 保存は手動スロットのみ。0番(オートセーブ)は対象外。
            if (index <= 0 || index > SaveFileManager.FileCount)
            {
                return;
            }

            // 初回保存でも、次回以降の上書きを見据えて確認は毎回表示する。
            if (!await _interaction.ConfirmSaveAsync(index))
            {
                return;
            }

            _model.SaveCurrentSetting(index);

            // ファイル単位の操作結果は通知を出す。
            _interaction.NotifySaveCompleted(index);

            // 保存後はダイアログを閉じず、更新日時表示を再読込する。
            _interaction.BeginRefresh(refresh);
        }
    }
}
