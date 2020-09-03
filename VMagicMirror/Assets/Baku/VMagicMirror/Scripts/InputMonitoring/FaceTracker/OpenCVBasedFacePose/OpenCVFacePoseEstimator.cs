using System.Collections.Generic;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// DlibFaceLandmarkDetectorによる特徴点入力を
    /// </summary>
    /// <remarks>
    /// CVVTuberExampleをかなり参考にしてますが、
    /// 根本的にはPnP問題(Perspective-n-Points Problem)を解くOpenCVの処理に頼ろう、という話です。
    /// VMagicMirror v1.3.0で使ってた「目と耳の上下関係から頭部ピッチが推定できる」みたいなヒューリスティックを
    /// もっと一般化したのがPnPで、特に顔の凹凸ベースでちゃんと推定するぶん精度がよいのがポイント。のはず。
    /// </remarks>
    public class OpenCVFacePoseEstimator
    {
        //左目、右目、鼻、鼻の下、左耳、右耳の6点の位置情報をこの順で決めてリファレンスにする
        private static readonly MatOfPoint3f _objPoints = new MatOfPoint3f(
            new Point3(-34, 90, 83),
            new Point3(34, 90, 83),
            new Point3(0.0, 50, 117),
            new Point3(0.0, 32, 97),
            new Point3(-79, 90, 10),
            new Point3(79, 90, 10)
        );

        private static readonly MatOfDouble _distCoeffs = new MatOfDouble(0, 0, 0, 0);

        //OpenCV / Unityの座標変換で使いたい2つの行列。画像座標系とか、右手/左手系の問題に関する。
        private static readonly Matrix4x4 _invertYM =
            Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));

        private static readonly Matrix4x4 _invertZM =
            Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));

        //NOTE: この辺は単に「毎回allocしたくないのでオブジェクトキャッシュしよ」系
        private readonly double[] _rVecArray = new double[3];
        private readonly double[] _tVecArray = new double[3];
        private readonly Vector2[] _landmarks = new Vector2[17];
        
        //NOTE: この辺は本質的に維持したいデータ
        private readonly Mat _camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        private readonly MatOfPoint2f _imagePoints = new MatOfPoint2f();

        private int _width = 0;
        private int _height = 0;
        private Matrix4x4 _vp;

        private Mat _rVec;
        private Mat _tVec;
        

        /// <summary> 現在入っているポーズ情報が有効なデータかどうかを取得します。 </summary>
        public bool HasValidPoseData { get; private set; }

        //TODO: Z方向は特に信用ならない値かもしれない…色々な理由で…
        /// <summary> 頭の位置を取得します。 </summary>
        public Vector3 HeadPosition { get; private set; }

        /// <summary> 頭の回転を取得します。 </summary>
        public Quaternion HeadRotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// 画像サイズを指定することで、PnPの計算に必要な行列を初期化します。
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetImageSize(int width, int height)
        {
            if (width != _width || height != _height)
            {
                _width = width;
                _height = height;

                SetCameraMatrix(_camMatrix, width, height);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            _width = 0;
            _height = 0;
            _tVec?.Dispose();
            _tVec = null;
            _rVec?.Dispose();
            _rVec = null;
            HasValidPoseData = false;
        }

        //NOTE: ここList渡しなのがちょっとイヤなんだけど、Dlibのマナーに沿うことを重視
        /// <summary>
        /// 特徴点を受け取って頭部姿勢の推定状況を更新します。
        /// </summary>
        /// <param name="landmarks"></param>
        public void EstimatePose(List<Vector2> landmarks)
        {
            //画像の初期化が終わってない場合は完全NG
            if (_width < 1 || _height < 1)
            {
                return;
            }

            HasValidPoseData = false;

            SetImagePoints(landmarks);

            //初期推定っぽいやつ
            if (_rVec == null || _tVec == null)
            {
                _rVec = new Mat(3, 1, CvType.CV_64FC1);
                _tVec = new Mat(3, 1, CvType.CV_64FC1);
                Calib3d.solvePnP(_objPoints, _imagePoints, _camMatrix, _distCoeffs, _rVec, _tVec);
            }

            float tx = (float) _tVec.get(0, 0)[0];
            float ty = (float) _tVec.get(1, 0)[0];
            float tz = (float) _tVec.get(2, 0)[0];

            bool notInViewport = false;
            var pos = _vp * new Vector4(tx, ty, tz, 1.0f);
            if (Mathf.Abs(pos.w) > 0.0001)
            {
                float x = pos.x / pos.w;
                float y = pos.y / pos.w;
                float z = pos.z / pos.w;
                notInViewport = Mathf.Abs(x) > 1.0f || Mathf.Abs(y) > 1.0f || Mathf.Abs(z) > 1.0f;
            }

            //NOTE: 要するに現状の推定が怪しすぎるならゼロベースで計算をやり直せ、って話
            if (float.IsNaN(tz) || notInViewport)
            {
                Calib3d.solvePnP(_objPoints, _imagePoints, _camMatrix, _distCoeffs, _rVec, _tVec);
            }
            else
            {
                //普通にトラッキングできてればこっちのはず
                Calib3d.solvePnP(
                    _objPoints, _imagePoints, _camMatrix, _distCoeffs, _rVec, _tVec,
                    true, Calib3d.SOLVEPNP_ITERATIVE
                );
            }

            if (notInViewport)
            {
                return;
            }

            // 最終的なt/rの値をUnityで使いたいのでPosition/Rotationに変換
            _rVec.get(0, 0, _rVecArray);
            _tVec.get(0, 0, _tVecArray);
            var poseData = ARUtils.ConvertRvecTvecToPoseData(_rVecArray, _tVecArray);
            
            // 0.001fは[mm]単位から[m]への変換
            // YMのほう: 右手系を左手系にする
            // ZMのほう: 手前/奥をひっくり返す(はず)
            var transformationM = 
                _invertYM *
                Matrix4x4.TRS(0.001f * poseData.pos, poseData.rot, Vector3.one) * 
                _invertZM;

            HeadPosition = ARUtils.ExtractTranslationFromMatrix(ref transformationM);
            HeadRotation = ARUtils.ExtractRotationFromMatrix(ref transformationM);
            HasValidPoseData = true;
        }

        private void SetImagePoints(List<Vector2> landmarks)
        {
            //Listにランダムアクセスしたくないので、配列に全部書き写したのを用いる
            int i = 0;
            foreach (var mark in landmarks)
            {
                _landmarks[i] = mark;
                i++;
            }

            //NOTE: 17点モデルから目、鼻、耳を(_objPointsと同じ対応付けで)取り出す。
            _imagePoints.fromArray (
                new Point ((_landmarks[2].x + _landmarks[3].x) / 2, (_landmarks[2].y + _landmarks[3].y) / 2),
                new Point ((_landmarks[4].x + _landmarks[5].x) / 2, (_landmarks[4].y + _landmarks[5].y) / 2),
                new Point (_landmarks[0].x, _landmarks[0].y),
                new Point (_landmarks[1].x, _landmarks[1].y),
                new Point (_landmarks[6].x, _landmarks[6].y),
                new Point (_landmarks[8].x, _landmarks[8].y)
            );
        }
                
        private void SetCameraMatrix(Mat camMatrix, float width, float height)
        {
            float maxD = Mathf.Max(width, height);
            float fx = maxD;
            float fy = maxD;
            float cx = width / 2.0f;
            float cy = height / 2.0f;
            var array = new double[] {fx, 0, cx, 0, fy, cy, 0, 0, 1.0};
            camMatrix.put(0, 0, array);

            //NOTE: ここのnear/farは[mm]単位のはず、と思って、物理カメラほぼ準拠であろうこの値を使います。
            //Unity内カメラのnear/farとは特に関係ないので注意！
            var p = ARUtils.CalculateProjectionMatrixFromCameraMatrixValues(
                fx, fy, cx, cy, width, height, 1, 3000
            );
            _vp = p * _invertZM;
        }
    }
}