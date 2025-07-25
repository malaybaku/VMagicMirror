﻿using Baku.VMagicMirror.ExternalTracker;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 顔の制御について現在誰が偉い、誰は動いてない、とかGUIでの設定値はどうなってる、といった情報を束ねるクラス。
    /// </summary>
    public class FaceControlConfiguration 
    {
        #region GUIの設定だけで確定する値

        /// <summary>
        /// 顔のトラッキング全体にかんする制御モードを取得、設定します。
        /// </summary>
        /// <remarks>
        /// setterを呼んでいいのは適切なメッセージをIPCで受信しているクラスだけです。
        /// </remarks>
        public FaceControlModes ControlMode => _faceControlMode.Value;

        private readonly ReactiveProperty<FaceControlModes> _faceControlMode 
            = new(FaceControlModes.WebCamLowPower);
        public IReadOnlyReactiveProperty<FaceControlModes> FaceControlMode => _faceControlMode;

        public void SetFaceControlMode(FaceControlModes mode) => _faceControlMode.Value = mode;
        
        /// <summary>
        /// パーフェクトシンクのon/offを取得、設定します。
        /// このフラグがtrueであり、かつ<see cref="ControlMode"/>がExternalTrackerの場合はパーフェクトシンクがオンです。
        /// </summary>
        public bool UseExternalTrackerPerfectSync { get; set; }

        /// <summary>
        /// Webカメラの高負荷モードにおいてパーフェクトシンクを使用するかどうかを取得、設定します。
        /// このフラグがtrueであり、かつ<see cref="ControlMode"/>がWebCamHighPowerの場合はパーフェクトシンクがオンです。
        /// </summary>
        public bool UseWebCamHighPowerModePerfectSync { get; set; }
        
        /// <summary>
        /// VMCPによるBlendShapeの適用が有効かどうかを取得、設定します。
        /// このフラグがtrueの場合、Word To Motion, Face Switchに次いでVMCPのBlendShapeが優先されます。
        /// </summary>
        public bool UseVMCPFacial { get; set; }

        /// <summary>
        /// 外部トラッキング機能またはWebカメラ機能に基づいてパーフェクトシンクを適用する場合はtrue、そうでなければfalse
        /// </summary>
        public bool PerfectSyncActive =>
            (ControlMode is FaceControlModes.ExternalTracker && UseExternalTrackerPerfectSync) ||
            (ControlMode is FaceControlModes.WebCamHighPower && UseWebCamHighPowerModePerfectSync);
        
        #endregion
        
        #region 内部的に特定クラスがsetterを呼ぶ値で、読み取り側は直接使わないでOK

        /// <summary>
        /// FaceSwitchが動作しているかどうかを取得、設定します。
        /// setterを使っていいのは<see cref="FaceSwitchUpdater"/>だけです。
        /// </summary>
        public bool FaceSwitchActive { get; set; }
        
        /// <summary>
        /// 外部トラッカーによるパーフェクトシンクによって、通常と異なる瞬き処理をしているとtrueになります。
        /// trueの場合、瞬き時の目下げ処理はスキップする必要があります。
        /// </summary>
        /// <remarks>
        /// setterを使っていいのは<see cref="ExternalTrackerPerfectSync"/>だけです。
        /// </remarks>
        public bool ShouldStopEyeDownOnBlink { get; set; }
        
        /// <summary>
        /// <see cref="ShouldStopEyeDownOnBlink"/>がtrueのとき、代わりに使うべきLBlinkの値が入ります。
        /// </summary>
        /// <remarks>
        /// setterを使っていいのは<see cref="ExternalTrackerPerfectSync"/>だけです。
        /// </remarks>
        public float AlternativeBlinkL { get; set; }

        /// <summary>
        /// <see cref="ShouldStopEyeDownOnBlink"/>がtrueのとき、代わりに使うべきRBlinkの値が入ります。
        /// </summary>
        /// <remarks>
        /// setterを使っていいのは<see cref="ExternalTrackerPerfectSync"/>だけです。
        /// </remarks>
        public float AlternativeBlinkR { get; set; }

        #endregion
        
        /// <summary> 口以外のブレンドシェイプ操作を一次的に適用停止すべきときtrueになります。 </summary>
        public bool ShouldSkipNonMouthBlendShape => FaceSwitchActive;
    }

    /// <summary>
    /// 顔トラッキングの仕組みとしてどれが有効かの一覧。
    /// </summary>
    public enum FaceControlModes
    {
        /// <summary> 顔トラッキングを行っていません。 </summary>
        None,
        /// <summary> Webカメラで低負荷な顔トラッキングを行っています。 </summary>
        WebCamLowPower,
        /// <summary> Webカメラの高負荷な顔トラッキングを行っています。 </summary>
        WebCamHighPower,
        /// <summary> 外部アプリによる顔トラッキングを行っています。 </summary>
        ExternalTracker,
        /// <summary> VMC Protocolで受信した頭部トラッキング </summary>
        /// <remarks>
        /// この値が指定されていてもBlendShapeは適用してない…というケースが想定されています。
        /// </remarks>
        VMCProtocol,
    }
}
