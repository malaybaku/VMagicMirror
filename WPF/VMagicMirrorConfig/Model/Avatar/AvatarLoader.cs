using Baku.VMagicMirror;
using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// アバターのロード処理やロード後の体型補正、ロード中の入力ガードの面倒までを見るクラス
    /// </summary>
    class AvatarLoader
    {
        public AvatarLoader() : this(
            ModelResolver.Instance.Resolve<RootSettingModel>(),
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>(),
            ModelResolver.Instance.Resolve<SaveFileManager>()
            )
        {
        }

        public AvatarLoader(RootSettingModel model, IMessageSender sender, IMessageReceiver receiver, SaveFileManager saveFileManager)
        {
            _setting = model;
            _sender = sender;
            receiver.ReceivedCommand += OnReceiveCommand;

            //このイベントの繋ぎを無関係な別クラスでやってもOK
            saveFileManager.VRoidModelLoadRequested += id => LoadSavedVRoidModelAsync(id, false);
            saveFileManager.LocalModelLoadRequested += path => LoadLocalModel(path);
            _loadTimer.Tick += (_, _) => UpdateLoadStatus();
            LanguageSelector.Instance.LanguageChanged += UpdateLoadStatus;
        }

        private readonly RootSettingModel _setting;
        private readonly IMessageSender _sender;

        public RProperty<bool> IsLocalModelLoading { get; } = new(false);
        public RProperty<string> LocalModelLoadMessage { get; } = new("");
        private readonly DispatcherTimer _loadTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly Stopwatch _loadElapsed = new();
        private string _loadingFileName = "";
        private bool _isLocalLoadConfirmationActive;

        // All local load entry points use this method to avoid overlapping requests.
        public bool LoadLocalModel(string path)
        {
            if (IsLocalModelLoading.Value || _isVRoidHubUiActive || !File.Exists(path)) return false;
            _loadingFileName = Path.GetFileName(path);
            _loadElapsed.Restart();
            IsLocalModelLoading.Value = true;
            UpdateLoadStatus();
            _loadTimer.Start();
            _sender.SendMessage(MessageFactory.OpenVrm(path));
            return true;
        }

        private void UpdateLoadStatus()
        {
            if (!IsLocalModelLoading.Value) return;
            var key = _loadElapsed.Elapsed.TotalSeconds >= 20 ? "ModelLoad_VerySlow" :
                _loadElapsed.Elapsed.TotalSeconds >= 10 ? "ModelLoad_Slow_Format" : "ModelLoad_Loading_Format";
            LocalModelLoadMessage.Value = string.Format(LocalizedString.GetString(key), _loadingFileName);
        }

        public void StopLoadStatus()
        {
            _loadTimer.Stop();
            _loadElapsed.Reset();
            IsLocalModelLoading.Value = false;
            LocalModelLoadMessage.Value = "";
        }

        private bool _isVRoidHubUiActive;
        private bool? _windowTransparentBeforeLoadProcess;

        private CancellationTokenSource? _ctsForDetectVrm10;

        private void OnReceiveCommand(CommandReceivedData e)
        {
            switch (e.Command)
            {
                case VmmServerCommands.LocalModelLoadEnded:
                    StopLoadStatus();
                    break;
                case VmmServerCommands.VRoidModelLoadCompleted:
                    //WPF側のダイアログによるUIガードを終了: _isVRoidHubUiActiveフラグは別のとこで折るのでここでは無視でOK
                    if (_isVRoidHubUiActive)
                    {
                        MessageBoxWrapper.Instance.SetDialogResult(false);
                    }

                    //ファイルパスではなくモデルID側を最新情報として覚えておく
                    _setting.OnVRoidModelLoaded(e.GetStringValue());

                    break;
                case VmmServerCommands.VRoidModelLoadCanceled:
                    //WPF側のダイアログによるUIガードを終了
                    if (_isVRoidHubUiActive)
                    {
                        MessageBoxWrapper.Instance.SetDialogResult(false);
                    }
                    break;
                case VmmServerCommands.ModelNameConfirmedOnLoad:
                    //ともかくモデルがロードされているため、実態に合わせておく
                    _setting.LoadedModelName = e.GetStringValue();
                    break;
            }
        }

        /// <summary>
        /// ファイルパスの取得処理を指定して、VRMをロードします。
        /// Funcにはファイルダイアログを呼び出す処理や、セーブ済みのファイルパスを指定する処理を指定します。
        /// </summary>
        /// <param name="getFilePathProcess"></param>
        public async Task LoadVrm(Func<string> getFilePathProcess, bool skipLicenceCheck)
        {
            if (IsLocalModelLoading.Value || _isLocalLoadConfirmationActive || _isVRoidHubUiActive) return;

            if (!skipLicenceCheck)
            {
                PrepareShowUiOnUnity();
            }

            RefreshCts();
            _ctsForDetectVrm10 = new();
            var token = _ctsForDetectVrm10.Token;

            string filePath = getFilePathProcess();
            if (!File.Exists(filePath))
            {
                if (!skipLicenceCheck)
                {
                    EndShowUiOnUnity();
                }

                return;
            }
            
            // ライセンスチェックをスキップする場合、シンプルに直ちにロードが始まる。
            // とくに「ロードしますか？」の確認ダイアログも省くのは、確認時にファイルパスとかが出ない挙動であり確認を挟む意義が薄いため。
            if (skipLicenceCheck)
            {
                LoadLocalModel(filePath);
                //NOTE: この時点だとモデルのロードの成否が不明なため、
                //Unityからロード成功を通知されるまでは記録しないのも手
                _setting.OnLocalModelLoaded(filePath);

                var snackbarMessage = string.Format(
                    LocalizedString.GetString("Snackbar_LocalVrmLicenceCheckSkipped_Format"),
                    Path.GetFileName(filePath)
                    );
                SnackbarWrapper.Enqueue(snackbarMessage);
            }
            else
            {
                _sender.SendMessage(MessageFactory.OpenVrmPreview(filePath));
                var indication = MessageIndication.LoadVrmConfirmation();

                try
                {
                    _isLocalLoadConfirmationActive = true;
                    var res = await MessageBoxWrapper.Instance.ShowAsync(
                        indication.Title,
                        indication.Content,
                        MessageBoxWrapper.MessageBoxStyle.OKCancel,
                        token
                        );

                    if (res)
                    {
                        LoadLocalModel(filePath);
                        //NOTE: この時点だとモデルのロードの成否が不明なため、
                        //Unityからロード成功を通知されるまでは記録しないのも手
                        _setting.OnLocalModelLoaded(filePath);
                    }
                    else
                    {
                        _sender.SendMessage(MessageFactory.CancelLoadVrm());
                    }
                }
                finally
                {
                    _isLocalLoadConfirmationActive = false;
                    EndShowUiOnUnity();
                }
            }
        }

        public async Task ConnectToVRoidHubAsync()
        {
            if (IsLocalModelLoading.Value || _isLocalLoadConfirmationActive || _isVRoidHubUiActive) return;
            PrepareShowUiOnUnity();

            _sender.SendMessage(MessageFactory.OpenVRoidSdkUi());

            //VRoidHub側の操作が終わるまでダイアログでガードをかける: モーダル的な管理状態をファイルロードの場合と揃える為
            _isVRoidHubUiActive = true;
            var message = MessageIndication.ShowVRoidSdkUi();
            bool _ = await MessageBoxWrapper.Instance.ShowAsync(
                message.Title, message.Content, MessageBoxWrapper.MessageBoxStyle.None
                );

            //モデルロード完了またはキャンセルによってここに来るので、共通の処理をして終わり
            _isVRoidHubUiActive = false;
            EndShowUiOnUnity();
        }

        public void LoadLastLoadedLocalVrm()
        {
            if (File.Exists(_setting.LastVrmLoadFilePath))
            {
                LoadLocalModel(_setting.LastVrmLoadFilePath);
            }
        }

        public async void LoadSavedVRoidModelAsync(string modelId, bool fromAutoSave)
        {
            if (IsLocalModelLoading.Value || _isLocalLoadConfirmationActive || _isVRoidHubUiActive || string.IsNullOrEmpty(modelId))
            {
                return;
            }

            PrepareShowUiOnUnity();

            //NOTE: モデルIDを載せる以外は通常のUIオープンと同じフロー
            _sender.SendMessage(MessageFactory.RequestLoadVRoidWithId(modelId));

            _isVRoidHubUiActive = true;
            //自動セーブなら「前回のモデル」だしそれ以外なら「設定ファイルに乗ってたモデル」となる。分けといたほうがわかりやすいので分ける。
            var message = fromAutoSave
                ? MessageIndication.ShowLoadingPreviousVRoid()
                : MessageIndication.ShowLoadingSavedVRoidModel();
            bool _ = await MessageBoxWrapper.Instance.ShowAsync(
                message.Title, message.Content, MessageBoxWrapper.MessageBoxStyle.None
                );

            //モデルロード完了またはキャンセルによってここに来るので、共通の処理をして終わり
            _isVRoidHubUiActive = false;
            EndShowUiOnUnity();
        }

        public void RequestAutoAdjust() => _sender.SendMessage(MessageFactory.RequestAutoAdjust());

        //Unity側でウィンドウを表示するとき、最前面と透過を無効にする必要があるため、その準備にあたる処理を行います。
        private void PrepareShowUiOnUnity()
        {
            _windowTransparentBeforeLoadProcess = _setting.Window.IsTransparent.Value;
            _setting.Window.IsTransparent.Value = false;
        }

        //Unity側でのUI表示が終わったとき、最前面と透過の設定をもとの状態に戻します。
        private void EndShowUiOnUnity()
        {
            if (_windowTransparentBeforeLoadProcess != null)
            {
                _setting.Window.IsTransparent.Value = _windowTransparentBeforeLoadProcess.Value;
                _windowTransparentBeforeLoadProcess = null;
            }
        }

        private void RefreshCts()
        {
            _ctsForDetectVrm10?.Cancel();
            _ctsForDetectVrm10?.Dispose();
            _ctsForDetectVrm10 = null;
        }
    }
}
