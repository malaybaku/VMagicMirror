using System;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="WordToMotionManager"/>の設定用メッセージのハンドラ
    /// </summary>
    public class WordToMotionManagerReceiver : MonoBehaviour
    {
        //Word to Motionの専用入力に使うデバイスを指定する定数値
        private const int DeviceTypeNone = -1;
        private const int DeviceTypeKeyboardTyping = 0;
        private const int DeviceTypeGamepad = 1;
        private const int DeviceTypeKeyboardTenKey = 2;
        private const int DeviceTypeMidi = 3;

        private RawInputChecker _rawInputChecker = null;
        [SerializeField] private WordToMotionManager manager = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver, RawInputChecker rawInputChecker)
        {
            _rawInputChecker = rawInputChecker;
            receiver.AssignCommandHandler(
                MessageCommandNames.ReloadMotionRequests,
                message => ReloadMotionRequests(message.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.PlayWordToMotionItem,
                message => PlayWordToMotionItem(message.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableWordToMotionPreview,
                message => manager.EnablePreview = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SendWordToMotionPreviewInfo,
                message => ReceiveWordToMotionPreviewInfo(message.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetDeviceTypeToStartWordToMotion,
                message => SetWordToMotionInputType(message.ToInt())
                );
            
            //NOTE: 残骸コードを残しときます。ビルトインモーション後の手の動きがちょっと心配ではあるよね、という話
            
            //NOTE: キーボード/マウスだけ消し、ゲームパッドや画像ハンドトラッキングがある、というケースでは多分無理にいじらないでも大丈夫です。 
            // case MessageCommandNames.EnableHidArmMotion:
            //     //腕アニメーションが無効なとき、アニメーションの終了処理をちょっと切り替える
            //     manager.ShouldSetDefaultClipAfterMotion = !message.ToBoolean();
            //     break;
        }
        
        private void Start()
        {
            _rawInputChecker.PressedKeys.Subscribe(info => manager.ReceiveKeyDown(info));
        }

        private void SetWordToMotionInputType(int deviceType)
        {
            manager.UseKeyboardWordTypingForWordToMotion = (deviceType == DeviceTypeKeyboardTyping);
            manager.UseGamepadForWordToMotion = (deviceType == DeviceTypeGamepad);
            manager.UseKeyboardForWordToMotion = (deviceType == DeviceTypeKeyboardTenKey);
            manager.UseMidiForWordToMotion = (deviceType == DeviceTypeMidi);
        }

        private void ReloadMotionRequests(string json)
        {
            try
            {
                manager.LoadItems(
                    JsonUtility.FromJson<MotionRequestCollection>(json)
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void PlayWordToMotionItem(string json)
        {
            try
            {
                manager.PlayItem(
                    JsonUtility.FromJson<MotionRequest>(json)
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void ReceiveWordToMotionPreviewInfo(string json)
        {
            try
            {
                manager.PreviewRequest = JsonUtility.FromJson<MotionRequest>(json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }
}
