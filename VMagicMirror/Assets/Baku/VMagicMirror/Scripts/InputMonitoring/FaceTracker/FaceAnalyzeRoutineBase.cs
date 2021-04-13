using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 顔トラッキングのメインの処理のうち、画像処理ライブラリにあまり依存しない実装を持つ基底クラスです。
    /// VMagicMirrorの顔検出はマルチスレッド前提なため、そのマルチスレッドの対応をこの基底クラスでやり
    /// 実装は各クラスが持つ、みたいな分担です。
    /// </summary>
    public abstract class FaceAnalyzeRoutineBase
    {
        private CancellationTokenSource _cts;

        //UIスレッドで書き込み、画像処理のスレッドで読み込む
        protected Color32[] _inputColors = null;
        protected int _inputWidth = 0;
        protected int _inputHeight = 0;
        
        /// <summary> 顔検出を行っているスレッド(非UIスレッド)上で、顔情報がアップデートされると発火します。 </summary>
        public event Action<FaceDetectionUpdateStatus> FaceDetectionUpdated;
        
        private readonly object _isActiveLock = new object();
        private bool _isActive = false;
        public bool IsActive
        {
            get { lock(_isActiveLock) return _isActive; }
            set { lock(_isActiveLock) _isActive = value; }
        }

        private readonly object _detectPreparedLock = new object();
        private bool _detectPrepared = false;
        /// <summary>
        /// UIスレッドが<see cref="RequestNextProcess"/>を呼び出しても問題ないかどうかを取得します。
        /// </summary>
        public bool DetectPrepared
        {
            get { lock (_detectPreparedLock) return _detectPrepared; }
            protected set { lock (_detectPreparedLock) _detectPrepared = value; }
        }

        private readonly object _faceDetectCompletedLock = new object();
        private bool _faceDetectCompleted = false;
        /// <summary>
        /// 顔の検出に成功しており、<see cref="GetFaceDetectResult"/>をUIスレッド上で呼び出してほしい状態になっているかどうかを取得します。
        /// 顔が検出出来ない場合、このフラグはfalseになり続けることがあります。
        /// </summary>
        public bool FaceDetectCompleted
        {
            get { lock (_faceDetectCompletedLock) return _faceDetectCompleted; }
            protected set { lock (_faceDetectCompletedLock) _faceDetectCompleted = value; }
        }

        /// <summary> デフォルト挙動であるミラー(左右反転)をやめたい場合にtrueにします。 </summary>
        public bool DisableHorizontalFlip { get; set; }
        
        /// <summary>
        /// 顔検出の結果を取得します。
        /// FaceAnalyzeResultはUIスレッドからのみ読み書きされます。
        /// </summary>
        public abstract IFaceAnalyzeResult Result { get; }

        
        /// <summary>
        /// ソフト起動後に呼び出すことで、必要ならdlibによる顔処理を回すスレッドを起動します。
        /// </summary>
        public virtual void SetUp()
        {
            //顔検出を別スレッドに退避
            _cts = new CancellationTokenSource();
            Task.Run(FaceDetectionRoutine);
        }

        /// <summary>
        /// ソフト終了時に呼び出すことで、起動していたスレッドを停止します。
        /// </summary>
        public virtual void Dispose()
        {
            _cts.Cancel();
        }

        /// <summary>
        /// データが準備できたときに呼び出すことで、画像処理をリクエストします。
        /// 実際の処理が終了すると<see cref="FaceDetectCompleted"/>がtrueになります。
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void RequestNextProcess(Color32[] colors, int width, int height)
        {
            if (!DetectPrepared)
            {
                return;
            }
            
            if (_inputColors == null || _inputColors.Length != colors.Length)
            {
                _inputColors = new Color32[colors.Length];
            }
            
            Array.Copy(colors, _inputColors, colors.Length);
            _inputWidth = width;
            _inputHeight = height;
            DetectPrepared = true;
        }

        /// <summary>
        /// 顔認識ができていない場合に呼び出すことで、<see cref="Result"/>の内容を正面向きの顔にリセットしていきます。
        /// lerpFactorは0-1の範囲の値で、定数にTime.deltaTimeが掛かった値を渡されます。
        /// </summary>
        /// <param name="lerpFactor"></param>
        public abstract void LerpToDefault(float lerpFactor);
        
        protected abstract void RunFaceDetection();

        /// <summary>
        /// キャリブレーションのデータ、および現在のトラッキング情報でキャリブレーションを行うべきかどうかを指定して、
        /// 顔検出の結果を適用します。
        /// 第2引数はGUIでキャリブレーションが指示された後の呼び出しでのみtrueにし、普段はfalseにします。
        /// </summary>
        /// <param name="calibration"></param>
        /// <param name="shouldCalibrate"></param>
        public abstract void ApplyFaceDetectionResult(CalibrationData calibration, bool shouldCalibrate);

        /// <summary>
        /// 顔認識の反復処理が停止したくなったタイミングで呼ばれます。
        /// マルチスレッド側で行っている途中の処理を中断するために用い、マルチスレッド自体は停止しないで下さい。
        /// </summary>
        public virtual void Stop()
        {
        }

        private void FaceDetectionRoutine()
        {
            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(16);
                //書いてある通りだが、以下のケースをガードすることでマルチスレッド特有の問題を防いてます
                // - そもそもルーチンを止めろと言われてる
                // - UIスレッド側からテクスチャが来ない
                // - 出力がすでにあり、UIスレッド上での読み出しを待機中
                if (!IsActive || !DetectPrepared || FaceDetectCompleted)
                {
                    continue;
                }
                
                RunFaceDetection();
            }
        }
        
        protected void RaiseFaceDetectionUpdate(FaceDetectionUpdateStatus status)
        {
            try
            {
                FaceDetectionUpdated?.Invoke(status);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

    }
    
    /// <summary>
    /// 顔トラッキング情報が更新されたとき、ハンドトラッキングで使うための情報を抽出してイベントを発火するのに使うデータです。
    /// </summary>
    public struct FaceDetectionUpdateStatus
    {
        /// <summary> 入力された画像 </summary>
        public Color32[] Image { get; set; }

        /// <summary> 画像の幅 </summary>
        public int Width { get; set; }
        
        /// <summary> 画像の高さ </summary>
        public int Height { get; set; }
        
        /// <summary> 顔が検出できており、<see cref="FaceArea"/>に意味のある顔領域のデータがあるかどうか </summary>
        public bool HasValidFaceArea { get; set; }
        
        /// <summary> <see cref="HasValidFaceArea"/>がtrueの場合、検出した顔領域 </summary>
        public Rect FaceArea { get; set; }
    }
}
