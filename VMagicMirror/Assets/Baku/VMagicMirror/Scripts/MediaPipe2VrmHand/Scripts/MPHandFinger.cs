using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// MediaPipeの指FK情報をストアするクラス。
    /// </summary>
    public class MPHandFinger
    {
        public MPHandFinger(Vector3[] leftHandPoints, Vector3[] rightHandPoints)
        {
            _leftHandPoints = leftHandPoints;
            _rightHandPoints = rightHandPoints;
        }

        //TODO: 親指の曲げ反映をちゃんと書いてね
        //NOTE: 人差し指～小指の曲げは割と適用にやっても大丈夫

        public void UpdateLeft()
        {
            UpdateNonThumbLeftFinger(5, 1);
            UpdateNonThumbLeftFinger(9, 2);
            UpdateNonThumbLeftFinger(13, 3);
            UpdateNonThumbLeftFinger(17, 4);
        }

        public void UpdateRight()
        {
            UpdateNonThumbRightFinger(5, 1);
            UpdateNonThumbRightFinger(9, 2);
            UpdateNonThumbRightFinger(13, 3);
            UpdateNonThumbRightFinger(17, 4);
        }
        
        /// <summary>
        /// <see cref="FingerConsts"/>の指番号を指定することで、その指の曲げ角度をdeg単位で取得します。
        /// この値を<see cref="FingerController"/>に渡すことを想定しています。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <returns></returns>
        public float GetFingerBendAngle(int fingerNumber)
        {
            if (fingerNumber < 0 || fingerNumber > FingerConsts.RightLittle)
            {
                return 0;
            }
            
            if (fingerNumber < FingerConsts.RightThumb)
            {
                var angles = _leftBendAngles[fingerNumber];
                return (angles[0] + angles[1] + angles[2]) / 3.0f;
            }
            else
            {
                var angles = _rightBendAngles[fingerNumber - 5];
                return (angles[0] + angles[1] + angles[2]) / 3.0f;
            }
        }

        private readonly Vector3[] _leftHandPoints;
        private readonly Vector3[] _rightHandPoints;
        
        //NOTE: 指ごとの曲げ角度で、指番号[0-4], 関節(0=根本, 2=指先)を指定して取得、設定する。
        //floatでダメな場合、Quaternionの矩形配列に差し替えるかも
        private readonly float[][] _rightBendAngles = new []
        {
            new float[3], new float[3], new float[3], new float[3], new float[3],
        };
        
        private readonly float[][] _leftBendAngles = new []
        {
            new float[3], new float[3], new float[3], new float[3], new float[3],
        };

        //親指以外の左指先の曲げをナイーブに適用するやつ。指の広げ方向には何もしないことに注意。
        private void UpdateNonThumbLeftFinger(int fingerRootIndex, int fingerNumber)
        {
            //NOTE: 0とは手首のことなので、指の種類には依存しない
            int i = fingerRootIndex;
            
            //関節で区切られる指の各セクションの方向ベクトルを拾うことで、曲げ角が分かる
            var v0 = (_leftHandPoints[i] - _leftHandPoints[0]).normalized;
            var v1 = (_leftHandPoints[i + 1] - _leftHandPoints[i]).normalized;
            var v2 = (_leftHandPoints[i + 2] - _leftHandPoints[i + 1]).normalized;
            var v3 = (_leftHandPoints[i + 3] - _leftHandPoints[i + 2]).normalized;
            
            _leftBendAngles[fingerNumber][0] = GetBendAngle(v0, v1);
            _leftBendAngles[fingerNumber][1] = GetBendAngle(v1, v2);
            _leftBendAngles[fingerNumber][2] = GetBendAngle(v2, v3);
        }

        //親指以外の右指先の曲げをナイーブに適用するやつ。指の広げ方向には何もしないことに注意。
        private void UpdateNonThumbRightFinger(int fingerRootIndex, int fingerNumber)
        {
            //NOTE: 0とは手首のことなので、指の種類には依存しない
            int i = fingerRootIndex;
            
            //関節で区切られる指の各セクションの方向ベクトルを拾うことで、曲げ角が分かる
            var v0 = (_rightHandPoints[i] - _rightHandPoints[0]).normalized;
            var v1 = (_rightHandPoints[i + 1] - _rightHandPoints[i]).normalized;
            var v2 = (_rightHandPoints[i + 2] - _rightHandPoints[i + 1]).normalized;
            var v3 = (_rightHandPoints[i + 3] - _rightHandPoints[i + 2]).normalized;
            
            //NOTE: UnityのHumanBodyBonesが指の付け根方向から定義されてる前提で書いてます。
            _rightBendAngles[fingerNumber][0] = GetBendAngle(v0, v1);
            _rightBendAngles[fingerNumber][1] = GetBendAngle(v1, v2);
            _rightBendAngles[fingerNumber][2] = GetBendAngle(v2, v3);
        }

        //コサイン類似度で指の曲げ角度を取得します。指先の第1、第2関節に対して妥当な計算です。
        //第3関節はホントはコレだとダメで、パーとチョップの区別ができません
        private static float GetBendAngle(Vector3 u, Vector3 v) =>
            Mathf.Clamp(
                Mathf.Acos(Vector3.Dot(u, v)) * Mathf.Rad2Deg,
                0f,
                90f
            );
    }
}
