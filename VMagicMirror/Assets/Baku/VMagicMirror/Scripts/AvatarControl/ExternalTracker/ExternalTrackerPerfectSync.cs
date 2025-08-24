using System.Collections.Generic;
using System.Linq;
using Baku.VMagicMirror.MediaPipeTracker;
using R3;
using UnityEngine;
using UniVRM10;
using Zenject;

//TODO: ExternalTrackerと関係ないnamespaceやクラス名に直したい
namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary> パーフェクトシンクをやるやつ </summary>
    /// <remarks>
    /// - ブレンドシェイプクリップ名はhinzkaさんのセットアップに倣う
    /// - 歴史的経緯で外部トラッキング(≒iFacialMocap連携)の一種のような扱いになってるが、下記3種類に対応を拡張予定で、実際にそうしたらクラスの移動やrenameをする予定
    ///   - 外部トラッキング
    ///   - 高負荷モードのwebカメラでのトラッキング
    ///   - VMCPで十分な数のブレンドシェイプを受信しているときのトラッキング
    /// </remarks>
    public class ExternalTrackerPerfectSync : MonoBehaviour
    {
        private ExternalTrackerDataSource _externalTracker;
        private FaceControlConfiguration _faceControlConfig;
        private IMessageSender _sender;

        private MediaPipeFacialValueRepository _mediaPipeFacialValueRepository;
        private MediaPipeTrackerRuntimeSettingsRepository _mediaPipeTrackerRuntimeSettings;

        //モデル本来のクリップ一覧
        private VRM10ExpressionMap _modelBasedMap = null;
        
        private bool _hasModel = false;

        /// <summary> 口周りのクリップについて、マイクの値よりもパーフェクトシンクの値を優先するかどうか </summary>
        /// <remarks>
        /// 内部的にはExTrackerとWebCamそれぞれで別のフラグを持っているので、FaceControlConfigの状態次第でこの値が切り替わる
        /// </remarks>
        public bool PreferWriteMouthBlendShape { get; private set; } = true;

        // 外部トラッキングの使用時にマイクのリップシンクより外部トラッキングの検出結果を優先する場合はtrue
        private readonly ReactiveProperty<bool> _preferExternalTrackerLipSyncThanMic = new(true);
        

        /// <summary>  </summary>
        public ExpressionKey[] NonPerfectSyncKeys { get; private set; } = null;

        public bool IsActive { get; private set; }
        
        public bool IsConnected => 
            (_faceControlConfig.BlendShapeControlMode.CurrentValue is FaceControlModes.ExternalTracker && _externalTracker.Connected) ||
            (_faceControlConfig.BlendShapeControlMode.CurrentValue is FaceControlModes.WebCamHighPower && _mediaPipeFacialValueRepository.IsTracked);

        private readonly ReactiveProperty<bool> _isExternalTrackerPerfectSyncEnabled = new();
        private ReadOnlyReactiveProperty<bool> WebCamHighPowerModePerfectSyncEnabled => 
            _mediaPipeTrackerRuntimeSettings.ShouldUsePerfectSyncResult;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver, 
            IMessageSender sender,
            IVRMLoadable vrmLoadable,
            ExternalTrackerDataSource externalTracker,
            MediaPipeFacialValueRepository mediaPipeFacialValueRepository,
            MediaPipeTrackerRuntimeSettingsRepository mediaPipeTrackerRuntimeSettings,
            FaceControlConfiguration faceControlConfig
            )
        {
            _sender = sender;
            _externalTracker = externalTracker;
            _faceControlConfig = faceControlConfig;

            _mediaPipeFacialValueRepository = mediaPipeFacialValueRepository;
            _mediaPipeTrackerRuntimeSettings = mediaPipeTrackerRuntimeSettings;

            vrmLoadable.VrmLoaded += info =>
            {
                //参照じゃなくて値コピーしとくことに注意(なにかと安全なので)
                _modelBasedMap = info.instance.Vrm.Expression.LoadExpressionMap();
                NonPerfectSyncKeys = LoadNonPerfectSyncKeys();
                _hasModel = true;
                ParseClipCompletenessToSendMessage();
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _modelBasedMap = null;
            };
            
            receiver.BindBoolProperty(
                VmmCommands.ExTrackerEnablePerfectSync,
                _isExternalTrackerPerfectSyncEnabled
            );
            receiver.BindBoolProperty(
                VmmCommands.ExTrackerEnableLipSync,
                _preferExternalTrackerLipSyncThanMic
            );

            _isExternalTrackerPerfectSyncEnabled
                .Subscribe(v => _faceControlConfig.UseExternalTrackerPerfectSync = v)
                .AddTo(this);
            WebCamHighPowerModePerfectSyncEnabled
                .Subscribe(v => _faceControlConfig.UseWebCamHighPowerModePerfectSync = v)
                .AddTo(this);

            _faceControlConfig.BlendShapeControlMode.CombineLatest(
                    _isExternalTrackerPerfectSyncEnabled,
                    WebCamHighPowerModePerfectSyncEnabled,
                    (mode, exTrackerPerfectSync, webCamPerfectSync) =>
                        (mode is FaceControlModes.ExternalTracker && exTrackerPerfectSync) ||
                        (mode is FaceControlModes.WebCamHighPower && webCamPerfectSync)
                )
                .Subscribe(isActive =>
                {
                    IsActive = isActive;
                    //パーフェクトシンク中はまばたき目下げを切る: 切らないと動きすぎになる為
                    _faceControlConfig.ShouldStopEyeDownOnBlink = isActive;
                })
                .AddTo(this);
            
            _faceControlConfig.BlendShapeControlMode.CombineLatest(
                    _preferExternalTrackerLipSyncThanMic,
                    _mediaPipeTrackerRuntimeSettings.ShouldUseLipSyncResult,
                    (mode, exTrackerLipSync, webCamLipSync) =>
                        (mode is FaceControlModes.ExternalTracker && exTrackerLipSync) ||
                        (mode is FaceControlModes.WebCamHighPower && webCamLipSync)
                )
                .Subscribe(usePerfectSyncLipSync => PreferWriteMouthBlendShape = usePerfectSyncLipSync)
                .AddTo(this);
        }

        public bool IsReadyToAccumulate
        {
            get
            {
                if (!_hasModel || !IsActive)
                {
                    return false;
                }

                var result = 
                    (_faceControlConfig.BlendShapeControlMode.CurrentValue is FaceControlModes.ExternalTracker && _externalTracker.Connected) ||
                    (_faceControlConfig.BlendShapeControlMode.CurrentValue is FaceControlModes.WebCamHighPower &&
                        _mediaPipeFacialValueRepository.IsTracked);
                return result;
            }
        }

        /// <summary>
        /// パーフェクトシンクで取得したブレンドシェイプ値があればそれを適用します。
        /// </summary>
        /// <param name="accumulator"></param>
        /// <param name="nonMouthPart">口以外を適用するかどうか</param>
        /// <param name="mouthPart">
        /// 口を適用するかどうか。原則<see cref="PreferWriteMouthBlendShape"/>
        /// </param>
        /// <param name="writeExcludedKeys">
        /// <param name="mouthWeight"></param>
        /// <param name="nonMouthWeight"></param>
        /// trueを指定すると非適用のクリップに0を書き込みます。
        /// これにより、常にパーフェクトシンク分のクリップ情報が過不足なく更新されるのを保証します。
        /// </param>
        public void Accumulate(
            ExpressionAccumulator accumulator, 
            bool nonMouthPart, bool mouthPart, bool writeExcludedKeys, 
            float mouthWeight = 1f, float nonMouthWeight = 1f
            )
        {
            if (!IsReadyToAccumulate)
            {                
                return;
            }

            // HACK: iFacialMocapとMediaPipeでブレンドシェイプの左右で一貫性がうまく取れてないので、if分岐のためにsourceの実体をチェックする
            // TODO: ホントはこうじゃなくて IFaceTrackBlendShapes の左右の扱いが揃うようになっていてほしい…

            var sourceIsExTracker = _faceControlConfig.BlendShapeControlMode.CurrentValue is FaceControlModes.ExternalTracker;
            var source = sourceIsExTracker
                ? _externalTracker.CurrentSource
                : _mediaPipeFacialValueRepository.CorrectedBlendShapes;
            
            var disableHorizontalFlip = _externalTracker.DisableHorizontalFlip;
            //NOTE: 関数レベルで分ける。DistableHorizontalFlipフラグを使って逐次的に三項演算子で書いてもいいんだけど、
            //それよりは関数ごと分けた方がパフォーマンスがいいのでは？という意図で書いてます。何となくです
            if ((sourceIsExTracker && _externalTracker.DisableHorizontalFlip) ||
                (!sourceIsExTracker && !disableHorizontalFlip))
            {
                AccumulateWithFlip(accumulator, source, nonMouthPart, mouthPart, writeExcludedKeys, mouthWeight, nonMouthWeight);
            }
            else
            {
                AccumulateWithoutFlip(accumulator, source, nonMouthPart, mouthPart, writeExcludedKeys, mouthWeight, nonMouthWeight);
            }
        }

        private void AccumulateWithoutFlip(
            ExpressionAccumulator accumulator, IFaceTrackBlendShapes source,
            bool nonMouthPart, bool mouthPart, bool writeExcludedKeys,
            float mouthWeight, float nonMouthWeight
            )
        {
            if (nonMouthPart)
            {
                //目
                var eye = source.Eye;
                accumulator.Accumulate(Keys.EyeBlinkLeft, eye.LeftBlink * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookUpLeft, eye.LeftLookUp * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookDownLeft, eye.LeftLookDown * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookInLeft, eye.LeftLookIn * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookOutLeft, eye.LeftLookOut * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeWideLeft, eye.LeftWide * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeSquintLeft, eye.LeftSquint * nonMouthWeight);

                accumulator.Accumulate(Keys.EyeBlinkRight, eye.RightBlink * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookUpRight, eye.RightLookUp * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookDownRight, eye.RightLookDown * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookInRight, eye.RightLookIn * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookOutRight, eye.RightLookOut * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeWideRight, eye.RightWide * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeSquintRight, eye.RightSquint * nonMouthWeight);

                //NOTE: 瞬き時の目下げ処理に使うためにセット
                _faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
                _faceControlConfig.AlternativeBlinkR = eye.RightBlink;
                
                
                //鼻
                accumulator.Accumulate(Keys.NoseSneerLeft, source.Nose.LeftSneer * nonMouthWeight);
                accumulator.Accumulate(Keys.NoseSneerRight, source.Nose.RightSneer * nonMouthWeight);

                //まゆげ
                accumulator.Accumulate(Keys.BrowDownLeft, source.Brow.LeftDown * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowOuterUpLeft, source.Brow.LeftOuterUp * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowDownRight, source.Brow.RightDown * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowOuterUpRight, source.Brow.RightOuterUp * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowInnerUp, source.Brow.InnerUp * nonMouthWeight);
            }
            else if (writeExcludedKeys)
            {
                //目
                accumulator.Accumulate(Keys.EyeBlinkLeft, 0);
                accumulator.Accumulate(Keys.EyeLookUpLeft, 0);
                accumulator.Accumulate(Keys.EyeLookDownLeft, 0);
                accumulator.Accumulate(Keys.EyeLookInLeft, 0);
                accumulator.Accumulate(Keys.EyeLookOutLeft, 0);
                accumulator.Accumulate(Keys.EyeWideLeft, 0);
                accumulator.Accumulate(Keys.EyeSquintLeft, 0);

                accumulator.Accumulate(Keys.EyeBlinkRight, 0);
                accumulator.Accumulate(Keys.EyeLookUpRight, 0);
                accumulator.Accumulate(Keys.EyeLookDownRight, 0);
                accumulator.Accumulate(Keys.EyeLookInRight, 0);
                accumulator.Accumulate(Keys.EyeLookOutRight, 0);
                accumulator.Accumulate(Keys.EyeWideRight, 0);
                accumulator.Accumulate(Keys.EyeSquintRight, 0);

                //NOTE: 瞬き時の目下げ処理に使うためにセット...は非適用時は要らない。
                //_faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
                //_faceControlConfig.AlternativeBlinkR = eye.RightBlink;                
                
                //鼻
                accumulator.Accumulate(Keys.NoseSneerLeft, 0);
                accumulator.Accumulate(Keys.NoseSneerRight, 0);

                //まゆげ
                accumulator.Accumulate(Keys.BrowDownLeft, 0);
                accumulator.Accumulate(Keys.BrowOuterUpLeft, 0);
                accumulator.Accumulate(Keys.BrowDownRight, 0);
                accumulator.Accumulate(Keys.BrowOuterUpRight, 0);
                accumulator.Accumulate(Keys.BrowInnerUp, 0);
            }

            if (mouthPart)
            {
                //口(多い)
                var mouth = source.Mouth;
                accumulator.Accumulate(Keys.MouthLeft, mouth.Left * mouthWeight);
                accumulator.Accumulate(Keys.MouthSmileLeft, mouth.LeftSmile * mouthWeight);
                accumulator.Accumulate(Keys.MouthFrownLeft, mouth.LeftFrown * mouthWeight);
                accumulator.Accumulate(Keys.MouthPressLeft, mouth.LeftPress * mouthWeight);
                accumulator.Accumulate(Keys.MouthUpperUpLeft, mouth.LeftUpperUp * mouthWeight);
                accumulator.Accumulate(Keys.MouthLowerDownLeft, mouth.LeftLowerDown * mouthWeight);
                accumulator.Accumulate(Keys.MouthStretchLeft, mouth.LeftStretch * mouthWeight);
                accumulator.Accumulate(Keys.MouthDimpleLeft, mouth.LeftDimple * mouthWeight);

                accumulator.Accumulate(Keys.MouthRight, mouth.Right * mouthWeight);
                accumulator.Accumulate(Keys.MouthSmileRight, mouth.RightSmile * mouthWeight);
                accumulator.Accumulate(Keys.MouthFrownRight, mouth.RightFrown * mouthWeight);
                accumulator.Accumulate(Keys.MouthPressRight, mouth.RightPress * mouthWeight);
                accumulator.Accumulate(Keys.MouthUpperUpRight, mouth.RightUpperUp * mouthWeight);
                accumulator.Accumulate(Keys.MouthLowerDownRight, mouth.RightLowerDown * mouthWeight);
                accumulator.Accumulate(Keys.MouthStretchRight, mouth.RightStretch * mouthWeight);
                accumulator.Accumulate(Keys.MouthDimpleRight, mouth.RightDimple * mouthWeight);

                accumulator.Accumulate(Keys.MouthClose, mouth.Close * mouthWeight);
                accumulator.Accumulate(Keys.MouthFunnel, mouth.Funnel * mouthWeight);
                accumulator.Accumulate(Keys.MouthPucker, mouth.Pucker * mouthWeight);
                accumulator.Accumulate(Keys.MouthShrugUpper, mouth.ShrugUpper * mouthWeight);
                accumulator.Accumulate(Keys.MouthShrugLower, mouth.ShrugLower * mouthWeight);
                accumulator.Accumulate(Keys.MouthRollUpper, mouth.RollUpper * mouthWeight);
                accumulator.Accumulate(Keys.MouthRollLower, mouth.RollLower * mouthWeight);

                //あご
                accumulator.Accumulate(Keys.JawOpen, source.Jaw.Open * mouthWeight);
                accumulator.Accumulate(Keys.JawForward, source.Jaw.Forward * mouthWeight);
                accumulator.Accumulate(Keys.JawLeft, source.Jaw.Left * mouthWeight);
                accumulator.Accumulate(Keys.JawRight, source.Jaw.Right * mouthWeight);

                //舌
                accumulator.Accumulate(Keys.TongueOut, source.Tongue.TongueOut * mouthWeight);

                //ほお
                accumulator.Accumulate(Keys.CheekPuff, source.Cheek.Puff * mouthWeight);
                accumulator.Accumulate(Keys.CheekSquintLeft, source.Cheek.LeftSquint * mouthWeight);
                accumulator.Accumulate(Keys.CheekSquintRight, source.Cheek.RightSquint * mouthWeight);                
            }
            else if (writeExcludedKeys)
            {
                //口(多い)
                accumulator.Accumulate(Keys.MouthLeft, 0);
                accumulator.Accumulate(Keys.MouthSmileLeft, 0);
                accumulator.Accumulate(Keys.MouthFrownLeft, 0);
                accumulator.Accumulate(Keys.MouthPressLeft, 0);
                accumulator.Accumulate(Keys.MouthUpperUpLeft, 0);
                accumulator.Accumulate(Keys.MouthLowerDownLeft, 0);
                accumulator.Accumulate(Keys.MouthStretchLeft, 0);
                accumulator.Accumulate(Keys.MouthDimpleLeft, 0);

                accumulator.Accumulate(Keys.MouthRight, 0);
                accumulator.Accumulate(Keys.MouthSmileRight, 0);
                accumulator.Accumulate(Keys.MouthFrownRight, 0);
                accumulator.Accumulate(Keys.MouthPressRight, 0);
                accumulator.Accumulate(Keys.MouthUpperUpRight, 0);
                accumulator.Accumulate(Keys.MouthLowerDownRight, 0);
                accumulator.Accumulate(Keys.MouthStretchRight, 0);
                accumulator.Accumulate(Keys.MouthDimpleRight, 0);

                accumulator.Accumulate(Keys.MouthClose, 0);
                accumulator.Accumulate(Keys.MouthFunnel, 0);
                accumulator.Accumulate(Keys.MouthPucker, 0);
                accumulator.Accumulate(Keys.MouthShrugUpper, 0);
                accumulator.Accumulate(Keys.MouthShrugLower, 0);
                accumulator.Accumulate(Keys.MouthRollUpper, 0);
                accumulator.Accumulate(Keys.MouthRollLower, 0);

                //あご
                accumulator.Accumulate(Keys.JawOpen, 0);
                accumulator.Accumulate(Keys.JawForward, 0);
                accumulator.Accumulate(Keys.JawLeft, 0);
                accumulator.Accumulate(Keys.JawRight, 0);

                //舌
                accumulator.Accumulate(Keys.TongueOut, 0);

                //ほお
                accumulator.Accumulate(Keys.CheekPuff, 0);
                accumulator.Accumulate(Keys.CheekSquintLeft, 0);
                accumulator.Accumulate(Keys.CheekSquintRight, 0);
            }
        }
        
        private void AccumulateWithFlip(
            ExpressionAccumulator accumulator, IFaceTrackBlendShapes source,
            bool nonMouthPart, bool mouthPart, bool writeExcludedKeys,
            float mouthWeight, float nonMouthWeight
        )
        {
            if (nonMouthPart)
            {
                //目
                var eye = source.Eye;
                accumulator.Accumulate(Keys.EyeBlinkRight, eye.LeftBlink * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookUpRight, eye.LeftLookUp * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookDownRight, eye.LeftLookDown * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookInRight, eye.LeftLookIn * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookOutRight, eye.LeftLookOut * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeWideRight, eye.LeftWide * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeSquintRight, eye.LeftSquint * nonMouthWeight);

                accumulator.Accumulate(Keys.EyeBlinkLeft, eye.RightBlink * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookUpLeft, eye.RightLookUp * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookDownLeft, eye.RightLookDown * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookInLeft, eye.RightLookIn * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeLookOutLeft, eye.RightLookOut * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeWideLeft, eye.RightWide * nonMouthWeight);
                accumulator.Accumulate(Keys.EyeSquintLeft, eye.RightSquint * nonMouthWeight);

                //NOTE: 瞬き時の目下げ処理に使うためにセット
                _faceControlConfig.AlternativeBlinkR = eye.LeftBlink;
                _faceControlConfig.AlternativeBlinkL = eye.RightBlink;

                //鼻
                accumulator.Accumulate(Keys.NoseSneerRight, source.Nose.LeftSneer * nonMouthWeight);
                accumulator.Accumulate(Keys.NoseSneerLeft, source.Nose.RightSneer * nonMouthWeight);

                //まゆげ
                accumulator.Accumulate(Keys.BrowDownRight, source.Brow.LeftDown * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowOuterUpRight, source.Brow.LeftOuterUp * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowDownLeft, source.Brow.RightDown * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowOuterUpLeft, source.Brow.RightOuterUp * nonMouthWeight);
                accumulator.Accumulate(Keys.BrowInnerUp, source.Brow.InnerUp * nonMouthWeight);
            }
            else if (writeExcludedKeys)
            {
                //目
                accumulator.Accumulate(Keys.EyeBlinkRight, 0);
                accumulator.Accumulate(Keys.EyeLookUpRight, 0);
                accumulator.Accumulate(Keys.EyeLookDownRight, 0);
                accumulator.Accumulate(Keys.EyeLookInRight, 0);
                accumulator.Accumulate(Keys.EyeLookOutRight, 0);
                accumulator.Accumulate(Keys.EyeWideRight, 0);
                accumulator.Accumulate(Keys.EyeSquintRight, 0);

                accumulator.Accumulate(Keys.EyeBlinkLeft, 0);
                accumulator.Accumulate(Keys.EyeLookUpLeft, 0);
                accumulator.Accumulate(Keys.EyeLookDownLeft, 0);
                accumulator.Accumulate(Keys.EyeLookInLeft, 0);
                accumulator.Accumulate(Keys.EyeLookOutLeft, 0);
                accumulator.Accumulate(Keys.EyeWideLeft, 0);
                accumulator.Accumulate(Keys.EyeSquintLeft, 0);

                //NOTE: 瞬き時の目下げ処理に使うためにセット...は非適用時は要らない。
                // _faceControlConfig.AlternativeBlinkR = eye.LeftBlink;
                // _faceControlConfig.AlternativeBlinkL = eye.RightBlink;

                //鼻
                accumulator.Accumulate(Keys.NoseSneerRight, 0);
                accumulator.Accumulate(Keys.NoseSneerLeft, 0);

                //まゆげ
                accumulator.Accumulate(Keys.BrowDownRight, 0);
                accumulator.Accumulate(Keys.BrowOuterUpRight, 0);
                accumulator.Accumulate(Keys.BrowDownLeft, 0);
                accumulator.Accumulate(Keys.BrowOuterUpLeft, 0);
                accumulator.Accumulate(Keys.BrowInnerUp, 0);
            }

            if (mouthPart)
            {
                //口(多い)
                var mouth = source.Mouth;
                accumulator.Accumulate(Keys.MouthRight, mouth.Left * mouthWeight);
                accumulator.Accumulate(Keys.MouthSmileRight, mouth.LeftSmile * mouthWeight);
                accumulator.Accumulate(Keys.MouthFrownRight, mouth.LeftFrown * mouthWeight);
                accumulator.Accumulate(Keys.MouthPressRight, mouth.LeftPress * mouthWeight);
                accumulator.Accumulate(Keys.MouthUpperUpRight, mouth.LeftUpperUp * mouthWeight);
                accumulator.Accumulate(Keys.MouthLowerDownRight, mouth.LeftLowerDown * mouthWeight);
                accumulator.Accumulate(Keys.MouthStretchRight, mouth.LeftStretch * mouthWeight);
                accumulator.Accumulate(Keys.MouthDimpleRight, mouth.LeftDimple * mouthWeight);

                accumulator.Accumulate(Keys.MouthLeft, mouth.Right * mouthWeight);
                accumulator.Accumulate(Keys.MouthSmileLeft, mouth.RightSmile * mouthWeight);
                accumulator.Accumulate(Keys.MouthFrownLeft, mouth.RightFrown * mouthWeight);
                accumulator.Accumulate(Keys.MouthPressLeft, mouth.RightPress * mouthWeight);
                accumulator.Accumulate(Keys.MouthUpperUpLeft, mouth.RightUpperUp * mouthWeight);
                accumulator.Accumulate(Keys.MouthLowerDownLeft, mouth.RightLowerDown * mouthWeight);
                accumulator.Accumulate(Keys.MouthStretchLeft, mouth.RightStretch * mouthWeight);
                accumulator.Accumulate(Keys.MouthDimpleLeft, mouth.RightDimple * mouthWeight);

                accumulator.Accumulate(Keys.MouthClose, mouth.Close * mouthWeight);
                accumulator.Accumulate(Keys.MouthFunnel, mouth.Funnel * mouthWeight);
                accumulator.Accumulate(Keys.MouthPucker, mouth.Pucker * mouthWeight);
                accumulator.Accumulate(Keys.MouthShrugUpper, mouth.ShrugUpper * mouthWeight);
                accumulator.Accumulate(Keys.MouthShrugLower, mouth.ShrugLower * mouthWeight);
                accumulator.Accumulate(Keys.MouthRollUpper, mouth.RollUpper * mouthWeight);
                accumulator.Accumulate(Keys.MouthRollLower, mouth.RollLower * mouthWeight);

                //あご
                accumulator.Accumulate(Keys.JawOpen, source.Jaw.Open * mouthWeight);
                accumulator.Accumulate(Keys.JawForward, source.Jaw.Forward * mouthWeight);
                accumulator.Accumulate(Keys.JawRight, source.Jaw.Left * mouthWeight);
                accumulator.Accumulate(Keys.JawLeft, source.Jaw.Right * mouthWeight);

                //舌
                accumulator.Accumulate(Keys.TongueOut, source.Tongue.TongueOut * mouthWeight);

                //ほお
                accumulator.Accumulate(Keys.CheekPuff, source.Cheek.Puff * mouthWeight);
                accumulator.Accumulate(Keys.CheekSquintRight, source.Cheek.LeftSquint * mouthWeight);
                accumulator.Accumulate(Keys.CheekSquintLeft, source.Cheek.RightSquint * mouthWeight);                
            }
            else if (writeExcludedKeys)
            {
                //口(多い)
                accumulator.Accumulate(Keys.MouthRight, 0);
                accumulator.Accumulate(Keys.MouthSmileRight, 0);
                accumulator.Accumulate(Keys.MouthFrownRight, 0);
                accumulator.Accumulate(Keys.MouthPressRight, 0);
                accumulator.Accumulate(Keys.MouthUpperUpRight, 0);
                accumulator.Accumulate(Keys.MouthLowerDownRight, 0);
                accumulator.Accumulate(Keys.MouthStretchRight, 0);
                accumulator.Accumulate(Keys.MouthDimpleRight, 0);

                accumulator.Accumulate(Keys.MouthLeft, 0);
                accumulator.Accumulate(Keys.MouthSmileLeft, 0);
                accumulator.Accumulate(Keys.MouthFrownLeft, 0);
                accumulator.Accumulate(Keys.MouthPressLeft, 0);
                accumulator.Accumulate(Keys.MouthUpperUpLeft, 0);
                accumulator.Accumulate(Keys.MouthLowerDownLeft, 0);
                accumulator.Accumulate(Keys.MouthStretchLeft, 0);
                accumulator.Accumulate(Keys.MouthDimpleLeft, 0);

                accumulator.Accumulate(Keys.MouthClose, 0);
                accumulator.Accumulate(Keys.MouthFunnel, 0);
                accumulator.Accumulate(Keys.MouthPucker, 0);
                accumulator.Accumulate(Keys.MouthShrugUpper, 0);
                accumulator.Accumulate(Keys.MouthShrugLower, 0);
                accumulator.Accumulate(Keys.MouthRollUpper, 0);
                accumulator.Accumulate(Keys.MouthRollLower, 0);

                //あご
                accumulator.Accumulate(Keys.JawOpen, 0);
                accumulator.Accumulate(Keys.JawForward, 0);
                accumulator.Accumulate(Keys.JawRight, 0);
                accumulator.Accumulate(Keys.JawLeft, 0);

                //舌
                accumulator.Accumulate(Keys.TongueOut, 0);

                //ほお
                accumulator.Accumulate(Keys.CheekPuff, 0);
                accumulator.Accumulate(Keys.CheekSquintRight, 0);
                accumulator.Accumulate(Keys.CheekSquintLeft, 0);
            }            
        }
        
        private void ParseClipCompletenessToSendMessage()
        {
            //やること: パーフェクトシンク用に定義されていてほしいにも関わらず、定義が漏れたクリップがないかチェックする。
            var modelBlendShapeNames = new HashSet<string>(
                _modelBasedMap.Keys
                    .Where(k => k.Preset == ExpressionPreset.custom)
                    .Select(k => k.Name)
            );
            var missedBlendShapeNames = Keys.BlendShapeNames
                .Where(n => !modelBlendShapeNames.Contains(n))
                .ToList();

            if (missedBlendShapeNames.Count > 0)
            {
                _sender.SendCommand(MessageFactory.ExTrackerSetPerfectSyncMissedClipNames(
                    $"Missing Count: {missedBlendShapeNames.Count} / 52,\n" + 
                    string.Join("\n", missedBlendShapeNames)
                    ));
            }
            else
            {
                //空文字列を送ることでエラーが解消したことを通知する
                _sender.SendCommand(MessageFactory.ExTrackerSetPerfectSyncMissedClipNames(""));
            }
        }
    
        private ExpressionKey[] LoadNonPerfectSyncKeys()
        {
            var perfectSyncKeys = Keys.PerfectSyncKeys;
            return _modelBasedMap.Keys
                .Where(key => !perfectSyncKeys.Any(k => k.Equals(key)))
                .ToArray();
        }
        
        /// <summary> 決め打ちされた、パーフェクトシンクで使うブレンドシェイプの一覧 </summary>
        public static class Keys
        {
            static Keys()
            {
                BlendShapeNames = new[]
                {
                    //目
                    nameof(EyeBlinkLeft),
                    nameof(EyeLookUpLeft),
                    nameof(EyeLookDownLeft),
                    nameof(EyeLookInLeft),
                    nameof(EyeLookOutLeft),
                    nameof(EyeWideLeft),
                    nameof(EyeSquintLeft),

                    nameof(EyeBlinkRight),
                    nameof(EyeLookUpRight),
                    nameof(EyeLookDownRight),
                    nameof(EyeLookInRight),
                    nameof(EyeLookOutRight),
                    nameof(EyeWideRight),
                    nameof(EyeSquintRight),

                    //口(多い)
                    nameof(MouthLeft),
                    nameof(MouthSmileLeft),
                    nameof(MouthFrownLeft),
                    nameof(MouthPressLeft),
                    nameof(MouthUpperUpLeft),
                    nameof(MouthLowerDownLeft),
                    nameof(MouthStretchLeft),
                    nameof(MouthDimpleLeft),

                    nameof(MouthRight),
                    nameof(MouthSmileRight),
                    nameof(MouthFrownRight),
                    nameof(MouthPressRight),
                    nameof(MouthUpperUpRight),
                    nameof(MouthLowerDownRight),
                    nameof(MouthStretchRight),
                    nameof(MouthDimpleRight),

                    nameof(MouthClose),
                    nameof(MouthFunnel),
                    nameof(MouthPucker),
                    nameof(MouthShrugUpper),
                    nameof(MouthShrugLower),
                    nameof(MouthRollUpper),
                    nameof(MouthRollLower),

                    //あご
                    nameof(JawOpen),
                    nameof(JawForward),
                    nameof(JawLeft),
                    nameof(JawRight),

                    //鼻
                    nameof(NoseSneerLeft),
                    nameof(NoseSneerRight),

                    //ほお
                    nameof(CheekPuff),
                    nameof(CheekSquintLeft),
                    nameof(CheekSquintRight),

                    //舌
                    nameof(TongueOut),

                    //まゆげ
                    nameof(BrowDownLeft),
                    nameof(BrowOuterUpLeft),
                    nameof(BrowDownRight),
                    nameof(BrowOuterUpRight),
                    nameof(BrowInnerUp),
                };             
                
                PerfectSyncKeys = new[]
                {
                    //目
                    EyeBlinkLeft,
                    EyeLookUpLeft,
                    EyeLookDownLeft,
                    EyeLookInLeft,
                    EyeLookOutLeft,
                    EyeWideLeft,
                    EyeSquintLeft,

                    EyeBlinkRight,
                    EyeLookUpRight,
                    EyeLookDownRight,
                    EyeLookInRight,
                    EyeLookOutRight,
                    EyeWideRight,
                    EyeSquintRight,

                    //口(多い)
                    MouthLeft,
                    MouthSmileLeft,
                    MouthFrownLeft,
                    MouthPressLeft,
                    MouthUpperUpLeft,
                    MouthLowerDownLeft,
                    MouthStretchLeft,
                    MouthDimpleLeft,

                    MouthRight,
                    MouthSmileRight,
                    MouthFrownRight,
                    MouthPressRight,
                    MouthUpperUpRight,
                    MouthLowerDownRight,
                    MouthStretchRight,
                    MouthDimpleRight,

                    MouthClose,
                    MouthFunnel,
                    MouthPucker,
                    MouthShrugUpper,
                    MouthShrugLower,
                    MouthRollUpper,
                    MouthRollLower,

                    //あご
                    JawOpen,
                    JawForward,
                    JawLeft,
                    JawRight,

                    //鼻
                    NoseSneerLeft,
                    NoseSneerRight,

                    //ほお
                    CheekPuff,
                    CheekSquintLeft,
                    CheekSquintRight,

                    //舌
                    TongueOut,

                    //まゆげ
                    BrowDownLeft,
                    BrowOuterUpLeft,
                    BrowDownRight,
                    BrowOuterUpRight,
                    BrowInnerUp,
                };                
                
                PerfectSyncMouthKeys =  new[]
                {
                    //口(多い)
                    MouthLeft,
                    MouthSmileLeft,
                    MouthFrownLeft,
                    MouthPressLeft,
                    MouthUpperUpLeft,
                    MouthLowerDownLeft,
                    MouthStretchLeft,
                    MouthDimpleLeft,

                    MouthRight,
                    MouthSmileRight,
                    MouthFrownRight,
                    MouthPressRight,
                    MouthUpperUpRight,
                    MouthLowerDownRight,
                    MouthStretchRight,
                    MouthDimpleRight,

                    MouthClose,
                    MouthFunnel,
                    MouthPucker,
                    MouthShrugUpper,
                    MouthShrugLower,
                    MouthRollUpper,
                    MouthRollLower,

                    //あご
                    JawOpen,
                    JawForward,
                    JawLeft,
                    JawRight,
                    
                    //ほお
                    CheekPuff,
                    CheekSquintLeft,
                    CheekSquintRight,

                    //舌
                    TongueOut,
                };                
                
                PerfectSyncNonMouthKeys = new[]
                {
                    //目
                    EyeBlinkLeft,
                    EyeLookUpLeft,
                    EyeLookDownLeft,
                    EyeLookInLeft,
                    EyeLookOutLeft,
                    EyeWideLeft,
                    EyeSquintLeft,

                    EyeBlinkRight,
                    EyeLookUpRight,
                    EyeLookDownRight,
                    EyeLookInRight,
                    EyeLookOutRight,
                    EyeWideRight,
                    EyeSquintRight,

                    //鼻
                    NoseSneerLeft,
                    NoseSneerRight,

                    //まゆげ
                    BrowDownLeft,
                    BrowOuterUpLeft,
                    BrowDownRight,
                    BrowOuterUpRight,
                    BrowInnerUp,
                };
            }

            //TODO: これ大文字小文字の配慮おかしいかも。要注意です
            /// <summary> Perfect Syncでいじる対象のブレンドシェイプキー名の一覧 </summary>
            public static string[] BlendShapeNames { get; }
            
            /// <summary> Perfect Syncでいじる対象のブレンドシェイプキー一覧を取得します。 </summary>
            public static ExpressionKey[] PerfectSyncKeys { get; }
            /// <summary> Perfect Syncの口、ほお、顎、舌のキーを取得します。</summary>
            public static ExpressionKey[] PerfectSyncMouthKeys { get; }
            /// <summary> Perfect Syncの目、鼻、眉のキーを取得します。</summary>
            public static ExpressionKey[] PerfectSyncNonMouthKeys { get; }
            
            //目
            public static readonly ExpressionKey EyeBlinkLeft = ExpressionKey.CreateCustom(nameof(EyeBlinkLeft));
            public static readonly ExpressionKey EyeLookUpLeft = ExpressionKey.CreateCustom(nameof(EyeLookUpLeft));
            public static readonly ExpressionKey EyeLookDownLeft = ExpressionKey.CreateCustom(nameof(EyeLookDownLeft));
            public static readonly ExpressionKey EyeLookInLeft = ExpressionKey.CreateCustom(nameof(EyeLookInLeft));
            public static readonly ExpressionKey EyeLookOutLeft = ExpressionKey.CreateCustom(nameof(EyeLookOutLeft));
            public static readonly ExpressionKey EyeWideLeft = ExpressionKey.CreateCustom(nameof(EyeWideLeft));
            public static readonly ExpressionKey EyeSquintLeft = ExpressionKey.CreateCustom(nameof(EyeSquintLeft));

            public static readonly ExpressionKey EyeBlinkRight = ExpressionKey.CreateCustom(nameof(EyeBlinkRight));
            public static readonly ExpressionKey EyeLookUpRight = ExpressionKey.CreateCustom(nameof(EyeLookUpRight));
            public static readonly ExpressionKey EyeLookDownRight = ExpressionKey.CreateCustom(nameof(EyeLookDownRight));
            public static readonly ExpressionKey EyeLookInRight = ExpressionKey.CreateCustom(nameof(EyeLookInRight));
            public static readonly ExpressionKey EyeLookOutRight = ExpressionKey.CreateCustom(nameof(EyeLookOutRight));
            public static readonly ExpressionKey EyeWideRight = ExpressionKey.CreateCustom(nameof(EyeWideRight));
            public static readonly ExpressionKey EyeSquintRight = ExpressionKey.CreateCustom(nameof(EyeSquintRight));

            //口(多い)
            public static readonly ExpressionKey MouthLeft = ExpressionKey.CreateCustom(nameof(MouthLeft));
            public static readonly ExpressionKey MouthSmileLeft = ExpressionKey.CreateCustom(nameof(MouthSmileLeft));
            public static readonly ExpressionKey MouthFrownLeft = ExpressionKey.CreateCustom(nameof(MouthFrownLeft));
            public static readonly ExpressionKey MouthPressLeft = ExpressionKey.CreateCustom(nameof(MouthPressLeft));
            public static readonly ExpressionKey MouthUpperUpLeft = ExpressionKey.CreateCustom(nameof(MouthUpperUpLeft));
            public static readonly ExpressionKey MouthLowerDownLeft = ExpressionKey.CreateCustom(nameof(MouthLowerDownLeft));
            public static readonly ExpressionKey MouthStretchLeft = ExpressionKey.CreateCustom(nameof(MouthStretchLeft));
            public static readonly ExpressionKey MouthDimpleLeft = ExpressionKey.CreateCustom(nameof(MouthDimpleLeft));

            public static readonly ExpressionKey MouthRight = ExpressionKey.CreateCustom(nameof(MouthRight));
            public static readonly ExpressionKey MouthSmileRight = ExpressionKey.CreateCustom(nameof(MouthSmileRight));
            public static readonly ExpressionKey MouthFrownRight = ExpressionKey.CreateCustom(nameof(MouthFrownRight));
            public static readonly ExpressionKey MouthPressRight = ExpressionKey.CreateCustom(nameof(MouthPressRight));
            public static readonly ExpressionKey MouthUpperUpRight = ExpressionKey.CreateCustom(nameof(MouthUpperUpRight));
            public static readonly ExpressionKey MouthLowerDownRight = ExpressionKey.CreateCustom(nameof(MouthLowerDownRight));
            public static readonly ExpressionKey MouthStretchRight = ExpressionKey.CreateCustom(nameof(MouthStretchRight));
            public static readonly ExpressionKey MouthDimpleRight = ExpressionKey.CreateCustom(nameof(MouthDimpleRight));
            
            public static readonly ExpressionKey MouthClose = ExpressionKey.CreateCustom(nameof(MouthClose));
            public static readonly ExpressionKey MouthFunnel = ExpressionKey.CreateCustom(nameof(MouthFunnel));
            public static readonly ExpressionKey MouthPucker = ExpressionKey.CreateCustom(nameof(MouthPucker));
            public static readonly ExpressionKey MouthShrugUpper = ExpressionKey.CreateCustom(nameof(MouthShrugUpper));
            public static readonly ExpressionKey MouthShrugLower = ExpressionKey.CreateCustom(nameof(MouthShrugLower));
            public static readonly ExpressionKey MouthRollUpper = ExpressionKey.CreateCustom(nameof(MouthRollUpper));
            public static readonly ExpressionKey MouthRollLower = ExpressionKey.CreateCustom(nameof(MouthRollLower));
            
            //あご
            public static readonly ExpressionKey JawOpen = ExpressionKey.CreateCustom(nameof(JawOpen));
            public static readonly ExpressionKey JawForward = ExpressionKey.CreateCustom(nameof(JawForward));
            public static readonly ExpressionKey JawLeft = ExpressionKey.CreateCustom(nameof(JawLeft));
            public static readonly ExpressionKey JawRight = ExpressionKey.CreateCustom(nameof(JawRight));
            
            //鼻
            public static readonly ExpressionKey NoseSneerLeft = ExpressionKey.CreateCustom(nameof(NoseSneerLeft));
            public static readonly ExpressionKey NoseSneerRight = ExpressionKey.CreateCustom(nameof(NoseSneerRight));

            //ほお
            public static readonly ExpressionKey CheekPuff = ExpressionKey.CreateCustom(nameof(CheekPuff));
            public static readonly ExpressionKey CheekSquintLeft = ExpressionKey.CreateCustom(nameof(CheekSquintLeft));
            public static readonly ExpressionKey CheekSquintRight = ExpressionKey.CreateCustom(nameof(CheekSquintRight));
            
            //舌
            public static readonly ExpressionKey TongueOut = ExpressionKey.CreateCustom(nameof(TongueOut));
            
            //まゆげ
            public static readonly ExpressionKey BrowDownLeft = ExpressionKey.CreateCustom(nameof(BrowDownLeft));
            public static readonly ExpressionKey BrowOuterUpLeft = ExpressionKey.CreateCustom(nameof(BrowOuterUpLeft));
            public static readonly ExpressionKey BrowDownRight = ExpressionKey.CreateCustom(nameof(BrowDownRight));
            public static readonly ExpressionKey BrowOuterUpRight = ExpressionKey.CreateCustom(nameof(BrowOuterUpRight));
            public static readonly ExpressionKey BrowInnerUp = ExpressionKey.CreateCustom(nameof(BrowInnerUp));
        }
        
        /// <summary> ブレンドシェイプの上書き処理で使うための、リップシンクのブレンドシェイプキー </summary>
        struct LipSyncValues
        {
            //NOTE: Accumulatorを渡すほうが良ければ修正してほしい
            public LipSyncValues(Vrm10RuntimeExpression expression)
            {
                A = expression.GetWeight(ExpressionKey.Aa);
                I = expression.GetWeight(ExpressionKey.Ih);
                U = expression.GetWeight(ExpressionKey.Ou);
                E = expression.GetWeight(ExpressionKey.Ee);
                O = expression.GetWeight(ExpressionKey.Oh);
            }
            public float A { get; set; }
            public float I { get; set; }
            public float U { get; set; }
            public float E { get; set; }
            public float O { get; set; }
        }
    }
}
