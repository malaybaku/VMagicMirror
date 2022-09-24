using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> <see cref="WordToMotionManager"/>の設定用メッセージのハンドラ </summary>
    public class WordToMotionManagerReceiver
    {
        //Word to Motionの専用入力に使うデバイスを指定する定数値
        private const int DeviceTypeNone = -1;
        private const int DeviceTypeKeyboardTyping = 0;
        private const int DeviceTypeGamepad = 1;
        private const int DeviceTypeKeyboardTenKey = 2;
        private const int DeviceTypeMidi = 3;
        
        public WordToMotionManagerReceiver(IMessageReceiver receiver, WordToMotionManager manager)
        {
            _manager = manager;
            receiver.AssignCommandHandler(
                VmmCommands.ReloadMotionRequests,
                message => ReloadMotionRequests(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.PlayWordToMotionItem,
                message => PlayWordToMotionItem(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableWordToMotionPreview,
                message => _manager.EnablePreview = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.SendWordToMotionPreviewInfo,
                message => ReceiveWordToMotionPreviewInfo(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetDeviceTypeToStartWordToMotion,
                message => SetWordToMotionInputType(message.ToInt())
                );
            
            receiver.AssignQueryHandler(
                VmmQueries.GetAvailableCustomMotionClipNames,
                q =>
                {
                    q.Result = string.Join("\t", _manager.LoadAvailableCustomMotionClipNames());
                    Debug.Log("Get Available CustomMotion Clip Names, result = " + q.Result);
                });
            
            //NOTE: 残骸コードを残しときます。ビルトインモーション後の手の動きがちょっと心配ではあるよね、という話
            
            //NOTE: キーボード/マウスだけ消し、ゲームパッドや画像ハンドトラッキングがある、というケースでは多分無理にいじらないでも大丈夫です。 
            // case MessageCommandNames.EnableHidArmMotion:
            //     //腕アニメーションが無効なとき、アニメーションの終了処理をちょっと切り替える
            //     manager.ShouldSetDefaultClipAfterMotion = !message.ToBoolean();
            //     break;
        }
        
        private readonly WordToMotionManager _manager;

        private void SetWordToMotionInputType(int deviceType)
        {
            _manager.UseKeyboardWordTypingForWordToMotion = (deviceType == DeviceTypeKeyboardTyping);
            _manager.UseGamepadForWordToMotion = (deviceType == DeviceTypeGamepad);
            _manager.UseKeyboardForWordToMotion = (deviceType == DeviceTypeKeyboardTenKey);
            _manager.UseMidiForWordToMotion = (deviceType == DeviceTypeMidi);
        }

        private void ReloadMotionRequests(string json)
        {
            try
            {
                _manager.LoadItems(
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
                _manager.PlayItem(
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
                _manager.PreviewRequest = JsonUtility.FromJson<MotionRequest>(json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }
}
