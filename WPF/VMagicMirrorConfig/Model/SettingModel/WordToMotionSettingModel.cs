using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    class WordToMotionSettingModel : SettingModelBase<WordToMotionSetting>
    {
        public WordToMotionSettingModel()
            : this(ModelResolver.Instance.Resolve<IMessageSender>(), ModelResolver.Instance.Resolve<IMessageReceiver>())
        {
        }


        public WordToMotionSettingModel(IMessageSender sender, IMessageReceiver receiver) : base(sender)
        {
            var settings = WordToMotionSetting.Default;
            var factory = MessageFactory.Instance;

            _motionRequests = MotionRequestCollection.LoadDefault();
            _midiNoteToMotionMap = MidiNoteToMotionMap.LoadDefault();

            PreviewDataSender = new WordToMotionItemPreviewDataSender(sender);

            SelectedDeviceType = new RProperty<int>(settings.SelectedDeviceType, i => SendMessage(factory.SetDeviceTypeToStartWordToMotion(i)));
            ItemsContentString = new RProperty<string>(settings.ItemsContentString, s => SendMessage(factory.ReloadMotionRequests(s)));
            MidiNoteMapString = new RProperty<string>(settings.MidiNoteMapString, s => SendMessage(factory.LoadMidiNoteToMotionMap(s)));
            EnablePreview = new RProperty<bool>(false, b =>
            {
                SendMessage(factory.EnableWordToMotionPreview(b));
                if (b)
                {
                    PreviewDataSender.Start();
                }
                else
                {
                    PreviewDataSender.End();
                }
            });

            MidiNoteReceiver = new MidiNoteReceiver(receiver);
            //NOTE: このStartは通信とかではないので、すぐ始めちゃってOK
            MidiNoteReceiver.Start();

            //NOTE: この2つの呼び出しにより、必ずデフォルト設定をUnity側に通知する+シリアライズ文字列が空ではなくなる
            SaveMidiNoteMap();
            SaveMotionRequests();
        }

        public override void ResetToDefault()
        {
            //何もしない: ここは複雑すぎるので…
        }

        /// <summary> NOTE: これはSyncというより一方的に情報を受け取るやつ </summary>
        internal MidiNoteReceiver MidiNoteReceiver { get; }

        internal WordToMotionItemPreviewDataSender PreviewDataSender { get; }

        public RProperty<int> SelectedDeviceType { get; }

        //NOTE: setterがpublicなのはLoadの都合なので、普通のコードからは使用しないこと。
        public List<string> ExtraBlendShapeClipNames { get; set; } = new List<string>();

        public RProperty<bool> EnablePreview { get; }

        public RProperty<string> ItemsContentString { get; }

        public RProperty<string> MidiNoteMapString { get; }

        private MotionRequestCollection _motionRequests;
        public MotionRequestCollection MotionRequests
        {
            get => _motionRequests;
            private set
            {
                if (_motionRequests != value)
                {
                    _motionRequests = value;
                    RaisePropertyChanged();
                    MotionRequestsReloaded?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler? MotionRequestsReloaded;

        private MidiNoteToMotionMap _midiNoteToMotionMap;
        public MidiNoteToMotionMap MidiNoteToMotionMap
        {
            get => _midiNoteToMotionMap;
            private set
            {
                if (_midiNoteToMotionMap != value)
                {
                    _midiNoteToMotionMap = value;
                    RaisePropertyChanged();
                    MidiNoteToMotionMapReloaded?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler? MidiNoteToMotionMapReloaded;

        public void RequestSerializeItems()
        {
            SaveMotionRequests();
            SaveMidiNoteMap();
        }

        public void SaveMotionRequests() => ItemsContentString.Value = MotionRequests.ToJson();
        public void SaveMidiNoteMap() => MidiNoteMapString.Value = MidiNoteToMotionMap.ToJson();

        public void RefreshMidiNoteMap(MidiNoteToMotionMap result)
        {
            MidiNoteToMotionMap = result;
            SaveMidiNoteMap();
        }

        /// <summary>
        /// 指定したモーションを実行します。再生ボタンを押したときに呼び出す想定です
        /// </summary>
        /// <param name="item"></param>
        public void Play(MotionRequest item)
            => SendMessage(MessageFactory.Instance.PlayWordToMotionItem(item.ToJson()));


        public void Play(int index)
        {
            if (index >= 0 && index < MotionRequests.Requests.Length)
            {
                Play(MotionRequests.Requests[index]);
            }
        }

        #region アイテムの並べ替えと削除

        public void MoveUpItem(MotionRequest item)
        {
            if (!MotionRequests.Requests.Contains(item)) { return; }

            var requests = MotionRequests.Requests.ToList();
            int index = requests.IndexOf(item);
            if (index > 0)
            {
                requests.RemoveAt(index);
                requests.Insert(index - 1, item);
                MotionRequests = new MotionRequestCollection(requests.ToArray());
                SaveMotionRequests();
            }
        }

        public void MoveDownItem(MotionRequest item)
        {
            if (!MotionRequests.Requests.Contains(item)) { return; }

            var requests = MotionRequests.Requests.ToList();
            int index = requests.IndexOf(item);
            if (index < requests.Count - 1)
            {
                requests.RemoveAt(index);
                requests.Insert(index + 1, item);
                MotionRequests = new MotionRequestCollection(requests.ToArray());
                SaveMotionRequests();
            }
        }

        public async Task DeleteItem(MotionRequest item)
        {
            if (!MotionRequests.Requests.Contains(item)) { return; }

            var indication = MessageIndication.DeleteWordToMotionItem();
            bool res = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                string.Format(indication.Content, item.Word),
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (res)
            {
                var requests = MotionRequests.Requests.ToList();
                //ダイアログ表示を挟んでいるので再チェック
                if (requests.Contains(item))
                {
                    requests.Remove(item);
                    MotionRequests = new MotionRequestCollection(requests.ToArray());
                    SaveMotionRequests();
                }
            }
        }

        public void AddNewItem()
        {
            var request = MotionRequests.Requests.ToList();
            request.Add(MotionRequest.GetDefault());
            MotionRequests = new MotionRequestCollection(request.ToArray());
            SaveMotionRequests();
        }

        public void LoadDefaultMotionRequests(List<string> extraBlendShapeClipNames)
        {
            var items = MotionRequest.GetDefaultMotionRequestSet();
            for (int i = 0; i < items.Length; i++)
            {
                foreach (var extraClip in extraBlendShapeClipNames)
                {
                    items[i].ExtraBlendShapeValues.Add(new BlendShapePairItem()
                    {
                        Name = extraClip,
                        Value = 0,
                    });
                }
            }
            MotionRequests = new MotionRequestCollection(items);
            SaveMotionRequests();
        }

        #endregion

        protected override void PreSave()
        {
            RequestSerializeItems();
        }

        protected override void AfterLoad(WordToMotionSetting entity)
        {
            LoadMotionRequests();
            LoadMidiNoteToMotionMap();
        }

        private void LoadMotionRequests()
        {
            if (string.IsNullOrEmpty(ItemsContentString.Value))
            {
                //TODO: こういう時ってExtraBlendShapeClip渡す方がいいのかな
                MotionRequests = MotionRequestCollection.LoadDefault();
                return;
            }

            try
            {
                using (var reader = new StringReader(ItemsContentString.Value))
                {
                    MotionRequests = MotionRequestCollection.FromJson(reader);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                MotionRequests = MotionRequestCollection.LoadDefault();
            }

        }

        private void LoadMidiNoteToMotionMap()
        {
            if (string.IsNullOrEmpty(MidiNoteMapString.Value))
            {
                MidiNoteToMotionMap = MidiNoteToMotionMap.LoadDefault();
                return;
            }

            try
            {
                using (var reader = new StringReader(MidiNoteMapString.Value))
                {
                    MidiNoteToMotionMap = MidiNoteToMotionMap.DeserializeFromJson(reader);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                MidiNoteToMotionMap = MidiNoteToMotionMap.LoadDefault();
            }
        }

        public void RequireMidiNoteOnMessage(bool require) 
            => SendMessage(MessageFactory.Instance.RequireMidiNoteOnMessage(require));
        
        public void RequestCustomMotionDoctor()
            => SendMessage(MessageFactory.Instance.RequestCustomMotionDoctor());
    }
}
