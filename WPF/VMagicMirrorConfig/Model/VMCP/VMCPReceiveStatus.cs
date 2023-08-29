using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    public class VMCPReceiveStatus
    {
        private readonly bool[] _connected = new bool[3];
        public IReadOnlyList<bool> Connected => _connected;

        /// <summary>
        /// NOTE: 戻り値は<see cref="Connected"/>の内容に変化があったかどうかで決まる。変化があるとtrueを返す
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public bool ApplySerializedStatus(string json)
        {
            try
            {
                using var sr = new StringReader(json);
                using var jr = new JsonTextReader(sr);
                var serializer = new JsonSerializer();
                var rawData = serializer.Deserialize<SerializedVMCPReceiveStatus>(jr);
                if (rawData != null &&
                    rawData.Connected != null && 
                    rawData.Connected.Length >= 3)
                {
                    var src = rawData.Connected;
                    var changed = _connected[0] != src[0] || _connected[1] != src[1] || _connected[2] != src[2];

                    _connected[0] = src[0];
                    _connected[1] = src[1];
                    _connected[2] = src[2];
                    return changed;
                }
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
            return false;
        }
    }

    public class SerializedVMCPReceiveStatus
    {
        public bool[]? Connected { get; set; }
    }
}
