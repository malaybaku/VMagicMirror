using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// MediaPipeの指FK情報をストアするクラス。
    /// </summary>
    public class MPHandFinger
    {
        public MPHandFinger(
            FingerController fingerController, Vector3[] leftHandPoints, Vector3[] rightHandPoints
            )
        {
            _fingerController = fingerController;
            _leftHandPoints = leftHandPoints;
            _rightHandPoints = rightHandPoints;
        }
        
        private readonly FingerController _fingerController;

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

        //第3関節の開き/閉じはBendとは別軸で保持する
        private readonly float[] _rightOpenAngles = new float[5];
        private readonly float[] _leftOpenAngles = new float[5];
        
        //TODO: 親指の曲げ反映をちゃんと書いてね

        private Vector3 _leftWristForward = Vector3.forward;
        private Vector3 _leftWristUp = Vector3.up;
        private Vector3 _rightWristForward = Vector3.forward;
        private Vector3 _rightWristUp = Vector3.up;

        public void UpdateLeft(Vector3 wristForward, Vector3 wristUp)
        {
            _leftWristForward = wristForward;
            _leftWristUp = wristUp;
            
            UpdateLeftThumb();
            UpdateNonThumbLeftFinger(5, 1);
            UpdateNonThumbLeftFinger(9, 2);
            UpdateNonThumbLeftFinger(13, 3);
            UpdateNonThumbLeftFinger(17, 4);

            _fingerController.Hold(FingerConsts.LeftThumb, GetFingerBendAngle(FingerConsts.LeftThumb));
            for (int i = FingerConsts.LeftIndex; i < FingerConsts.LeftLittle + 1; i++)
            {
                _fingerController.Hold(i, GetFingerBendAngle(i));
                _fingerController.HoldOpen(i, _leftOpenAngles[i]);
            }
        }

        public void UpdateRight(Vector3 wristForward, Vector3 wristUp)
        {
            _rightWristForward = wristForward;
            _rightWristUp = wristUp;

            UpdateRightThumb();
            UpdateNonThumbRightFinger(5, 1);
            UpdateNonThumbRightFinger(9, 2);
            UpdateNonThumbRightFinger(13, 3);
            UpdateNonThumbRightFinger(17, 4);

            _fingerController.Hold(FingerConsts.RightThumb, GetFingerBendAngle(FingerConsts.RightThumb));
            for (int i = FingerConsts.RightIndex; i < FingerConsts.RightLittle + 1; i++)
            {
                _fingerController.Hold(i, GetFingerBendAngle(i));
                _fingerController.HoldOpen(i, _rightOpenAngles[i - 5]);
            }
        }

        /// <summary>
        /// 左手の指の曲げ情報をリセットします。手のステートが変化する時に使います。
        /// </summary>
        public void ReleaseLeftHand()
        {
            for (int i = FingerConsts.LeftThumb; i < FingerConsts.LeftLittle + 1; i++)
            {
                _fingerController.Release(i);
                _fingerController.ReleaseOpen(i);
            }
        }
        
        /// <summary>
        /// 右手の指の曲げ情報をリセットします。手のステートが変化する時に使います。
        /// </summary>
        public void ReleaseRightHand()
        {
            for (int i = FingerConsts.RightThumb; i < FingerConsts.RightLittle + 1; i++)
            {
                _fingerController.Release(i);
                _fingerController.ReleaseOpen(i);
            }
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

        private void UpdateLeftThumb()
        {
            int i = 1;
            
            //NOTE: 0とは手首のことなので、指の種類には依存しない
            var v0 = (_leftHandPoints[i] - _leftHandPoints[0]).normalized;
            var v1 = (_leftHandPoints[i + 1] - _leftHandPoints[i]).normalized;
            var v2 = (_leftHandPoints[i + 2] - _leftHandPoints[i + 1]).normalized;
            var v3 = (_leftHandPoints[i + 3] - _leftHandPoints[i + 2]).normalized;

            //めんどくさいので全てをcos類似度で取ってみる
            _leftBendAngles[0][0] = GetBendAngle(v0, v1);
            _leftBendAngles[0][1] = GetBendAngle(v1, v2);
            _leftBendAngles[0][2] = GetBendAngle(v2, v3);
        }

        private void UpdateRightThumb()
        {
            int i = 1;
            
            //NOTE: 0とは手首のことなので、指の種類には依存しない
            var v0 = (_rightHandPoints[i] - _rightHandPoints[0]).normalized;
            var v1 = (_rightHandPoints[i + 1] - _rightHandPoints[i]).normalized;
            var v2 = (_rightHandPoints[i + 2] - _rightHandPoints[i + 1]).normalized;
            var v3 = (_rightHandPoints[i + 3] - _rightHandPoints[i + 2]).normalized;

            //めんどくさいので全てをcos類似度で取ってみる
            _rightBendAngles[0][0] = GetBendAngle(v0, v1);
            _rightBendAngles[0][1] = GetBendAngle(v1, v2);
            _rightBendAngles[0][2] = GetBendAngle(v2, v3);
        }

        //親指以外の左指先の曲げをナイーブに適用するやつ。指の広げ方向には何もしないことに注意。
        private void UpdateNonThumbLeftFinger(int fingerRootIndex, int fingerNumber)
        {
            int i = fingerRootIndex;
            var v1 = (_leftHandPoints[i + 1] - _leftHandPoints[i]).normalized;
            var v2 = (_leftHandPoints[i + 2] - _leftHandPoints[i + 1]).normalized;
            var v3 = (_leftHandPoints[i + 3] - _leftHandPoints[i + 2]).normalized;

            //第1/第2関節: 関節が1DoFしかないため、ベクトルのコサイン類似度だけで特定可能
            var bendAngle1 = _leftBendAngles[fingerNumber][1] = GetBendAngle(v1, v2);
            var bendAngle2 = _leftBendAngles[fingerNumber][2] = GetBendAngle(v2, v3);

            //第3関節: 2DoFあるので注意
            // - 曲げ: 手のひらの平面から指が出ていく成分方向だけ抽出
            // - 開閉: 指のベクトルを手のひらの平面に射影したあと、外積を計算するとsin値が拾える
            var bendAngle0 = _leftBendAngles[fingerNumber][0] = Mathf.Clamp(
                Mathf.Asin(Vector3.Dot(v1, -_leftWristUp)) * Mathf.Rad2Deg,
                0f,
                90f
            );

            //NOTE: 指がある程度ピンとしてる場合のみ、パー/チョップの検出をやる
            float openAngle = 0f;
            if (bendAngle0 + bendAngle1 + bendAngle2 < 120f)
            {
                var v1Horizontal = (v1 - _leftWristUp * Vector3.Dot(v1, _leftWristUp)).normalized;
                var cross = Vector3.Cross(v1Horizontal, _leftWristForward);
                var sign = -Mathf.Sign(Vector3.Dot(cross, _leftWristUp));
                //TODO: このangleが案外デカく出てしまう…
                openAngle = Mathf.Asin(sign * cross.magnitude) * Mathf.Rad2Deg;
            }

            //NOTE: デカすぎるかもしれないが、デバッグ中は大きめの値にしておく
            _leftOpenAngles[fingerNumber] = Mathf.Clamp(
                openAngle,
                -30f,
                30f
            );
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
            
            var bendAngle1 = _rightBendAngles[fingerNumber][1] = GetBendAngle(v1, v2);
            var bendAngle2 = _rightBendAngles[fingerNumber][2] = GetBendAngle(v2, v3);
            
            //第3関節: 2DoFあるので注意
            // - 曲げ: 手のひらの平面から指が出ていく成分方向だけ抽出
            // - 開閉: 指のベクトルを手のひらの平面に射影したあと、外積を計算するとsin値が拾える
            var bendAngle0 = _rightBendAngles[fingerNumber][0] = Mathf.Clamp(
                Mathf.Asin(Vector3.Dot(v1, -_rightWristUp)) * Mathf.Rad2Deg,
                0f,
                90f
            );

            //NOTE: 指がある程度ピンとしてる場合のみ、パー/チョップの検出をやる
            float openAngle = 0f;
            if (bendAngle0 + bendAngle1 + bendAngle2 < 120f)
            {
                var v1Horizontal = (v1 - _rightWristUp * Vector3.Dot(v1, _rightWristUp)).normalized;
                var cross = Vector3.Cross(v1Horizontal, _rightWristForward);
                var sign = -Mathf.Sign(Vector3.Dot(cross, _rightWristUp));
                //TODO: このangleが案外デカく出てしまう…
                openAngle = Mathf.Asin(sign * cross.magnitude) * Mathf.Rad2Deg;
            }

            //NOTE: デカすぎるかもしれないが、デバッグ中は大きめの値にしておく
            _rightOpenAngles[fingerNumber] = Mathf.Clamp(
                openAngle,
                -30f,
                30f
            );
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
