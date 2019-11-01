using System;
using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="WordToMotionManager"/>の設定用メッセージのハンドラ
    /// </summary>
    public class WordToMotionManagerReceiver : MonoBehaviour
    {
        //Word to Motionの専用入力に使うデバイスを指定する定数値
        private const int DeviceTypeNone = 0;
        private const int DeviceTypeGamepad = 1;
        private const int DeviceTypeKeyboard = 2;

        [Inject] private ReceivedMessageHandler _handler = null;
        [Inject] private RawInputChecker _rawInputChecker = null;
        [SerializeField] private WordToMotionManager manager = null;

        void Start()
        {
            _rawInputChecker.PressedKeys.Subscribe(info => manager.ReceiveKeyDown(info));
            
            _handler.Commands.Subscribe(message =>
            {
                switch(message.Command)
                {
                    case MessageCommandNames.EnableWordToMotion:
                        manager.EnableReadKey = message.ToBoolean();
                        break;
                    case MessageCommandNames.ReloadMotionRequests:
                        ReloadMotionRequests(message.Content);
                        break;
                    case MessageCommandNames.PlayWordToMotionItem:
                        PlayWordToMotionItem(message.Content);
                        break;
                    case MessageCommandNames.EnableWordToMotionPreview:
                        manager.EnablePreview = message.ToBoolean();
                        break;
                    case MessageCommandNames.SendWordToMotionPreviewInfo:
                        ReceiveWordToMotionPreviewInfo(message.Content);
                        break;
                    case MessageCommandNames.EnableHidArmMotion:
                        //腕アニメーションが無効なとき、アニメーションの終了処理をちょっと切り替える
                        manager.ShouldSetDefaultClipAfterMotion = !message.ToBoolean();
                        break;
                    case MessageCommandNames.SetDeviceTypeToStartWordToMotion:
                        SetWordToMotionInputType(message.ToInt());
                        break;
                    default:
                        break;
                }
            });
            
        }

        private void SetWordToMotionInputType(int deviceType)
        {
            switch (deviceType)
            {
                case DeviceTypeNone:
                    manager.UseGamepadForWordToMotion = false;
                    manager.UseKeyboardForWordToMotion = false;
                    break;
                case DeviceTypeGamepad:
                    manager.UseGamepadForWordToMotion = true;
                    manager.UseKeyboardForWordToMotion = false;
                    break;
                case DeviceTypeKeyboard:
                    manager.UseGamepadForWordToMotion = false;
                    manager.UseKeyboardForWordToMotion = true;
                    break;
                default:
                    break;
            }
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
