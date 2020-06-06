using System;
using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker
{
    public abstract class ExternalTrackSourceProvider : MonoBehaviour, IExternalTrackSourceProvider
    {
        public abstract void StartReceive();
        public abstract void StopReceive();

        public abstract IFaceTrackSource FaceTrackSource { get; }
        public virtual bool SupportHandTracking => false;
        public virtual bool SupportFacePositionOffset => false;

        /// <summary> 顔トラッキング情報が更新されるとUIスレッド上で発火します。 </summary>
        public event Action<IFaceTrackSource> FaceTrackUpdated;

        protected void RaiseFaceTrackUpdated()
            => FaceTrackUpdated?.Invoke(FaceTrackSource);

        //NOTE: デフォルトは「キャリブレーションをそもそもサポートしてません！」みたいな状態に相当
        public virtual void Calibrate() { }
        public virtual string CalibrationData { get; set; }

        //NOTE: 角度はデータ形式によってやることが変わるため、実装必須でございます。
        public abstract Quaternion HeadRotation { get; }
        
        //NOTE: オフセットはサポートしてない場合zeroのままでいいのでvirtual
        public virtual Vector3 HeadPositionOffset => Vector3.zero;
    }

    public interface IExternalTrackSourceProvider
    {
        /// <summary> データの受信を開始します。 </summary>
        void StartReceive();
        /// <summary> データの受信を停止します。アプリ終了時はインターフェースの使用側からは呼ばれないことに注意して下さい。 </summary>
        void StopReceive();
        
        /// <summary> 現時点での顔トラッキング情報を取得します </summary>
        IFaceTrackSource FaceTrackSource { get; }
        /// <summary> このトラッキングがハンドトラッキングをサポートするかどうかを取得します </summary>
        bool SupportHandTracking { get; }
        /// <summary> このトラッキングが頭部位置のオフセットを取得するかどうかを取得します </summary>
        bool SupportFacePositionOffset { get; }

        /// <summary> 頭の回転について、キャリブレーションのみ行い、スムージングはしていない状態の回転を取得します。 </summary>
        Quaternion HeadRotation { get; }

        /// <summary>
        /// <see cref="SupportFacePositionOffset"/>がtrueの場合、
        /// キャリブレーション位置からのユーザー頭部の移動量を(VMagicMirror内の)ワールド座標系ベクトルとして取得します。
        /// <see cref="SupportFacePositionOffset"/>がfalseの場合、<see cref="Vector3.zero"/>を取得します。
        /// </summary>
        Vector3 HeadPositionOffset { get; }
        
        /// <summary> 顔トラッキング情報が更新されるとUIスレッド上で発火します。 </summary>
        event Action<IFaceTrackSource> FaceTrackUpdated;

        /// <summary> 今持っている顔トラッキング情報を用いてキャリブレーションします。 </summary>
        void Calibrate();
        /// <summary> 文字列としてキャリブレーション情報を取得したり保存したりします。 </summary>
        string CalibrationData { get; set; }
    }
}
