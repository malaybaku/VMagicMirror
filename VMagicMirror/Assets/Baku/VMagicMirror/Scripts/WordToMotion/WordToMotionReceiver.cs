using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Word To Motionと関係のあるメッセージのハンドラ
    /// </summary>
    public class WordToMotionReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler _handler = null;

        [SerializeField]
        private WordToMotionController _controller = null;

        void Start()
        {
            _handler?.Commands?.Subscribe(message =>
            {
                switch(message.Command)
                {
                    case MessageCommandNames.KeyDown:
                        ReceiveKeyDown(message.Content);
                        break;
                    case MessageCommandNames.EnableWordToMotion:
                        EnableWordToMotion(message.ToBoolean());
                        break;
                    case MessageCommandNames.ReloadMotionRequests:
                        ReloadMotionRequests(message.Content);
                        break;
                    case MessageCommandNames.PlayWordToMotionItem:
                        PlayWordToMotionItem(message.Content);
                        break;
                    case MessageCommandNames.EnableWordToMotionPreview:
                        EnableWordToMotionPreview(message.ToBoolean());
                        break;
                    case MessageCommandNames.SendWordToMotionPreviewInfo:
                        ReceiveWordToMotionPreviewInfo(message.Content);
                        break;
                    default:
                        break;
                }
            });
        }

        private void EnableWordToMotion(bool v)
        {
            _controller.EnableReadKey = v;
        }

        private void ReloadMotionRequests(string json)
        {
            try
            {
                _controller.LoadItems(
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
                _controller.PlayItem(
                    JsonUtility.FromJson<MotionRequest>(json)
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void EnableWordToMotionPreview(bool v)
        {
            _controller.EnablePreview = v;
        }

        private void ReceiveWordToMotionPreviewInfo(string json)
        {
            try
            {
                _controller.PreviewRequest = JsonUtility.FromJson<MotionRequest>(json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void ReceiveKeyDown(string content)
        {
            _controller.ReceiveKeyDown(content);
        }

    }
}
