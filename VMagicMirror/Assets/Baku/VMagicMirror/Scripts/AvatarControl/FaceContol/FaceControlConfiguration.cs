using Baku.VMagicMirror.ExternalTracker;
using R3;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 顔の制御について現在誰が偉い、誰は動いてない、とかGUIでの設定値はどうなってる、といった情報を束ねるクラス。
    /// </summary>
    public class FaceControlConfiguration 
    {
        #region GUIの設定だけで確定する値

        /// <summary>
        /// 頭部の動作を制御している処理の種類を取得します。
        ///
        /// とくにVMCProtocolの利用中は、この値が <see cref="FaceControlModes.VMCProtocol"/> であるものの、
        /// 表情は別で処理をする、つまり <see cref="BlendShapeControlMode"/> の最新値は他の値である…というケースもあることに注意して下さい。
        /// </summary>
        public FaceControlModes HeadMotionControlModeValue => _headMotionControlMode.Value;
        
        private readonly ReactiveProperty<FaceControlModes> _headMotionControlMode = new(FaceControlModes.WebCamLowPower);
        /// <summary>
        /// 頭部動作を制御している処理の種類を取得します。
        /// VMCProtocolについては、この値が<see cref="FaceControlModes.VMCProtocol"/>以外の場合であっても、
        /// <see cref="UseAdditionalVmcpHeadMotion"/>が true の場合は加算的にモーションを適用することがあります。
        /// </summary>
        public ReadOnlyReactiveProperty<FaceControlModes> HeadMotionControlMode => _headMotionControlMode;

        private readonly ReactiveProperty<FaceControlModes> _blendShapeControlMode = new(FaceControlModes.WebCamLowPower);

        /// <summary> ブレンドシェイプを制御している処理の種類を取得します。 </summary>
        public ReadOnlyReactiveProperty<FaceControlModes> BlendShapeControlMode => _blendShapeControlMode;
        
        private readonly ReactiveProperty<bool> _useAdditionalVmcpHeadMotion = new();
        /// <summary> 頭部加算ベースでVMCProtocolの頭部動作を適用するかどうかを取得します </summary>
        public ReadOnlyReactiveProperty<bool> UseAdditionalVmcpHeadMotion => _useAdditionalVmcpHeadMotion;
        
        public void SetFaceControlMode(
            FaceControlModes headMotionMode,
            FaceControlModes blendShapeMode,
            bool useAdditionalVmcpHeadMotion)
        {
            _headMotionControlMode.Value = headMotionMode;
            _blendShapeControlMode.Value = blendShapeMode;
            _useAdditionalVmcpHeadMotion.Value = useAdditionalVmcpHeadMotion;
        }

        /// <summary>
        /// パーフェクトシンクのon/offを取得、設定します。
        /// このフラグがtrueであり、かつ<see cref="HeadMotionControlModeValue"/>がExternalTrackerの場合はパーフェクトシンクがオンです。
        /// </summary>
        public bool UseExternalTrackerPerfectSync { get; set; }

        /// <summary>
        /// Webカメラの高負荷モードにおいてパーフェクトシンクを使用するかどうかを取得、設定します。
        /// このフラグがtrueであり、かつ<see cref="HeadMotionControlModeValue"/>がWebCamHighPowerの場合はパーフェクトシンクがオンです。
        /// </summary>
        public bool UseWebCamHighPowerModePerfectSync { get; set; }

        /// <summary>
        /// 外部トラッキング機能またはWebカメラ機能に基づいてパーフェクトシンクを適用する場合はtrue、そうでなければfalse
        /// </summary>
        public bool PerfectSyncActive =>
            (BlendShapeControlMode.CurrentValue is FaceControlModes.ExternalTracker && UseExternalTrackerPerfectSync) ||
            (BlendShapeControlMode.CurrentValue is FaceControlModes.WebCamHighPower && UseWebCamHighPowerModePerfectSync);
        
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
    /// メインの顔トラッキング挙動がどれになっているかを指す値。
    /// 頭部の6DoFトラッキングと、表情のトラッキングで別の値を使うことがある。
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
