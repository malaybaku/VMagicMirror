using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> 設定ファイルの読み書きを行う、わりと権力のあるクラス。 </summary>
    class SettingFileIo
    {
        public SettingFileIo()
            : this(ModelResolver.Instance.Resolve<RootSettingModel>(), ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        /// <summary>
        /// ファイルI/Oを行うときに操作対象とするモデルを指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="model"></param>
        public SettingFileIo(RootSettingModel model, IMessageSender sender)
        {
            _model = model;
            _sender = sender;
        }

        private readonly RootSettingModel _model;
        private readonly IMessageSender _sender;

        public void SaveSetting(string path, SettingFileReadWriteModes mode)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            //データを作ったらXMLで保存してるだけ
            using (var sw = new StreamWriter(path))
            {
                SaveSettingSub(
                    mode,
                    saveData => new XmlSerializer(typeof(SaveData)).Serialize(sw, saveData)
                    );
            }
        }

        /// <summary>
        /// 指定したファイルパスから設定をロードします。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <param name="content"></param>
        /// <param name="fromAutomation"></param>
        public void LoadSetting(
            string path,
            SettingFileReadWriteModes mode,
            SettingFileReadContent content = SettingFileReadContent.All,
            bool fromAutomation = false
            )
        {
            if (!File.Exists(path))
            {
                LogOutput.Instance.Write($"Setting file load requested (mode={mode}), but file does not exist at: {path}");
                return;
            }

            try
            {
                //NOTE: ファイルロードではメッセージが凄い量になるので、
                //コンポジットして「1つの大きいメッセージ」として書き込むためにこうしてます
                _sender.StartCommandComposite();
                LoadSettingSub(path, mode, content, fromAutomation);
                _sender.EndCommandComposite();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load setting file {path} : {ex.Message}");
            }
        }

        //NOTE: Settingとは言ってるが実態はファイル削除なことに注意
        public void DeleteSetting(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// いま指定したファイルにセーブを行ったらそのファイルの中身が書き換わるかどうかをチェックします。
        /// ファイルがないばあいtrueを返します。
        /// よくあるDirty判定と違い、「設定ファイルの文字列を試しに作って書き込み対象ファイルの中身と比べる」
        /// という非常に強引な実装であることに注意して下さい。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool CheckSettingIsDifferent(string path, SettingFileReadWriteModes mode)
        {
            if (!File.Exists(path)) 
            {
                return true;
            }

            var settingInFile = File.ReadAllText(path);

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                SaveSettingSub(
                    mode, 
                    saveData => new XmlSerializer(typeof(SaveData)).Serialize(sw, saveData)
                );
            }
            var settingToSave = sb.ToString();

            return settingInFile != settingToSave;
        }

        private void SaveSettingSub(SettingFileReadWriteModes mode, Action<SaveData> act)
        {
            var autoLoadEnabled = _model.AutoLoadLastLoadedVrm.Value;

            var saveData = new SaveData()
            {
                //ここ若干名前がややこしいが、歴史的経緯によるものです
                IsInternalSaveFile = (mode == SettingFileReadWriteModes.AutoSave),
                LastLoadedVrmFilePath = mode switch
                {
                    SettingFileReadWriteModes.AutoSave => autoLoadEnabled ? _model.LastVrmLoadFilePath : "",
                    SettingFileReadWriteModes.Internal => _model.LastVrmLoadFilePath,
                    _ => "",
                },
                LastLoadedVRoidModelId = mode switch
                {
                    SettingFileReadWriteModes.AutoSave => autoLoadEnabled ? _model.LastLoadedVRoidModelId : "",
                    SettingFileReadWriteModes.Internal => _model.LastLoadedVRoidModelId,
                    _ => "",
                },

                AutoLoadLastLoadedVrm = (mode == SettingFileReadWriteModes.AutoSave) ? autoLoadEnabled : false,
                PreferredLanguageName = (mode == SettingFileReadWriteModes.AutoSave) ? _model.LanguageName.Value : "",
                LoadCharacterWhenLoad = _model.LoadCharacterWhenLoadInternalFile.Value,
                LoadNonCharacterWhenLoad = _model.LoadNonCharacterWhenLoadInternalFile.Value,
                WindowSetting = _model.Window.Save(),
                MotionSetting = _model.Motion.Save(),
                LayoutSetting = _model.Layout.Save(),
                LightSetting = _model.Light.Save(),
                WordToMotionSetting = _model.WordToMotion.Save(),
                VMCPSetting = _model.VMCP.Save(),
                ExternalTrackerSetting = _model.ExternalTracker.Save(),
                AccessorySetting = _model.Accessory.Save(),
                AutomationSetting = _model.Automation.Save(),
                BuddySetting = _model.Buddy.Save(),
            };

            saveData.LastLoadedVrmName =
                (!string.IsNullOrEmpty(saveData.LastLoadedVrmFilePath) ||
                !string.IsNullOrEmpty(saveData.LastLoadedVRoidModelId))
                ? _model.LoadedModelName
                : "";

            //ここだけ互換性の都合で入れ子になってることに注意
            saveData.LayoutSetting.Gamepad = _model.Gamepad.Save();

            act(saveData);
        }

        private void LoadSettingSub(string path, SettingFileReadWriteModes mode, SettingFileReadContent content, bool fromAutomation)
        {
            using (var sr = new StreamReader(path))
            {
                var serializer = new XmlSerializer(typeof(SaveData));
                var saveData = (SaveData?)serializer.Deserialize(sr);
                if (saveData == null)
                {
                    LogOutput.Instance.Write("Setting file loaded, but result was not EntityBasedSaveData.");
                    return;
                }

                if (mode == SettingFileReadWriteModes.AutoSave && saveData.IsInternalSaveFile)
                {
                    //NOTE: AutoSaveの場合、Content == Allのケースしかないため、いちいち調べない
                    _model.LastVrmLoadFilePath = saveData.LastLoadedVrmFilePath ?? "";
                    _model.LastLoadedVRoidModelId = saveData.LastLoadedVRoidModelId ?? "";
                    _model.AutoLoadLastLoadedVrm.Value = saveData.AutoLoadLastLoadedVrm;
                    _model.LanguageName.Value =
                        LanguageSelector.Instance.AvailableLanguageNames.Contains(saveData.PreferredLanguageName ?? "") ?
                        (saveData.PreferredLanguageName ?? "") :
                        "";
                    _model.LoadCharacterWhenLoadInternalFile.Value = saveData.LoadCharacterWhenLoad;
                    _model.LoadNonCharacterWhenLoadInternalFile.Value = saveData.LoadNonCharacterWhenLoad;
                }
                else if (mode == SettingFileReadWriteModes.Internal &&
                    (content == SettingFileReadContent.Character || content == SettingFileReadContent.All)
                    )
                {
                    //NOTE: ここ若干ぎこちない処理。
                    //モデルのロード情報を書き込むが、書き込んだデータは実態に合ってるとは限らないため、
                    //呼び出し元のSaveFileManagerがデータをすぐ元に戻してくれる、という前提で書かれてます

                    //このケースでは最後に使ったローカルVRMのデータは見に行ってもOKなのだが、
                    //存在しないVRMで上書きするのはちょっと問題あるので避けておく
                    if (File.Exists(saveData.LastLoadedVrmFilePath))
                    {
                        _model.LastVrmLoadFilePath = saveData.LastLoadedVrmFilePath ?? "";
                    }
                    else
                    {
                        _model.LastVrmLoadFilePath = "";
                    }

                    //NOTE: オートメーションではVRoid Hubモデルのロード情報は触らない。
                    //どのみちユーザーによるライセンス確認が必要で、オートメーションとして完結しないから。
                    if (fromAutomation)
                    {
                        _model.LastLoadedVRoidModelId = "";
                    }
                    else 
                    { 
                        _model.LastLoadedVRoidModelId = saveData.LastLoadedVRoidModelId ?? "";
                    }
                }

                if (content == SettingFileReadContent.All || content == SettingFileReadContent.NonCharacter)
                {
                    _model.Window.Load(saveData.WindowSetting);
                    _model.Motion.Load(saveData.MotionSetting);
                    _model.Layout.Load(saveData.LayoutSetting);
                    _model.Gamepad.Load(saveData.LayoutSetting?.Gamepad);
                    _model.Light.Load(saveData.LightSetting);
                    _model.WordToMotion.Load(saveData.WordToMotionSetting);
                    _model.VMCP.Load(saveData.VMCPSetting);
                    _model.ExternalTracker.Load(saveData.ExternalTrackerSetting);
                    _model.Accessory.Load(saveData.AccessorySetting);
                    _model.Buddy.Load(saveData.BuddySetting);

                    //固定スロットからロード/セーブする場合にオートメーション設定をいじってしまうと
                    //「オートメーションで設定変えたらオートメーションがオフになって反応しなくなった」という珍事が起きる。ポート番号が変わる場合も同様。
                    //それは困るため、この設定だけ特別に無視する
                    if (mode != SettingFileReadWriteModes.Internal)
                    {
                        _model.Automation.Load(saveData.AutomationSetting);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 設定ファイルをどこから読み書きしているかのパターンです。
    /// </summary>
    internal enum SettingFileReadWriteModes
    {
        /// <summary> 自動セーブ </summary>
        AutoSave,
        /// <summary> 自動ではないが内部的に"_save1"みたいな名前で持つやつ </summary>
        Internal,
        /// <summary> 「設定を保存」でユーザーが好きなファイルに書き出すやつ </summary>
        Exported,
    }

    /// <summary>
    /// 設定ファイルを読み込むときの内容
    /// </summary>
    internal enum SettingFileReadContent
    {
        /// <summary>何も読み込まない: 普通ありえない</summary>
        None,
        /// <summary>ローカルVRMの情報だけ読み込む</summary>
        Character,
        /// <summary>キャラ以外の情報だけ読み込む</summary>
        NonCharacter,
        /// <summary>全て読み込む: 普通はこれ</summary>
        All,
    }

}
