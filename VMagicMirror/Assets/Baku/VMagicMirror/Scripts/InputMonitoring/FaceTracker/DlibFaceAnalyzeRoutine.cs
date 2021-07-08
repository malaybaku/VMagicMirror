using System;
using System.Collections.Generic;
using System.IO;
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
        //もとのWebCamTextureの画像が大きいとき、この幅/高さにだいたい揃うよう縮めてから用いる。
        private const int TargetWidth = 320;
        private const int TargetHeight = 240;
    
        /// <summary>
        /// 学習済みモデルのファイルのパスを指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="predictorFilePath"></param>
        public DlibFaceAnalyzeRoutine(string predictorFilePath)
        {
            _detector = new FaceLandmarkDetector(
                Path.Combine(Application.streamingAssetsPath, predictorFilePath)
                );
        }

        private readonly FaceLandmarkDetector _detector;
        private Color32[] _dlibInputColors = null;
        private int _width;
        private int _height;

        private readonly FaceParts _faceParts = new FaceParts();
        public override IFaceAnalyzeResult Result => _faceParts;

        //Dlibの呼び出しスレッドが書き込み、UIスレッドが読み込む
        private Rect _mainPersonRect;
        private List<Vector2> _mainPersonLandmarks = null;

        public override void Start()
        {
            base.Start();
            CanRequestNextProcess = true;
        }

        public override void Stop()
        {
            base.Stop();
            HasResultToApply = false;
        }

        protected override void RunFaceDetection()
        {
            //NOTE: ここで_widthとか_heightが必要に応じて書き換わることに注意
            UpdateInputColors();
            int width = _width;
            int height = _height;
        
            //この時点で入力データを抜き終わっているので、次のデータがあればセットしても大丈夫
            CanRequestNextProcess = true;

            _detector.SetImage(_dlibInputColors, width, height, 4, true);
            List<Rect> faceRects = _detector.Detect();
            //NOTE: 顔の抽出、顔のなかの特徴点抽出の2ステップが特に時間かかるため、これらの直後では
            //「次の処理ってやってもいいの？」というのを確認しておく
            if (!IsActive)
            {
                return;
            }
            
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
            if (!IsActive)
            {
                return;
            }
            
            RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
            {
                Image = _dlibInputColors,
                Width = width,
                Height = height,
                HasValidFaceArea = true,
                FaceArea = mainPersonRect,
            });

            HasResultToApply = true;
        }

        public override void ApplyResult(CalibrationData calibration, bool shouldCalibrate)
        {
            var mainPersonRect = _mainPersonRect;
            //特徴点リストは参照ごと受け取ってOK(Completedフラグが降りるまでは競合しない)
            var landmarks = _mainPersonLandmarks;
            _mainPersonLandmarks = null;

            //出力を拾い終わった時点で次の処理に入ってもらって大丈夫
            HasResultToApply = false;

            //処理を停止したくなった後から結果を受け取った場合、いろいろと無視
            if (!IsActive)
            {
                return;
            }

            int width = _width;
            int height = _height;

            var faceRect = new Rect(
                mainPersonRect.xMin / width - 0.5f,
                (height * 0.5f - mainPersonRect.yMax) / width,
                mainPersonRect.width / width,
                mainPersonRect.height / width
            );
            
            _faceParts.Update(faceRect, landmarks, calibration, shouldCalibrate);
        }

        public override void LerpToDefault(float lerpFactor) => _faceParts.LerpToDefault(lerpFactor);

        private void UpdateInputColors()
        {
            //等倍で使ってもいいパターン(珍しいが)
            if (_inputWidth < TargetWidth * 2 && _inputHeight < TargetHeight * 2)
            {
                _width = _inputWidth;
                _height = _inputHeight;

                if (_dlibInputColors == null || _dlibInputColors.Length != _width * _height)
                {
                    _dlibInputColors = new Color32[_width * _height];
                }
                Array.Copy(_inputColors, _dlibInputColors, _dlibInputColors.Length);
                return;
            }
            
            //NOTE: 基本的には元画像が640x480程度のはずなのを踏まえて、リサイズは1/2のみサポートする。1/4とかは無し。
            int stride = 2;
            _width = _inputWidth / stride;
            _height = _inputHeight / stride;
            if (_dlibInputColors == null || _dlibInputColors.Length != _width * _height)
            {
                _dlibInputColors = new Color32[_width * _height];
            }

            //単にfor文でピクセルを間引く。k-NN相当。
            //マルチスレッドではこれが安全、かつforループの回数が案外少ない(だいたい80000回)ので、
            //GPU-CPU間のやりとりするよりはラク。
            //ただし、stride > 2だと(画像がギザギザになるせいで)顔検出の性能が下がるっぽいので注意。
            int index = 0;
            for (int y = 0; y < _inputHeight; y += stride)
            {
                for (int x = 0; x < _inputWidth; x += stride)
                {
                    _dlibInputColors[index] = _inputColors[y * _inputWidth + x];
                    index++;
                }
            }
        }
    }
}