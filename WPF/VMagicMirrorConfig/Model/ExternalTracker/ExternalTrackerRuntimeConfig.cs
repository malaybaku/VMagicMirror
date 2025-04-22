using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    class ExternalTrackerRuntimeConfig
    {
        public ExternalTrackerRuntimeConfig() : this(
            ModelResolver.Instance.Resolve<IMessageReceiver>(),
            ModelResolver.Instance.Resolve<ExternalTrackerSettingModel>()
            )
        {           
        }

        public ExternalTrackerRuntimeConfig(IMessageReceiver receiver, ExternalTrackerSettingModel setting)
        {
            _setting = setting;

            receiver.ReceivedCommand += OnReceiveCommand;

            MissingBlendShapeNames = new RProperty<string>(
                "", _ => UpdateShouldNotifyMissingBlendShapeClipNames()
                );
            _setting.EnableExternalTracking.PropertyChanged +=
                (_, __) => UpdateShouldNotifyMissingBlendShapeClipNames();
        }

        private readonly ExternalTrackerSettingModel _setting;
        private readonly ExternalTrackerBlendShapeNameStore _blendShapeNameStore = new();

        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command == ReceiveMessageNames.ExtraBlendShapeClipNames)
            {
                try
                {
                    //いちおう信頼はするけどIPCだし…みたいな書き方です
                    var names = e.Args
                        .Split(',')
                        .Where(w => !string.IsNullOrEmpty(w))
                        .ToArray();
                    _blendShapeNameStore.Refresh(names);
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
            else if (e.Command == ReceiveMessageNames.ExTrackerCalibrateComplete)
            {
                //キャリブレーション結果を向こうから受け取る: この場合は、ただ覚えてるだけでよい
                _setting.CalibrateData.SilentSet(e.Args);
            }
            else if (e.Command == ReceiveMessageNames.ExTrackerSetPerfectSyncMissedClipNames)
            {
                MissingBlendShapeNames.Value = e.Args;
            }
            else if (e.Command == ReceiveMessageNames.ExTrackerSetIFacialMocapTroubleMessage)
            {
                IFacialMocapTroubleMessage.Value = e.Args;
            }
        }

        public RProperty<string> IFacialMocapTroubleMessage { get; } = new RProperty<string>("");

        public RProperty<bool> ShouldNotifyMissingBlendShapeClipNames { get; } = new RProperty<bool>(false);

        public RProperty<string> MissingBlendShapeNames { get; }

        public ReadOnlyObservableCollection<string> BlendShapeNames => _blendShapeNameStore.BlendShapeNames;

        public void RefreshBlendShapeNames() => _blendShapeNameStore.Refresh(_setting.FaceSwitchSetting);

        private void UpdateShouldNotifyMissingBlendShapeClipNames()
        {
            ShouldNotifyMissingBlendShapeClipNames.Value =
                _setting.EnableExternalTracking.Value &&
                !string.IsNullOrEmpty(MissingBlendShapeNames.Value);
        }
    }


    /// <summary>
    /// ブレンドシェイプクリップ名を一覧保持するクラスです。
    /// </summary>
    public class ExternalTrackerBlendShapeNameStore
    {
        public ExternalTrackerBlendShapeNameStore()
        {
            BlendShapeNames = new ReadOnlyObservableCollection<string>(_blendShapeNames);
            var defaultNames = DefaultBlendShapeNameStore.LoadDefaultNames();
            for (int i = 0; i < defaultNames.Length; i++)
            {
                _blendShapeNames.Add(defaultNames[i]);
            }
        }

        private readonly ObservableCollection<string> _blendShapeNames = new();
        /// <summary> UIに表示するのが妥当と考えられるブレンドシェイプクリップ名の一覧です。 </summary>
        public ReadOnlyObservableCollection<string> BlendShapeNames { get; }

        //Unityで読み込まれたアバターのブレンドシェイプ名の一覧です。
        //NOTE: この値は標準ブレンドシェイプ名を含んでいてもいなくてもOK。ただし現行動作では標準ブレンドシェイプ名は含まない。
        private string[] _avatarClipNames = Array.Empty<string>();

        //設定ファイルから読み込んだ設定で使われていたブレンドシェイプ名の一覧。
        //NOTE: この値に標準ブレンドシェイプ名とそうでないのが混在することがあるが、それはOK
        private string[] _settingUsedNames = Array.Empty<string>();

        /// <summary>
        /// ロードされたVRMの標準以外のブレンドシェイプ名を指定して、名前一覧を更新します。
        /// </summary>
        /// <param name="avatarBlendShapeNames"></param>
        public void Refresh(string[] avatarBlendShapeNames)
        {
            //なんとなく正格評価しておく(値コピーの方が安心なので…
            _avatarClipNames = avatarBlendShapeNames.ToArray();
            RefreshInternal();
        }

        /// <summary>
        /// ファイルからロードされたはずの設定を参照し、その中で使われているブレンドシェイプ名を参考にして名前一覧を更新します。
        /// </summary>
        /// <param name="currentSetting"></param>
        public void Refresh(ExternalTrackerFaceSwitchSetting currentSetting)
        {
            _settingUsedNames = currentSetting.Items
                .Select(i => i.ClipName)
                .ToArray();
            RefreshInternal();
        }

        private void RefreshInternal()
        {
            //理想の並び: デフォルトのやつ一覧、今ロードしたVRMにある名前一覧、(今ロードしたVRMにはないけど)設定で使ってる名前一覧
            var newNames =  DefaultBlendShapeNameStore.LoadDefaultNames().ToList();
            int defaultSetLength = newNames.Count;
            foreach (var nameInModel in _avatarClipNames)
            {
                if (!newNames.Contains(nameInModel))
                {
                    newNames.Add(nameInModel);
                }
            }

            foreach (var nameInSetting in _settingUsedNames)
            {
                if (!newNames.Contains(nameInSetting))
                {
                    newNames.Add(nameInSetting);
                }
            }

            var newNameArray = newNames.ToArray();

            //NOTE: ここポイントで、既存要素は消さないよう慎重に並べ替えます(消すとOC<T>の怒りを買ってUI側の要素選択に悪影響が出たりするので…)
            for (int i = defaultSetLength; i < newNameArray.Length; i++)
            {
                if (_blendShapeNames.Contains(newNameArray[i]))
                {
                    int currentIndex = _blendShapeNames.IndexOf(newNameArray[i]);
                    if (currentIndex != i)
                    {
                        //もう入ってる値だが、場所を入れ替えたいケース
                        _blendShapeNames.Move(currentIndex, i);
                    }
                }
                else
                {
                    //そもそも入ってないケース
                    _blendShapeNames.Insert(i, newNameArray[i]);
                }
            }

            //OC<T>側のほうが配列が長い場合、ハミ出た分は余計なやつなので消しちゃってOK
            while (_blendShapeNames.Count > newNameArray.Length)
            {
                _blendShapeNames.RemoveAt(newNameArray.Length);
            }
        }
    }
}
