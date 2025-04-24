using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    class LayoutSettingModel : SettingModelBase<LayoutSetting>
    {
        public LayoutSettingModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>()
            )
        {
        }

        public LayoutSettingModel(IMessageSender sender, IMessageReceiver receiver) : base(sender)
        {
            var s = LayoutSetting.Default;

            CameraFov = new RProperty<int>(s.CameraFov, i => SendMessage(MessageFactory.CameraFov(i)));
            EnableMidiRead = new RProperty<bool>(
                s.EnableMidiRead, b => SendMessage(MessageFactory.EnableMidiRead(b))
                );
            MidiControllerVisibility = new RProperty<bool>(
                s.MidiControllerVisibility, b => SendMessage(MessageFactory.MidiControllerVisibility(b))
                );

            CameraPosition = new RProperty<string>(s.CameraPosition, v => SendMessage(MessageFactory.SetCustomCameraPosition(v)));
            DeviceLayout = new RProperty<string>(s.DeviceLayout, v => SendMessage(MessageFactory.SetDeviceLayout(v)));

            //NOTE: ここは初期値が空なのであんまり深い意味はない。
            QuickSave1 = new RProperty<string>(s.QuickSave1);
            QuickSave2 = new RProperty<string>(s.QuickSave2);
            QuickSave3 = new RProperty<string>(s.QuickSave3);

            HidVisibility = new RProperty<bool>(s.HidVisibility, b => SendMessage(MessageFactory.HidVisibility(b)));
            PenVisibility = new RProperty<bool>(s.PenVisibility, b => SendMessage(MessageFactory.SetPenVisibility(b)));
            SelectedTypingEffectId = new RProperty<int>(s.SelectedTypingEffectId, i => SendMessage(MessageFactory.SetKeyboardTypingEffectType(i)));

            HideUnusedDevices = new RProperty<bool>(s.HideUnusedDevices, b => SendMessage(MessageFactory.HideUnusedDevices(b)));

            EnableFreeCameraMode = new RProperty<bool>(false, b => OnEnableFreeCameraModeChanged(b));
            EnableDeviceFreeLayout = new RProperty<bool>(false, v => SendMessage(MessageFactory.EnableDeviceFreeLayout(v)));

            receiver.ReceivedCommand += OnReceiveCommand;
        }

        //NOTE: Gamepadはモデルクラス的には関連づけしないでおく: 代わりにSave/Loadの関数内で調整してもらう感じにする

        public RProperty<int> CameraFov { get; }
        public RProperty<bool> EnableMidiRead { get; }
        public RProperty<bool> MidiControllerVisibility { get; }

        public RProperty<string> CameraPosition { get; }
        public RProperty<string> DeviceLayout { get; }

        //NOTE: この辺にカメラ、フリーレイアウトのフラグも用意した方がいいかも。揮発フラグだがViewModelで保持するのも違和感あるため。
        public RProperty<string> QuickSave1 { get; }
        public RProperty<string> QuickSave2 { get; }
        public RProperty<string> QuickSave3 { get; }

        public RProperty<bool> HidVisibility { get; }
        public RProperty<bool> PenVisibility { get; }
        public RProperty<int> SelectedTypingEffectId { get; }

        public RProperty<bool> HideUnusedDevices { get; }

        //NOTE: この2つの値はファイルには保存しない
        public RProperty<bool> EnableFreeCameraMode { get; }
        public RProperty<bool> EnableDeviceFreeLayout { get; }

        #region API

        public void RequestResetCameraPosition()
            => SendMessage(MessageFactory.ResetCameraPosition());

        public async Task QuickSaveViewPoint(string? index)
        {
            if (!(int.TryParse(index, out int i) && i > 0 && i <= 3))
            {
                return;
            }

            try
            {
                string res = await SendQueryAsync(MessageFactory.CurrentCameraPosition());
                string saveData = new JObject()
                {
                    ["fov"] = CameraFov.Value,
                    ["pos"] = res,
                }.ToString(Formatting.None);

                switch (i)
                {
                    case 1:
                        QuickSave1.Value = saveData;
                        break;
                    case 2:
                        QuickSave2.Value = saveData;
                        break;
                    case 3:
                        QuickSave3.Value = saveData;
                        break;
                    default:
                        //NOTE: ここは来ない
                        break;
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public void QuickLoadViewPoint(string? index)
        {
            if (!(int.TryParse(index, out int i) && i > 0 && i <= 3))
            {
                return;
            }
            QuickLoadViewPoint(i);
        }

        public void QuickLoadViewPoint(int index)
        {
            try
            {
                string saveData =
                    (index == 1) ? QuickSave1.Value :
                    (index == 2) ? QuickSave2.Value :
                    QuickSave3.Value;

                if (string.IsNullOrEmpty(saveData))
                {
                    return;
                }

                var obj = JObject.Parse(saveData);
                string cameraPos = (string?)obj["pos"] ?? "";
                int fov = (int)(obj["fov"] ?? new JValue(40));

                CameraFov.Value = fov;
                //NOTE: CameraPositionには書き込まない。
                //CameraPositionへの書き込みはCameraPositionCheckerのポーリングに任せとけばOK 
                SendMessage(MessageFactory.QuickLoadViewPoint(cameraPos));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        #endregion

        private async void OnEnableFreeCameraModeChanged(bool value)
        {
            SendMessage(MessageFactory.EnableFreeCameraMode(EnableFreeCameraMode.Value));
            //トグルさげた場合: 切った時点のカメラポジションを取得、保存する。
            //NOTE: フリーレイアウトの終了時にも同じ処理をすることが考えられるが、
            //まあCameraPositionCheckerも別で走っているので、そこまではケアしないことにする。
            if (!value)
            {
                string response = await SendQueryAsync(MessageFactory.CurrentCameraPosition());
                if (!string.IsNullOrWhiteSpace(response))
                {
                    CameraPosition.SilentSet(response);
                }
            }
        }

        #region Reset API

        public void ResetCameraSetting()
        {
            var setting = LayoutSetting.Default;
            //NOTE: フリーカメラモードについては、もともと揮発性の設定にしているのでココでは触らない
            CameraFov.Value = setting.CameraFov;
            QuickSave1.Value = setting.QuickSave1;
            QuickSave2.Value = setting.QuickSave2;
            QuickSave3.Value = setting.QuickSave3;

            //NOTE: カメラ位置を戻すようUnityにメッセージ投げる必要もある: この後にリセットされた値を拾うのはポーリングでいい
            SendMessage(MessageFactory.ResetCameraPosition());
        }

        /// <summary>
        /// NOTE: 設定ファイルは直ちには書き換わらない。この関数を呼び出すとUnity側がよろしくレイアウトを直し、
        /// そのあと直したレイアウト情報を別途投げ返してくる
        /// </summary>
        public void ResetDeviceLayout() => SendMessage(MessageFactory.ResetDeviceLayout());

        public void ResetHidSetting()
        {
            var setting = LayoutSetting.Default;
            HidVisibility.Value = setting.HidVisibility;
            PenVisibility.Value = setting.PenVisibility;
            MidiControllerVisibility.Value = setting.MidiControllerVisibility;
            //NOTE: ここにGamepadのvisibilityも載ってたけど消した。必要なら書かないといけない
            SelectedTypingEffectId.Value = setting.SelectedTypingEffectId;
            HideUnusedDevices.Value = setting.HideUnusedDevices;
        }

        public void ResetMidiSetting()
        {
            var setting = LayoutSetting.Default;
            EnableMidiRead.Value = setting.EnableMidiRead;
            MidiControllerVisibility.Value = setting.MidiControllerVisibility;
        }

        public override void ResetToDefault()
        {
            ResetHidSetting();
            ResetMidiSetting();
            ResetCameraSetting();
        }

        #endregion

        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command == ReceiveMessageNames.UpdateDeviceLayout)
            {
                //NOTE: Unity側から来た値なため、送り返さないでよいことに注意
                DeviceLayout.SilentSet(e.Args);
            }
        }
    }
}
