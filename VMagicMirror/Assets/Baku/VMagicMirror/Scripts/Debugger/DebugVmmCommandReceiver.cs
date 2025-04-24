using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class DebugVmmCommandReceiver : PresenterBase
    {
        private readonly IMessageReceiver _receiver;

        [Inject]
        public DebugVmmCommandReceiver(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.DebugSendLargeData,
                command => CheckLargeDataContent(command.GetStringValue())
                );
        }

        private void CheckLargeDataContent(string data)
        {
            Debug.Log($"{nameof(CheckLargeDataContent)}...");
            const int ExpectedDataLength = 26 * 100_000;

            // WPF側から来るデータはテスト用のデータで「a-zの小文字アルファベットを10万回繰り返したもの」であるのが既知なので、
            // 実際にそのデータが欠損なく到達していることを確認する
            if (data.Length != ExpectedDataLength)
            {
                Debug.LogError($"{nameof(CheckLargeDataContent)}, Error: expected data length={ExpectedDataLength}, but actual={data.Length}");
                return;
            }
            
            for (var i = 0; i < ExpectedDataLength; i++)
            {
                if (data[i] != 'a' + (i % 26))
                {
                    Debug.LogError($"{nameof(CheckLargeDataContent)}, Error: data[{i}]={data[i]} is not expected value.");
                    return;
                }
            }
            
            // いちおうデータの冒頭をちょっとだけ見ておく(明らかにヘンなデータじゃないのだけ目視したいので)
            Debug.Log($"{nameof(CheckLargeDataContent)}, received large data seems like... {data[..250]}");
            
            Debug.Log($"{nameof(CheckLargeDataContent)}, received large data has expected content.");
        }
    }
}
