using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="WordToMotionManager"/>の設定用メッセージのハンドラ
    /// </summary>
    public class WordToMotionManagerReceiver : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler = null;
        [SerializeField] private WordToMotionManager manager = null;

        void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch(message.Command)
                {
                    case MessageCommandNames.KeyDown:
                        manager.ReceiveKeyDown(message.Content);
                        break;
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
                    default:
                        break;
                }
            });
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
