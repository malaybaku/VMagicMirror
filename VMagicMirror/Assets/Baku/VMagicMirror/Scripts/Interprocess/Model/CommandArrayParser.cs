using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    // NOTE: このクラス、というか「結合したメッセージ」がstring扱いになっているのは歴史的経緯によるもの。
    // - 理想形としてはIPCが根本的にQueueに対応したら本クラスは不要になるため、現状ではイビツな方法で互換性を保っている
    public static class CommandArrayParser
    {
        /// <summary>
        /// contentは「コマンドのbinaryを一つずつbase64エンコードしたstringの配列」であることを期待してる
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static IEnumerable<ReceivedCommand> ParseCommandArray(string content)
        {
            try
            {
                return JsonUtility.FromJson<MessageItemArray>(content)
                    .items
                    .Select(item => new ReceivedCommand(Convert.FromBase64String(item)));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return Enumerable.Empty<ReceivedCommand>();
            }
        }
    }
    
    [Serializable]
    public class MessageItemArray
    {
        public string[] items;
    }
}
