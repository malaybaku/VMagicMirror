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
        [Tooltip("VRoidデフォルト設定が使いたいとき、元アバターのClipとこのAvatarのClipを融合させる")]
        [SerializeField] private BlendShapeAvatar vroidDefaultBlendShapeAvatar = null;

        private ExternalTrackerDataSource _externalTracker = null;
        private BlendShapeInitializer _blendShapeInitializer = null;
        private FaceControlConfiguration _faceControlConfig = null;
        private IMessageSender _sender = null;
        //VRoid向けのデフォルト設定に入ってるクリップのうち、パーフェクトシンクの分だけ抜き出したもの
        private List<BlendShapeClip> _vroidDefaultClips = null;

        private VRMBlendShapeProxy _blendShape = null;
        //モデル本来のクリップ一覧
        private List<BlendShapeClip> _modelBaseClips = null;
        //モデル本来のクリップにVRoidのデフォルト設定を書き込んだクリップ一覧
        private List<BlendShapeClip> _modelClipsWithVRoidSetting = null;
        
        private bool _hasModel = false;

        /// <summary> 口周りのクリップもパーフェクトシンクのを適用するかどうか。デフォルトではtrue </summary>
        public bool PreferWriteMouthBlendShape { get; private set; }= true;
        
        /// <summary> VRoidデフォルトの設定を使った場合にモデルに上乗せされるクリップのキー一覧 </summary>
        public BlendShapeKey[] ProgramaticallyAddedVRoidClipKeys { get; private set; } = null;

        /// <summary>  </summary>
        public BlendShapeKey[] NonPerfectSyncKeys { get; private set; } = null;

        private bool _isActive = false;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    RefreshClips();
                }
            }
        }
        
        private bool _useVRoidSetting = false;
        public bool UseVRoidDefaultSetting
        {
            get => _useVRoidSetting;
            set
            {  
                if (_useVRoidSetting != value)
                {
                    _useVRoidSetting = value;
                    RefreshClips();
                }
            } 
        }

        [Inject]
        public void Initialize(
            IMessageReceiver receiver, 
            IMessageSender sender,
            IVRMLoadable vrmLoadable,
            ExternalTrackerDataSource externalTracker,
            BlendShapeInitializer blendShapeInitializer,
            FaceControlConfiguration faceControlConfig
            )
        {
            _sender = sender;
            _externalTracker = externalTracker;
            _blendShapeInitializer = blendShapeInitializer;
            _faceControlConfig = faceControlConfig;

            vrmLoadable.VrmLoaded += info =>
            {
                _blendShape = info.blendShape;
                //参照じゃなくて値コピーしとくことに注意(なにかと安全なので)
                _modelBaseClips = info.blendShape.BlendShapeAvatar.Clips.ToList();
                _modelClipsWithVRoidSetting = CreateClipsWithVRoidDefault();
                ProgramaticallyAddedVRoidClipKeys = LoadAddedClipKeys();
                NonPerfectSyncKeys = LoadNonPerfectSyncKeys();
                _hasModel = true;
                ParseClipCompletenessToSendMessage();
            };

            vrmLoadable.PostVrmLoaded += info =>
            {
                if (IsActive && UseVRoidDefaultSetting)
                {
                    RefreshClips();
                }
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _blendShape = null;
                _modelBaseClips = null;
                _modelClipsWithVRoidSetting = null;
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
                VmmCommands.ExTrackerUseVRoidDefaultForPerfectSync,
                command => UseVRoidDefaultSetting = command.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnableLipSync,
                message => PreferWriteMouthBlendShape = message.ToBoolean()
            );

            //VRoidのデフォルト設定クリップにはAIUEOとかのクリップも入っちゃってるので、
            //それを取り除き、パーフェクトシンク用のだけ残す
            var perfectSyncKeys = Keys.PerfectSyncKeys;
            _vroidDefaultClips = vroidDefaultBlendShapeAvatar.Clips
                .Where(c =>
                {
                    var key = BlendShapeKey.CreateFromClip(c);
                    return
                        c.Preset == BlendShapePreset.Unknown &&
                        perfectSyncKeys.Any(k => k.Name == key.Name && k.Preset == key.Preset);
                })
                .ToList();
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

            //NOTE: とくにVRoidデフォルト設定を使わない場合、本来ほしいブレンドシェイプの一部が定義されてないと
            //「実際にはアバターが持ってないキーを指定してしまう」ということが起きるが、
            //これはBlendShapeMergerのレベルで実質無視してくれるので、気にせず指定しちゃってOK
            var source = _externalTracker.CurrentSource;
            
            if (nonMouthPart)
            {
                //目
                var eye = source.Eye;
                proxy.AccumulateValue(Keys.EyeBlinkLeft, eye.LeftBlink);
                proxy.AccumulateValue(Keys.EyeLookUpLeft, eye.LeftLookUp);
                proxy.AccumulateValue(Keys.EyeLookDownLeft, eye.LeftLookDown);
                proxy.AccumulateValue(Keys.EyeLookInLeft, eye.LeftLookIn);
                proxy.AccumulateValue(Keys.EyeLookOutLeft, eye.LeftLookOut);
                proxy.AccumulateValue(Keys.EyeWideLeft, eye.LeftWide);
                proxy.AccumulateValue(Keys.EyeSquintLeft, eye.LeftSquint);

                proxy.AccumulateValue(Keys.EyeBlinkRight, eye.RightBlink);
                proxy.AccumulateValue(Keys.EyeLookUpRight, eye.RightLookUp);
                proxy.AccumulateValue(Keys.EyeLookDownRight, eye.RightLookDown);
                proxy.AccumulateValue(Keys.EyeLookInRight, eye.RightLookIn);
                proxy.AccumulateValue(Keys.EyeLookOutRight, eye.RightLookOut);
                proxy.AccumulateValue(Keys.EyeWideRight, eye.RightWide);
                proxy.AccumulateValue(Keys.EyeSquintRight, eye.RightSquint);

                //NOTE: 瞬き時の目下げ処理に使うためにセット
                _faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
                _faceControlConfig.AlternativeBlinkR = eye.RightBlink;
                
                
                //鼻
                proxy.AccumulateValue(Keys.NoseSneerLeft, source.Nose.LeftSneer);
                proxy.AccumulateValue(Keys.NoseSneerRight, source.Nose.RightSneer);

                //まゆげ
                proxy.AccumulateValue(Keys.BrowDownLeft, source.Brow.LeftDown);
                proxy.AccumulateValue(Keys.BrowOuterUpLeft, source.Brow.LeftOuterUp);
                proxy.AccumulateValue(Keys.BrowDownRight, source.Brow.RightDown);
                proxy.AccumulateValue(Keys.BrowOuterUpRight, source.Brow.RightOuterUp);
                proxy.AccumulateValue(Keys.BrowInnerUp, source.Brow.InnerUp);
            }
            else if (writeExcludedKeys)
            {
                //目
                proxy.AccumulateValue(Keys.EyeBlinkLeft, 0);
                proxy.AccumulateValue(Keys.EyeLookUpLeft, 0);
                proxy.AccumulateValue(Keys.EyeLookDownLeft, 0);
                proxy.AccumulateValue(Keys.EyeLookInLeft, 0);
                proxy.AccumulateValue(Keys.EyeLookOutLeft, 0);
                proxy.AccumulateValue(Keys.EyeWideLeft, 0);
                proxy.AccumulateValue(Keys.EyeSquintLeft, 0);

                proxy.AccumulateValue(Keys.EyeBlinkRight, 0);
                proxy.AccumulateValue(Keys.EyeLookUpRight, 0);
                proxy.AccumulateValue(Keys.EyeLookDownRight, 0);
                proxy.AccumulateValue(Keys.EyeLookInRight, 0);
                proxy.AccumulateValue(Keys.EyeLookOutRight, 0);
                proxy.AccumulateValue(Keys.EyeWideRight, 0);
                proxy.AccumulateValue(Keys.EyeSquintRight, 0);

                //NOTE: 瞬き時の目下げ処理に使うためにセット...は非適用時は要らない。
                //_faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
                //_faceControlConfig.AlternativeBlinkR = eye.RightBlink;                
                
                //鼻
                proxy.AccumulateValue(Keys.NoseSneerLeft, 0);
                proxy.AccumulateValue(Keys.NoseSneerRight, 0);

                //まゆげ
                proxy.AccumulateValue(Keys.BrowDownLeft, 0);
                proxy.AccumulateValue(Keys.BrowOuterUpLeft, 0);
                proxy.AccumulateValue(Keys.BrowDownRight, 0);
                proxy.AccumulateValue(Keys.BrowOuterUpRight, 0);
                proxy.AccumulateValue(Keys.BrowInnerUp, 0);
            }

            if (mouthPart)
            {
                //口(多い)
                var mouth = source.Mouth;
                proxy.AccumulateValue(Keys.MouthLeft, mouth.Left);
                proxy.AccumulateValue(Keys.MouthSmileLeft, mouth.LeftSmile);
                proxy.AccumulateValue(Keys.MouthFrownLeft, mouth.LeftFrown);
                proxy.AccumulateValue(Keys.MouthPressLeft, mouth.LeftPress);
                proxy.AccumulateValue(Keys.MouthUpperUpLeft, mouth.LeftUpperUp);
                proxy.AccumulateValue(Keys.MouthLowerDownLeft, mouth.LeftLowerDown);
                proxy.AccumulateValue(Keys.MouthStretchLeft, mouth.LeftStretch);
                proxy.AccumulateValue(Keys.MouthDimpleLeft, mouth.LeftDimple);

                proxy.AccumulateValue(Keys.MouthRight, mouth.Right);
                proxy.AccumulateValue(Keys.MouthSmileRight, mouth.RightSmile);
                proxy.AccumulateValue(Keys.MouthFrownRight, mouth.RightFrown);
                proxy.AccumulateValue(Keys.MouthPressRight, mouth.RightPress);
                proxy.AccumulateValue(Keys.MouthUpperUpRight, mouth.RightUpperUp);
                proxy.AccumulateValue(Keys.MouthLowerDownRight, mouth.RightLowerDown);
                proxy.AccumulateValue(Keys.MouthStretchRight, mouth.RightStretch);
                proxy.AccumulateValue(Keys.MouthDimpleRight, mouth.RightDimple);

                proxy.AccumulateValue(Keys.MouthClose, mouth.Close);
                proxy.AccumulateValue(Keys.MouthFunnel, mouth.Funnel);
                proxy.AccumulateValue(Keys.MouthPucker, mouth.Pucker);
                proxy.AccumulateValue(Keys.MouthShrugUpper, mouth.ShrugUpper);
                proxy.AccumulateValue(Keys.MouthShrugLower, mouth.ShrugLower);
                proxy.AccumulateValue(Keys.MouthRollUpper, mouth.RollUpper);
                proxy.AccumulateValue(Keys.MouthRollLower, mouth.RollLower);

                //あご
                proxy.AccumulateValue(Keys.JawOpen, source.Jaw.Open);
                proxy.AccumulateValue(Keys.JawForward, source.Jaw.Forward);
                proxy.AccumulateValue(Keys.JawLeft, source.Jaw.Left);
                proxy.AccumulateValue(Keys.JawRight, source.Jaw.Right);

                //舌
                proxy.AccumulateValue(Keys.TongueOut, source.Tongue.TongueOut);

                //ほお
                proxy.AccumulateValue(Keys.CheekPuff, source.Cheek.Puff);
                proxy.AccumulateValue(Keys.CheekSquintLeft, source.Cheek.LeftSquint);
                proxy.AccumulateValue(Keys.CheekSquintRight, source.Cheek.RightSquint);                
            }
            else if (writeExcludedKeys)
            {
                //口(多い)
                proxy.AccumulateValue(Keys.MouthLeft, 0);
                proxy.AccumulateValue(Keys.MouthSmileLeft, 0);
                proxy.AccumulateValue(Keys.MouthFrownLeft, 0);
                proxy.AccumulateValue(Keys.MouthPressLeft, 0);
                proxy.AccumulateValue(Keys.MouthUpperUpLeft, 0);
                proxy.AccumulateValue(Keys.MouthLowerDownLeft, 0);
                proxy.AccumulateValue(Keys.MouthStretchLeft, 0);
                proxy.AccumulateValue(Keys.MouthDimpleLeft, 0);

                proxy.AccumulateValue(Keys.MouthRight, 0);
                proxy.AccumulateValue(Keys.MouthSmileRight, 0);
                proxy.AccumulateValue(Keys.MouthFrownRight, 0);
                proxy.AccumulateValue(Keys.MouthPressRight, 0);
                proxy.AccumulateValue(Keys.MouthUpperUpRight, 0);
                proxy.AccumulateValue(Keys.MouthLowerDownRight, 0);
                proxy.AccumulateValue(Keys.MouthStretchRight, 0);
                proxy.AccumulateValue(Keys.MouthDimpleRight, 0);

                proxy.AccumulateValue(Keys.MouthClose, 0);
                proxy.AccumulateValue(Keys.MouthFunnel, 0);
                proxy.AccumulateValue(Keys.MouthPucker, 0);
                proxy.AccumulateValue(Keys.MouthShrugUpper, 0);
                proxy.AccumulateValue(Keys.MouthShrugLower, 0);
                proxy.AccumulateValue(Keys.MouthRollUpper, 0);
                proxy.AccumulateValue(Keys.MouthRollLower, 0);

                //あご
                proxy.AccumulateValue(Keys.JawOpen, 0);
                proxy.AccumulateValue(Keys.JawForward, 0);
                proxy.AccumulateValue(Keys.JawLeft, 0);
                proxy.AccumulateValue(Keys.JawRight, 0);

                //舌
                proxy.AccumulateValue(Keys.TongueOut, 0);

                //ほお
                proxy.AccumulateValue(Keys.CheekPuff, 0);
                proxy.AccumulateValue(Keys.CheekSquintLeft, 0);
                proxy.AccumulateValue(Keys.CheekSquintRight, 0);
            }
        }

        
        private void RefreshClips()
        {
            if (!_hasModel)
            {
                return;
            }
            
            //差し替え前後で表情が崩れないよう完全にリセット
            _blendShape.Apply();
            _blendShapeInitializer.InitializeBlendShapes();
            _blendShape.Apply();

            _blendShape.BlendShapeAvatar.Clips = IsActive && UseVRoidDefaultSetting
                ? _modelClipsWithVRoidSetting.ToList()
                : _modelBaseClips.ToList();

            //BlendShapeInitializerは切り替わったあとのClip一覧を理解しているべき。
            //そうじゃないとFaceSwitchとかWord to Motionとの組み合わせで破綻するため。
            _blendShapeInitializer.ReloadClips();
            
            //TODO: このリロードの実装のためにUniVRMを書き換えているが、本当は書き換えたくない…
            _blendShape.ReloadBlendShape();
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
        
        private List<BlendShapeClip> CreateClipsWithVRoidDefault()
        {
            var overwriteClipKeys = Keys.PerfectSyncKeys;
            return _modelBaseClips
                .Where(c =>
                {
                    var key = BlendShapeKey.CreateFromClip(c);
                    return !overwriteClipKeys.Contains(key);
                })
                .Concat(_vroidDefaultClips)
                .ToList();
        }

        private BlendShapeKey[] LoadAddedClipKeys()
        {
            var baseKeys = _modelBaseClips
                .Select(BlendShapeKey.CreateFromClip)
                .ToArray();
            return _modelClipsWithVRoidSetting
                .Select(BlendShapeKey.CreateFromClip)
                .Where(key => !baseKeys.Any(k => k.Preset == key.Preset && k.Name == key.Name))
                .ToArray();
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
            
            //TODO: 名前はあとで調べて直すこと！絶対に間違った名前が入ってるぞ！
            
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
