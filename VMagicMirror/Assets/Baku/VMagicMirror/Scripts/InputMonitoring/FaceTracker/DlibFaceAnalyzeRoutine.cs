using System;
using System.Collections.Generic;
using System.Linq;
using DlibFaceLandmarkDetector;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// DlibFaceLandmarkDetectorによって顔検出を行うルーチン
    /// </summary>
    public class DlibFaceAnalyzeRoutine : FaceAnalyzeRoutineBase
    {
        /// <summary>
        /// 学習済みモデルのファイルのパスを指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="predictorFilePath"></param>
        public DlibFaceAnalyzeRoutine(string predictorFilePath)
        {
            _detector = new FaceLandmarkDetector(predictorFilePath);
        }

        private readonly FaceLandmarkDetector _detector;
        private Color32[] _dlibInputColors = null;

        private readonly FaceParts _faceParts = new FaceParts();
        public override IFaceAnalyzeResult Result => _faceParts;

        //Dlibの呼び出しスレッドが書き込み、UIスレッドが読み込む
        private Rect _mainPersonRect;
        private List<Vector2> _mainPersonLandmarks = null;

        public override void ApplyFaceDetectionResult(CalibrationData calibration, bool shouldCalibrate)
        {
            var mainPersonRect = _mainPersonRect;
            //特徴点リストは参照ごと受け取ってOK(Completedフラグが降りるまでは競合しない)
            var landmarks = _mainPersonLandmarks;
            _mainPersonLandmarks = null;

            //出力を拾い終わった時点で次の処理に入ってもらって大丈夫
            FaceDetectCompleted = false;

            //処理を停止したくなった後から結果を受け取った場合、いろいろと無視
            if (!IsActive)
            {
                return;
            }

            var faceRect = new Rect(
                mainPersonRect.xMin / _inputWidth - 0.5f,
                -(mainPersonRect.yMax - _inputHeight * 0.5f) / _inputWidth,
                mainPersonRect.width / _inputWidth,
                mainPersonRect.height / _inputWidth
            );
            
            _faceParts.Update(faceRect, landmarks, calibration, shouldCalibrate);
        }

        public override void LerpToDefault(float lerpFactor) => _faceParts.LerpToDefault(lerpFactor);

        protected override void RunFaceDetection()
        {
            int width = _inputWidth;
            int height = _inputHeight;
            if (_dlibInputColors == null || _dlibInputColors.Length != _inputColors.Length)
            {
                _dlibInputColors = new Color32[_inputColors.Length];
            }

            Array.Copy(_inputColors, _dlibInputColors, _inputColors.Length);

            //この時点で入力データを抜き終わっているので、次のデータがあればセットしても大丈夫
            DetectPrepared = false;

            _detector.SetImage(_dlibInputColors, width, height, 4, true);

            //TODO: 検出途中で「やっぱりストップ」って言われたときの処理の安全性を高めておきたい

            List<Rect> faceRects = _detector.Detect();
            //顔が取れないのでCompletedフラグは立てないままでOK
            if (faceRects == null || faceRects.Count == 0)
            {
                RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
                {
                    Image = _dlibInputColors,
                    Width = width,
                    Height = height,
                    HasValidFaceArea = false,
                });
                return;
            }

            Rect mainPersonRect = faceRects[0];
            //通常来ないが、複数人居たらいちばん大きく映っている人を採用
            if (faceRects.Count > 1)
            {
                mainPersonRect = faceRects
                    .OrderByDescending(r => r.width * r.height)
                    .First();
            }

            _mainPersonRect = mainPersonRect;
            _mainPersonLandmarks = _detector.DetectLandmark(mainPersonRect);

            RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
            {
                Image = _dlibInputColors,
                Width = width,
                Height = height,
                HasValidFaceArea = true,
                FaceArea = mainPersonRect,
            });

            FaceDetectCompleted = true;
        }
    }
}