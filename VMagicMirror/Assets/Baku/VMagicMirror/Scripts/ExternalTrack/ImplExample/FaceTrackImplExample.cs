using System;
using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker.ImplExample
{
    /// <summary> トラッキング実装のサンプル…だったんだけど残骸化したやつ </summary>
    public class FaceTrackImplExample : ExternalTrackSourceProvider
    {
        public override void StartReceive()
        {
        }

        public override void StopReceive()
        {
        }
        
        private bool _hasOutputError = false;
        private readonly RecordFaceTrackSource _faceTrackSource = new RecordFaceTrackSource();
        public override IFaceTrackSource FaceTrackSource => _faceTrackSource;
        public override Quaternion HeadRotation => Quaternion.identity;

        //NOTE: ここで
        public void ReceiveData(string message)
        {
            try
            {
                var data = JsonUtility.FromJson<FaceTrackData>(message);
                AssignTrackData(data);
            }
            catch (Exception ex)
            {
                if (!_hasOutputError)
                {
                    _hasOutputError = true;
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        private void AssignTrackData(FaceTrackData data)
        {
            //TODO: dataをfaceTrackSourceに反映する
        }
        
        private static float Decode(int value)
        {
            if (value == -1)
            {
                value = 0;
            }

            return Mathf.Clamp01(value * BlendShapeDecodeFactor);
        }
        
        private const float BlendShapeDecodeFactor = 1.0f / 65535f;
    }
}
