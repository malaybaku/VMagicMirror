using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using VRM;

namespace Baku.VMagicMirror.ExternalTracker
{
    //TODO: 処理順が以下になってると多分正しいので、これがScript Execution Orderで保証されてるかチェック
    //> 1. (自動まばたき、LookAt、EyeJitter、パーフェクトシンクじゃないExTrackerまばたき)
    //> 2. このスクリプト
    //> 3. ExternalTrackerFaceSwitchApplier

    /// <summary> パーフェクトシンクをやるやつです。 </summary>
    /// <remarks>
    /// ブレンドシェイプクリップ名はhinzkaさんのセットアップに倣います。
    /// </remarks>
    public class ExternalTrackerPerfectSync : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] private float emphasizeThreshold = 0.5f;
        [SerializeField] private bool emphasizeExpression;
        [SerializeField] private PerfectSyncEmphasizeSetting emphasizeSetting;
        [SerializeField] private bool recordOnEditor = false;

        private ExternalTrackerDataSource _externalTracker = null;
        private FaceControlConfiguration _faceControlConfig = null;
        private IMessageSender _sender = null;

        //モデル本来のクリップ一覧
        private List<BlendShapeClip> _modelBaseClips = null;

        private bool _hasModel = false;

        /// <summary> 口周りのクリップもパーフェクトシンクのを適用するかどうか。デフォルトではtrue </summary>
        public bool PreferWriteMouthBlendShape { get; private set; } = true;

        /// <summary> 表情を強調するための後処理を行うかどうか。デフォルトではfalse </summary>
        public bool EmphasizeExpression
        {
            get => emphasizeExpression;
            private set => emphasizeExpression = value;
        }

        /// <summary>  </summary>
        public BlendShapeKey[] NonPerfectSyncKeys { get; private set; } = null;

        public bool IsActive { get; private set; }

        /// <summary> デバッグ目的で使います。Accumulate処理の経過を観察したいときに使えます。 </summary>
        public event Action<BlendShapeKey, float> ValueAccumulated;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IMessageSender sender,
            IVRMLoadable vrmLoadable,
            ExternalTrackerDataSource externalTracker,
            FaceControlConfiguration faceControlConfig
        )
        {
            _sender = sender;
            _externalTracker = externalTracker;
            _faceControlConfig = faceControlConfig;

            vrmLoadable.VrmLoaded += info =>
            {
                //参照じゃなくて値コピーしとくことに注意(なにかと安全なので)
                _modelBaseClips = info.blendShape.BlendShapeAvatar.Clips.ToList();
                NonPerfectSyncKeys = LoadNonPerfectSyncKeys();
                _hasModel = true;
                ParseClipCompletenessToSendMessage();
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _modelBaseClips = null;
            };

            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnablePerfectSync,
                command =>
                {
                    IsActive = command.ToBoolean();
                    //パーフェクトシンク中はまばたき目下げを切る: 切らないと動きすぎになる為
                    _faceControlConfig.ShouldStopEyeDownOnBlink = IsActive;
                });
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnableLipSync,
                message => PreferWriteMouthBlendShape = message.ToBoolean()
            );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnableEmphasizeExpression,
                message => EmphasizeExpression = message.ToBoolean());
        }

        private PerfectSyncEmphasizeSettingsHandler _emphasizeSettingsHandler;
        private void Start()
        {
            _emphasizeSettingsHandler = new PerfectSyncEmphasizeSettingsHandler(emphasizeSetting);
        }

        private void OnDestroy()
        {
            if (recordOnEditor)
            {
                _emphasizeSettingsHandler.SaveIfEditor();
            }
        }

        public bool IsReadyToAccumulate => _hasModel && IsActive && _externalTracker.Connected;
        
        /// <summary>
        /// パーフェクトシンクで取得したブレンドシェイプ値があればそれを適用します。
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="nonMouthPart">口以外を適用するかどうか</param>
        /// <param name="mouthPart">
        /// 口を適用するかどうか。原則<see cref="PreferWriteMouthBlendShape"/>
        /// </param>
        /// <param name="writeExcludedKeys">
        /// trueを指定すると非適用のクリップに0を書き込みます。
        /// これにより、常にパーフェクトシンク分のクリップ情報が過不足なく更新されるのを保証します。
        /// </param>
        public void Accumulate(VRMBlendShapeProxy proxy, bool nonMouthPart, bool mouthPart, bool writeExcludedKeys)
        {
            if (!IsReadyToAccumulate)
            {                
                return;
            }

            var source = _externalTracker.CurrentSource;
            
            //NOTE: 関数レベルで分ける。DistableHorizontalFlipフラグを使って逐次的に三項演算子で書いてもいいんだけど、
            //それよりは関数ごと分けた方がパフォーマンスがいいのでは？という意図で書いてます。何となくです
            if (_externalTracker.DisableHorizontalFlip)
            {
                AccumulateWithFlip(proxy, source, nonMouthPart, mouthPart, writeExcludedKeys);
            }
            else
            {
                AccumulateWithoutFlip(proxy, source, nonMouthPart, mouthPart, writeExcludedKeys);
            }
        }

        private void AccumulateWithoutFlip(
            VRMBlendShapeProxy proxy,  IFaceTrackSource source,
            bool nonMouthPart, bool mouthPart, bool writeExcludedKeys
            )
        {
            if (nonMouthPart)
            {
                //目
                var eye = source.Eye;
                Accumulate(proxy, Keys.EyeBlinkLeft, eye.LeftBlink);
                Accumulate(proxy, Keys.EyeLookUpLeft, eye.LeftLookUp);
                Accumulate(proxy, Keys.EyeLookDownLeft, eye.LeftLookDown);
                Accumulate(proxy, Keys.EyeLookInLeft, eye.LeftLookIn);
                Accumulate(proxy, Keys.EyeLookOutLeft, eye.LeftLookOut);
                Accumulate(proxy, Keys.EyeWideLeft, eye.LeftWide);
                Accumulate(proxy, Keys.EyeSquintLeft, eye.LeftSquint);

                Accumulate(proxy, Keys.EyeBlinkRight, eye.RightBlink);
                Accumulate(proxy, Keys.EyeLookUpRight, eye.RightLookUp);
                Accumulate(proxy, Keys.EyeLookDownRight, eye.RightLookDown);
                Accumulate(proxy, Keys.EyeLookInRight, eye.RightLookIn);
                Accumulate(proxy, Keys.EyeLookOutRight, eye.RightLookOut);
                Accumulate(proxy, Keys.EyeWideRight, eye.RightWide);
                Accumulate(proxy, Keys.EyeSquintRight, eye.RightSquint);

                //NOTE: 瞬き時の目下げ処理に使うためにセット
                _faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
                _faceControlConfig.AlternativeBlinkR = eye.RightBlink;
                
                
                //鼻
                Accumulate(proxy, Keys.NoseSneerLeft, source.Nose.LeftSneer);
                Accumulate(proxy, Keys.NoseSneerRight, source.Nose.RightSneer);

                //まゆげ
                Accumulate(proxy, Keys.BrowDownLeft, source.Brow.LeftDown);
                Accumulate(proxy, Keys.BrowOuterUpLeft, source.Brow.LeftOuterUp);
                Accumulate(proxy, Keys.BrowDownRight, source.Brow.RightDown);
                Accumulate(proxy, Keys.BrowOuterUpRight, source.Brow.RightOuterUp);
                Accumulate(proxy, Keys.BrowInnerUp, source.Brow.InnerUp);
            }
            else if (writeExcludedKeys)
            {
                //目
                Accumulate(proxy, Keys.EyeBlinkLeft, 0);
                Accumulate(proxy, Keys.EyeLookUpLeft, 0);
                Accumulate(proxy, Keys.EyeLookDownLeft, 0);
                Accumulate(proxy, Keys.EyeLookInLeft, 0);
                Accumulate(proxy, Keys.EyeLookOutLeft, 0);
                Accumulate(proxy, Keys.EyeWideLeft, 0);
                Accumulate(proxy, Keys.EyeSquintLeft, 0);

                Accumulate(proxy, Keys.EyeBlinkRight, 0);
                Accumulate(proxy, Keys.EyeLookUpRight, 0);
                Accumulate(proxy, Keys.EyeLookDownRight, 0);
                Accumulate(proxy, Keys.EyeLookInRight, 0);
                Accumulate(proxy, Keys.EyeLookOutRight, 0);
                Accumulate(proxy, Keys.EyeWideRight, 0);
                Accumulate(proxy, Keys.EyeSquintRight, 0);

                //NOTE: 瞬き時の目下げ処理に使うためにセット...は非適用時は要らない。
                //_faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
                //_faceControlConfig.AlternativeBlinkR = eye.RightBlink;                
                
                //鼻
                Accumulate(proxy, Keys.NoseSneerLeft, 0);
                Accumulate(proxy, Keys.NoseSneerRight, 0);

                //まゆげ
                Accumulate(proxy, Keys.BrowDownLeft, 0);
                Accumulate(proxy, Keys.BrowOuterUpLeft, 0);
                Accumulate(proxy, Keys.BrowDownRight, 0);
                Accumulate(proxy, Keys.BrowOuterUpRight, 0);
                Accumulate(proxy, Keys.BrowInnerUp, 0);
            }

            if (mouthPart)
            {
                //口(多い)
                var mouth = source.Mouth;
                Accumulate(proxy, Keys.MouthLeft, mouth.Left);
                Accumulate(proxy, Keys.MouthSmileLeft, mouth.LeftSmile);
                Accumulate(proxy, Keys.MouthFrownLeft, mouth.LeftFrown);
                Accumulate(proxy, Keys.MouthPressLeft, mouth.LeftPress);
                Accumulate(proxy, Keys.MouthUpperUpLeft, mouth.LeftUpperUp);
                Accumulate(proxy, Keys.MouthLowerDownLeft, mouth.LeftLowerDown);
                Accumulate(proxy, Keys.MouthStretchLeft, mouth.LeftStretch);
                Accumulate(proxy, Keys.MouthDimpleLeft, mouth.LeftDimple);

                Accumulate(proxy, Keys.MouthRight, mouth.Right);
                Accumulate(proxy, Keys.MouthSmileRight, mouth.RightSmile);
                Accumulate(proxy, Keys.MouthFrownRight, mouth.RightFrown);
                Accumulate(proxy, Keys.MouthPressRight, mouth.RightPress);
                Accumulate(proxy, Keys.MouthUpperUpRight, mouth.RightUpperUp);
                Accumulate(proxy, Keys.MouthLowerDownRight, mouth.RightLowerDown);
                Accumulate(proxy, Keys.MouthStretchRight, mouth.RightStretch);
                Accumulate(proxy, Keys.MouthDimpleRight, mouth.RightDimple);

                Accumulate(proxy, Keys.MouthClose, mouth.Close);
                Accumulate(proxy, Keys.MouthFunnel, mouth.Funnel);
                Accumulate(proxy, Keys.MouthPucker, mouth.Pucker);
                Accumulate(proxy, Keys.MouthShrugUpper, mouth.ShrugUpper);
                Accumulate(proxy, Keys.MouthShrugLower, mouth.ShrugLower);
                Accumulate(proxy, Keys.MouthRollUpper, mouth.RollUpper);
                Accumulate(proxy, Keys.MouthRollLower, mouth.RollLower);

                //あご
                Accumulate(proxy, Keys.JawOpen, source.Jaw.Open);
                Accumulate(proxy, Keys.JawForward, source.Jaw.Forward);
                Accumulate(proxy, Keys.JawLeft, source.Jaw.Left);
                Accumulate(proxy, Keys.JawRight, source.Jaw.Right);

                //舌
                Accumulate(proxy, Keys.TongueOut, source.Tongue.TongueOut);

                //ほお
                Accumulate(proxy, Keys.CheekPuff, source.Cheek.Puff);
                Accumulate(proxy, Keys.CheekSquintLeft, source.Cheek.LeftSquint);
                Accumulate(proxy, Keys.CheekSquintRight, source.Cheek.RightSquint);                
            }
            else if (writeExcludedKeys)
            {
                //口(多い)
                Accumulate(proxy, Keys.MouthLeft, 0);
                Accumulate(proxy, Keys.MouthSmileLeft, 0);
                Accumulate(proxy, Keys.MouthFrownLeft, 0);
                Accumulate(proxy, Keys.MouthPressLeft, 0);
                Accumulate(proxy, Keys.MouthUpperUpLeft, 0);
                Accumulate(proxy, Keys.MouthLowerDownLeft, 0);
                Accumulate(proxy, Keys.MouthStretchLeft, 0);
                Accumulate(proxy, Keys.MouthDimpleLeft, 0);

                Accumulate(proxy, Keys.MouthRight, 0);
                Accumulate(proxy, Keys.MouthSmileRight, 0);
                Accumulate(proxy, Keys.MouthFrownRight, 0);
                Accumulate(proxy, Keys.MouthPressRight, 0);
                Accumulate(proxy, Keys.MouthUpperUpRight, 0);
                Accumulate(proxy, Keys.MouthLowerDownRight, 0);
                Accumulate(proxy, Keys.MouthStretchRight, 0);
                Accumulate(proxy, Keys.MouthDimpleRight, 0);

                Accumulate(proxy, Keys.MouthClose, 0);
                Accumulate(proxy, Keys.MouthFunnel, 0);
                Accumulate(proxy, Keys.MouthPucker, 0);
                Accumulate(proxy, Keys.MouthShrugUpper, 0);
                Accumulate(proxy, Keys.MouthShrugLower, 0);
                Accumulate(proxy, Keys.MouthRollUpper, 0);
                Accumulate(proxy, Keys.MouthRollLower, 0);

                //あご
                Accumulate(proxy, Keys.JawOpen, 0);
                Accumulate(proxy, Keys.JawForward, 0);
                Accumulate(proxy, Keys.JawLeft, 0);
                Accumulate(proxy, Keys.JawRight, 0);

                //舌
                Accumulate(proxy, Keys.TongueOut, 0);

                //ほお
                Accumulate(proxy, Keys.CheekPuff, 0);
                Accumulate(proxy, Keys.CheekSquintLeft, 0);
                Accumulate(proxy, Keys.CheekSquintRight, 0);
            }
        }
        
        private void AccumulateWithFlip(
            VRMBlendShapeProxy proxy,  IFaceTrackSource source,
            bool nonMouthPart, bool mouthPart, bool writeExcludedKeys
        )
        {
            if (nonMouthPart)
            {
                //目
                var eye = source.Eye;
                Accumulate(proxy, Keys.EyeBlinkRight, eye.LeftBlink);
                Accumulate(proxy, Keys.EyeLookUpRight, eye.LeftLookUp);
                Accumulate(proxy, Keys.EyeLookDownRight, eye.LeftLookDown);
                Accumulate(proxy, Keys.EyeLookInRight, eye.LeftLookIn);
                Accumulate(proxy, Keys.EyeLookOutRight, eye.LeftLookOut);
                Accumulate(proxy, Keys.EyeWideRight, eye.LeftWide);
                Accumulate(proxy, Keys.EyeSquintRight, eye.LeftSquint);

                Accumulate(proxy, Keys.EyeBlinkLeft, eye.RightBlink);
                Accumulate(proxy, Keys.EyeLookUpLeft, eye.RightLookUp);
                Accumulate(proxy, Keys.EyeLookDownLeft, eye.RightLookDown);
                Accumulate(proxy, Keys.EyeLookInLeft, eye.RightLookIn);
                Accumulate(proxy, Keys.EyeLookOutLeft, eye.RightLookOut);
                Accumulate(proxy, Keys.EyeWideLeft, eye.RightWide);
                Accumulate(proxy, Keys.EyeSquintLeft, eye.RightSquint);

                //NOTE: 瞬き時の目下げ処理に使うためにセット
                _faceControlConfig.AlternativeBlinkR = eye.LeftBlink;
                _faceControlConfig.AlternativeBlinkL = eye.RightBlink;

                //鼻
                Accumulate(proxy, Keys.NoseSneerRight, source.Nose.LeftSneer);
                Accumulate(proxy, Keys.NoseSneerLeft, source.Nose.RightSneer);

                //まゆげ
                Accumulate(proxy, Keys.BrowDownRight, source.Brow.LeftDown);
                Accumulate(proxy, Keys.BrowOuterUpRight, source.Brow.LeftOuterUp);
                Accumulate(proxy, Keys.BrowDownLeft, source.Brow.RightDown);
                Accumulate(proxy, Keys.BrowOuterUpLeft, source.Brow.RightOuterUp);
                Accumulate(proxy, Keys.BrowInnerUp, source.Brow.InnerUp);
            }
            else if (writeExcludedKeys)
            {
                //目
                Accumulate(proxy, Keys.EyeBlinkRight, 0);
                Accumulate(proxy, Keys.EyeLookUpRight, 0);
                Accumulate(proxy, Keys.EyeLookDownRight, 0);
                Accumulate(proxy, Keys.EyeLookInRight, 0);
                Accumulate(proxy, Keys.EyeLookOutRight, 0);
                Accumulate(proxy, Keys.EyeWideRight, 0);
                Accumulate(proxy, Keys.EyeSquintRight, 0);

                Accumulate(proxy, Keys.EyeBlinkLeft, 0);
                Accumulate(proxy, Keys.EyeLookUpLeft, 0);
                Accumulate(proxy, Keys.EyeLookDownLeft, 0);
                Accumulate(proxy, Keys.EyeLookInLeft, 0);
                Accumulate(proxy, Keys.EyeLookOutLeft, 0);
                Accumulate(proxy, Keys.EyeWideLeft, 0);
                Accumulate(proxy, Keys.EyeSquintLeft, 0);

                //NOTE: 瞬き時の目下げ処理に使うためにセット...は非適用時は要らない。
                // _faceControlConfig.AlternativeBlinkR = eye.LeftBlink;
                // _faceControlConfig.AlternativeBlinkL = eye.RightBlink;

                //鼻
                Accumulate(proxy, Keys.NoseSneerRight, 0);
                Accumulate(proxy, Keys.NoseSneerLeft, 0);

                //まゆげ
                Accumulate(proxy, Keys.BrowDownRight, 0);
                Accumulate(proxy, Keys.BrowOuterUpRight, 0);
                Accumulate(proxy, Keys.BrowDownLeft, 0);
                Accumulate(proxy, Keys.BrowOuterUpLeft, 0);
                Accumulate(proxy, Keys.BrowInnerUp, 0);
            }

            if (mouthPart)
            {
                //口(多い)
                var mouth = source.Mouth;
                Accumulate(proxy, Keys.MouthRight, mouth.Left);
                Accumulate(proxy, Keys.MouthSmileRight, mouth.LeftSmile);
                Accumulate(proxy, Keys.MouthFrownRight, mouth.LeftFrown);
                Accumulate(proxy, Keys.MouthPressRight, mouth.LeftPress);
                Accumulate(proxy, Keys.MouthUpperUpRight, mouth.LeftUpperUp);
                Accumulate(proxy, Keys.MouthLowerDownRight, mouth.LeftLowerDown);
                Accumulate(proxy, Keys.MouthStretchRight, mouth.LeftStretch);
                Accumulate(proxy, Keys.MouthDimpleRight, mouth.LeftDimple);

                Accumulate(proxy, Keys.MouthLeft, mouth.Right);
                Accumulate(proxy, Keys.MouthSmileLeft, mouth.RightSmile);
                Accumulate(proxy, Keys.MouthFrownLeft, mouth.RightFrown);
                Accumulate(proxy, Keys.MouthPressLeft, mouth.RightPress);
                Accumulate(proxy, Keys.MouthUpperUpLeft, mouth.RightUpperUp);
                Accumulate(proxy, Keys.MouthLowerDownLeft, mouth.RightLowerDown);
                Accumulate(proxy, Keys.MouthStretchLeft, mouth.RightStretch);
                Accumulate(proxy, Keys.MouthDimpleLeft, mouth.RightDimple);

                Accumulate(proxy, Keys.MouthClose, mouth.Close);
                Accumulate(proxy, Keys.MouthFunnel, mouth.Funnel);
                Accumulate(proxy, Keys.MouthPucker, mouth.Pucker);
                Accumulate(proxy, Keys.MouthShrugUpper, mouth.ShrugUpper);
                Accumulate(proxy, Keys.MouthShrugLower, mouth.ShrugLower);
                Accumulate(proxy, Keys.MouthRollUpper, mouth.RollUpper);
                Accumulate(proxy, Keys.MouthRollLower, mouth.RollLower);

                //あご
                Accumulate(proxy, Keys.JawOpen, source.Jaw.Open);
                Accumulate(proxy, Keys.JawForward, source.Jaw.Forward);
                Accumulate(proxy, Keys.JawRight, source.Jaw.Left);
                Accumulate(proxy, Keys.JawLeft, source.Jaw.Right);

                //舌
                Accumulate(proxy, Keys.TongueOut, source.Tongue.TongueOut);

                //ほお
                Accumulate(proxy, Keys.CheekPuff, source.Cheek.Puff);
                Accumulate(proxy, Keys.CheekSquintRight, source.Cheek.LeftSquint);
                Accumulate(proxy, Keys.CheekSquintLeft, source.Cheek.RightSquint);                
            }
            else if (writeExcludedKeys)
            {
                //口(多い)
                Accumulate(proxy, Keys.MouthRight, 0);
                Accumulate(proxy, Keys.MouthSmileRight, 0);
                Accumulate(proxy, Keys.MouthFrownRight, 0);
                Accumulate(proxy, Keys.MouthPressRight, 0);
                Accumulate(proxy, Keys.MouthUpperUpRight, 0);
                Accumulate(proxy, Keys.MouthLowerDownRight, 0);
                Accumulate(proxy, Keys.MouthStretchRight, 0);
                Accumulate(proxy, Keys.MouthDimpleRight, 0);

                Accumulate(proxy, Keys.MouthLeft, 0);
                Accumulate(proxy, Keys.MouthSmileLeft, 0);
                Accumulate(proxy, Keys.MouthFrownLeft, 0);
                Accumulate(proxy, Keys.MouthPressLeft, 0);
                Accumulate(proxy, Keys.MouthUpperUpLeft, 0);
                Accumulate(proxy, Keys.MouthLowerDownLeft, 0);
                Accumulate(proxy, Keys.MouthStretchLeft, 0);
                Accumulate(proxy, Keys.MouthDimpleLeft, 0);

                Accumulate(proxy, Keys.MouthClose, 0);
                Accumulate(proxy, Keys.MouthFunnel, 0);
                Accumulate(proxy, Keys.MouthPucker, 0);
                Accumulate(proxy, Keys.MouthShrugUpper, 0);
                Accumulate(proxy, Keys.MouthShrugLower, 0);
                Accumulate(proxy, Keys.MouthRollUpper, 0);
                Accumulate(proxy, Keys.MouthRollLower, 0);

                //あご
                Accumulate(proxy, Keys.JawOpen, 0);
                Accumulate(proxy, Keys.JawForward, 0);
                Accumulate(proxy, Keys.JawRight, 0);
                Accumulate(proxy, Keys.JawLeft, 0);

                //舌
                Accumulate(proxy, Keys.TongueOut, 0);

                //ほお
                Accumulate(proxy, Keys.CheekPuff, 0);
                Accumulate(proxy, Keys.CheekSquintRight, 0);
                Accumulate(proxy, Keys.CheekSquintLeft, 0);
            }            
        }

        private void Accumulate(VRMBlendShapeProxy proxy, BlendShapeKey key, float value)
        {
            ValueAccumulated?.Invoke(key, value);
            //調整をかけるケースについても大半はコッチを通過するので分けておく
            if (!(value > 0f))
            {
                proxy.AccumulateValue(key, 0f);
                return;
            }
            
            if (!EmphasizeExpression)
            {
                proxy.AccumulateValue(key, value);
            }
            else
            {
                float max = _emphasizeSettingsHandler.Items[key].maxValue;
                float adjusted = Mathf.Clamp01(value / max);
                proxy.AccumulateValue(key, adjusted);
#if UNITY_EDITOR
                if (recordOnEditor && value > max)
                {
                    _emphasizeSettingsHandler.Items[key].maxValue = value;
                }
#endif
            }
        }
        
        private void ParseClipCompletenessToSendMessage()
        {
            //やること: パーフェクトシンク用に定義されていてほしいにも関わらず、定義が漏れたクリップがないかチェックする。
            var modelBlendShapeNames = _modelBaseClips.Select(c => c.BlendShapeName).ToArray(); 
            var missedBlendShapeNames = Keys.BlendShapeNames
                .Where(n => !modelBlendShapeNames.Contains(n))
                .ToList();

            if (missedBlendShapeNames.Count > 0)
            {
                _sender.SendCommand(MessageFactory.Instance.ExTrackerSetPerfectSyncMissedClipNames(
                    $"Missing Count: {missedBlendShapeNames.Count} / 52,\n" + 
                    string.Join("\n", missedBlendShapeNames)
                    ));
            }
            else
            {
                //空文字列を送ることでエラーが解消したことを通知する
                _sender.SendCommand(MessageFactory.Instance.ExTrackerSetPerfectSyncMissedClipNames(""));
            }
                
        }
    
        private BlendShapeKey[] LoadNonPerfectSyncKeys()
        {
            var perfectSyncKeys = Keys.PerfectSyncKeys;
            return _modelBaseClips
                .Select(BlendShapeKey.CreateFromClip)
                .Where(key => !perfectSyncKeys.Any(k => k.Preset == key.Preset && k.Name == key.Name))
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

            /// <summary>
            /// Perfect Syncでいじる対象のブレンドシェイプキー名の一覧を、大文字化される前の状態で取得します。
            /// </summary>
            /// <remarks>
            /// UniVRMが0.55.0でも動くようにしてます(0.56.0ならPerfectSyncKeysのKeyのNameとかでも大丈夫)
            /// </remarks>
            public static string[] BlendShapeNames { get; }
            
            /// <summary> Perfect Syncでいじる対象のブレンドシェイプキー一覧を取得します。 </summary>
            public static BlendShapeKey[] PerfectSyncKeys { get; }
            /// <summary> Perfect Syncの口、ほお、顎、舌のキーを取得します。</summary>
            public static BlendShapeKey[] PerfectSyncMouthKeys { get; }
            /// <summary> Perfect Syncの目、鼻、眉のキーを取得します。</summary>
            public static BlendShapeKey[] PerfectSyncNonMouthKeys { get; }
            
            //目
            public static readonly BlendShapeKey EyeBlinkLeft = BlendShapeKey.CreateUnknown(nameof(EyeBlinkLeft));
            public static readonly BlendShapeKey EyeLookUpLeft = BlendShapeKey.CreateUnknown(nameof(EyeLookUpLeft));
            public static readonly BlendShapeKey EyeLookDownLeft = BlendShapeKey.CreateUnknown(nameof(EyeLookDownLeft));
            public static readonly BlendShapeKey EyeLookInLeft = BlendShapeKey.CreateUnknown(nameof(EyeLookInLeft));
            public static readonly BlendShapeKey EyeLookOutLeft = BlendShapeKey.CreateUnknown(nameof(EyeLookOutLeft));
            public static readonly BlendShapeKey EyeWideLeft = BlendShapeKey.CreateUnknown(nameof(EyeWideLeft));
            public static readonly BlendShapeKey EyeSquintLeft = BlendShapeKey.CreateUnknown(nameof(EyeSquintLeft));

            public static readonly BlendShapeKey EyeBlinkRight = BlendShapeKey.CreateUnknown(nameof(EyeBlinkRight));
            public static readonly BlendShapeKey EyeLookUpRight = BlendShapeKey.CreateUnknown(nameof(EyeLookUpRight));
            public static readonly BlendShapeKey EyeLookDownRight = BlendShapeKey.CreateUnknown(nameof(EyeLookDownRight));
            public static readonly BlendShapeKey EyeLookInRight = BlendShapeKey.CreateUnknown(nameof(EyeLookInRight));
            public static readonly BlendShapeKey EyeLookOutRight = BlendShapeKey.CreateUnknown(nameof(EyeLookOutRight));
            public static readonly BlendShapeKey EyeWideRight = BlendShapeKey.CreateUnknown(nameof(EyeWideRight));
            public static readonly BlendShapeKey EyeSquintRight = BlendShapeKey.CreateUnknown(nameof(EyeSquintRight));

            //口(多い)
            public static readonly BlendShapeKey MouthLeft = BlendShapeKey.CreateUnknown(nameof(MouthLeft));
            public static readonly BlendShapeKey MouthSmileLeft = BlendShapeKey.CreateUnknown(nameof(MouthSmileLeft));
            public static readonly BlendShapeKey MouthFrownLeft = BlendShapeKey.CreateUnknown(nameof(MouthFrownLeft));
            public static readonly BlendShapeKey MouthPressLeft = BlendShapeKey.CreateUnknown(nameof(MouthPressLeft));
            public static readonly BlendShapeKey MouthUpperUpLeft = BlendShapeKey.CreateUnknown(nameof(MouthUpperUpLeft));
            public static readonly BlendShapeKey MouthLowerDownLeft = BlendShapeKey.CreateUnknown(nameof(MouthLowerDownLeft));
            public static readonly BlendShapeKey MouthStretchLeft = BlendShapeKey.CreateUnknown(nameof(MouthStretchLeft));
            public static readonly BlendShapeKey MouthDimpleLeft = BlendShapeKey.CreateUnknown(nameof(MouthDimpleLeft));

            public static readonly BlendShapeKey MouthRight = BlendShapeKey.CreateUnknown(nameof(MouthRight));
            public static readonly BlendShapeKey MouthSmileRight = BlendShapeKey.CreateUnknown(nameof(MouthSmileRight));
            public static readonly BlendShapeKey MouthFrownRight = BlendShapeKey.CreateUnknown(nameof(MouthFrownRight));
            public static readonly BlendShapeKey MouthPressRight = BlendShapeKey.CreateUnknown(nameof(MouthPressRight));
            public static readonly BlendShapeKey MouthUpperUpRight = BlendShapeKey.CreateUnknown(nameof(MouthUpperUpRight));
            public static readonly BlendShapeKey MouthLowerDownRight = BlendShapeKey.CreateUnknown(nameof(MouthLowerDownRight));
            public static readonly BlendShapeKey MouthStretchRight = BlendShapeKey.CreateUnknown(nameof(MouthStretchRight));
            public static readonly BlendShapeKey MouthDimpleRight = BlendShapeKey.CreateUnknown(nameof(MouthDimpleRight));
            
            public static readonly BlendShapeKey MouthClose = BlendShapeKey.CreateUnknown(nameof(MouthClose));
            public static readonly BlendShapeKey MouthFunnel = BlendShapeKey.CreateUnknown(nameof(MouthFunnel));
            public static readonly BlendShapeKey MouthPucker = BlendShapeKey.CreateUnknown(nameof(MouthPucker));
            public static readonly BlendShapeKey MouthShrugUpper = BlendShapeKey.CreateUnknown(nameof(MouthShrugUpper));
            public static readonly BlendShapeKey MouthShrugLower = BlendShapeKey.CreateUnknown(nameof(MouthShrugLower));
            public static readonly BlendShapeKey MouthRollUpper = BlendShapeKey.CreateUnknown(nameof(MouthRollUpper));
            public static readonly BlendShapeKey MouthRollLower = BlendShapeKey.CreateUnknown(nameof(MouthRollLower));
            
            //あご
            public static readonly BlendShapeKey JawOpen = BlendShapeKey.CreateUnknown(nameof(JawOpen));
            public static readonly BlendShapeKey JawForward = BlendShapeKey.CreateUnknown(nameof(JawForward));
            public static readonly BlendShapeKey JawLeft = BlendShapeKey.CreateUnknown(nameof(JawLeft));
            public static readonly BlendShapeKey JawRight = BlendShapeKey.CreateUnknown(nameof(JawRight));
            
            //鼻
            public static readonly BlendShapeKey NoseSneerLeft = BlendShapeKey.CreateUnknown(nameof(NoseSneerLeft));
            public static readonly BlendShapeKey NoseSneerRight = BlendShapeKey.CreateUnknown(nameof(NoseSneerRight));

            //ほお
            public static readonly BlendShapeKey CheekPuff = BlendShapeKey.CreateUnknown(nameof(CheekPuff));
            public static readonly BlendShapeKey CheekSquintLeft = BlendShapeKey.CreateUnknown(nameof(CheekSquintLeft));
            public static readonly BlendShapeKey CheekSquintRight = BlendShapeKey.CreateUnknown(nameof(CheekSquintRight));
            
            //舌
            public static readonly BlendShapeKey TongueOut = BlendShapeKey.CreateUnknown(nameof(TongueOut));
            
            //まゆげ
            public static readonly BlendShapeKey BrowDownLeft = BlendShapeKey.CreateUnknown(nameof(BrowDownLeft));
            public static readonly BlendShapeKey BrowOuterUpLeft = BlendShapeKey.CreateUnknown(nameof(BrowOuterUpLeft));
            public static readonly BlendShapeKey BrowDownRight = BlendShapeKey.CreateUnknown(nameof(BrowDownRight));
            public static readonly BlendShapeKey BrowOuterUpRight = BlendShapeKey.CreateUnknown(nameof(BrowOuterUpRight));
            public static readonly BlendShapeKey BrowInnerUp = BlendShapeKey.CreateUnknown(nameof(BrowInnerUp));
        }
        
        /// <summary> ブレンドシェイプの上書き処理で使うための、リップシンクのブレンドシェイプキー </summary>
        struct LipSyncValues
        {
            public LipSyncValues(VRMBlendShapeProxy proxy)
            {
                A = proxy.GetValue(AKey);
                I = proxy.GetValue(IKey);
                U = proxy.GetValue(UKey);
                E = proxy.GetValue(EKey);
                O = proxy.GetValue(OKey);
            }
            public float A { get; set; }
            public float I { get; set; }
            public float U { get; set; }
            public float E { get; set; }
            public float O { get; set; }
            
            public static readonly BlendShapeKey AKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.A); 
            public static readonly BlendShapeKey IKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.I);
            public static readonly BlendShapeKey UKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.U);
            public static readonly BlendShapeKey EKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.E);
            public static readonly BlendShapeKey OKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.O);
        }
    }
}
