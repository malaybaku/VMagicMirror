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
        private bool _canOverwriteMouthBlendShape = false;

        private VRMBlendShapeProxy _blendShape = null;
        //モデル本来のクリップ一覧
        private List<BlendShapeClip> _modelBaseClips = null;
        //モデル本来のクリップにVRoidのデフォルト設定を書き込んだクリップ一覧
        private List<BlendShapeClip> _modelClipsWithVRoidSetting = null;
        private bool _hasModel = false;

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
                _hasModel = true;
                ParseClipCompletenessToSendMessage();
            };

            vrmLoadable.PostVrmLoaded += info => RefreshClips();

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
                message => _canOverwriteMouthBlendShape = message.ToBoolean()
            );

            //VRoidのデフォルト設定クリップにはAIUEOとかのクリップも入っちゃってるので、
            //それを取り除き、パーフェクトシンク用のだけ残す
            var perfectSyncKeys = Keys.PerfectSyncKeys;
            _vroidDefaultClips = vroidDefaultBlendShapeAvatar.Clips
                .Where(c =>
                {
                    var key = BlendShapeKey.CreateFrom(c);
                    return
                        c.Preset == BlendShapePreset.Unknown &&
                        perfectSyncKeys.Any(k => k.Name == key.Name && k.Preset == key.Preset);
                })
                .ToList();
        }

        private void LateUpdate()
        {
            if (_hasModel && IsActive && _externalTracker.Connected)
            {
                MapToBlendShapeClips();
            }
        }
        
        /// <summary> 外部トラッキングで取得したブレンドシェイプをアバターに反映します。 </summary>
        private void MapToBlendShapeClips()
        {
            //現行ブレンドシェイプを捨てて上書きするが、
            //もしマイクベースのリップシンクが優先ならそちらを勝たせて、口まわりの形は保持してあげる
            _blendShape.Apply();
            
            var lipSyncValues = new LipSyncValues(_blendShape);
            //NOTE: ブレンドシェイプが色々と競合するリスクがあるので、リップシンクを残す場合もいったん全部ゼロ埋めする
            _blendShapeInitializer.InitializeBlendShapes(false);
            
            //NOTE: とくにVRoidデフォルト設定を使わない場合、本来ほしいブレンドシェイプの一部が定義されてないと
            //「実際にはアバターが持ってないキーを指定してしまう」ということが起きるが、
            //これはBlendShapeMergerのレベルで実質無視してくれるので、気にせず指定しちゃってOK
            var source = _externalTracker.CurrentSource;
            
            //目
            var eye = source.Eye;
            _blendShape.AccumulateValue(Keys.EyeBlinkLeft, eye.LeftBlink);
            _blendShape.AccumulateValue(Keys.EyeLookUpLeft, eye.LeftLookUp);
            _blendShape.AccumulateValue(Keys.EyeLookDownLeft, eye.LeftLookDown);
            _blendShape.AccumulateValue(Keys.EyeLookInLeft, eye.LeftLookIn);
            _blendShape.AccumulateValue(Keys.EyeLookOutLeft, eye.LeftLookOut);
            _blendShape.AccumulateValue(Keys.EyeWideLeft, eye.LeftWide);
            _blendShape.AccumulateValue(Keys.EyeSquintLeft, eye.LeftSquint);

            _blendShape.AccumulateValue(Keys.EyeBlinkRight, eye.RightBlink);
            _blendShape.AccumulateValue(Keys.EyeLookUpRight, eye.RightLookUp);
            _blendShape.AccumulateValue(Keys.EyeLookDownRight, eye.RightLookDown);
            _blendShape.AccumulateValue(Keys.EyeLookInRight, eye.RightLookIn);
            _blendShape.AccumulateValue(Keys.EyeLookOutRight, eye.RightLookOut);
            _blendShape.AccumulateValue(Keys.EyeWideRight, eye.RightWide);
            _blendShape.AccumulateValue(Keys.EyeSquintRight, eye.RightSquint);

            //NOTE: 瞬き時の目下げ処理に使うためにセット
            _faceControlConfig.AlternativeBlinkL = eye.LeftBlink;
            _faceControlConfig.AlternativeBlinkR = eye.RightBlink;
            
            
            //鼻
            _blendShape.AccumulateValue(Keys.NoseSneerLeft, source.Nose.LeftSneer);
            _blendShape.AccumulateValue(Keys.NoseSneerRight, source.Nose.RightSneer);

            //まゆげ
            _blendShape.AccumulateValue(Keys.BrowDownLeft, source.Brow.LeftDown);
            _blendShape.AccumulateValue(Keys.BrowOuterUpLeft, source.Brow.LeftOuterUp);
            _blendShape.AccumulateValue(Keys.BrowDownRight, source.Brow.RightDown);
            _blendShape.AccumulateValue(Keys.BrowOuterUpRight, source.Brow.RightOuterUp);
            _blendShape.AccumulateValue(Keys.BrowInnerUp, source.Brow.InnerUp);

            // 口、顎、頬はどれもマイクリップシンクと競合リスクがあるので、口までPerfect Syncのときだけやっておく
            if (_canOverwriteMouthBlendShape)
            {
                //口(多い)
                var mouth = source.Mouth;
                _blendShape.AccumulateValue(Keys.MouthLeft, mouth.Left);
                _blendShape.AccumulateValue(Keys.MouthSmileLeft, mouth.LeftSmile);
                _blendShape.AccumulateValue(Keys.MouthFrownLeft, mouth.LeftFrown);
                _blendShape.AccumulateValue(Keys.MouthPressLeft, mouth.LeftPress);
                _blendShape.AccumulateValue(Keys.MouthUpperUpLeft, mouth.LeftUpperUp);
                _blendShape.AccumulateValue(Keys.MouthLowerDownLeft, mouth.LeftLowerDown);
                _blendShape.AccumulateValue(Keys.MouthStretchLeft, mouth.LeftStretch);
                _blendShape.AccumulateValue(Keys.MouthDimpleLeft, mouth.LeftDimple);

                _blendShape.AccumulateValue(Keys.MouthRight, mouth.Right);
                _blendShape.AccumulateValue(Keys.MouthSmileRight, mouth.RightSmile);
                _blendShape.AccumulateValue(Keys.MouthFrownRight, mouth.RightFrown);
                _blendShape.AccumulateValue(Keys.MouthPressRight, mouth.RightPress);
                _blendShape.AccumulateValue(Keys.MouthUpperUpRight, mouth.RightUpperUp);
                _blendShape.AccumulateValue(Keys.MouthLowerDownRight, mouth.RightLowerDown);
                _blendShape.AccumulateValue(Keys.MouthStretchRight, mouth.RightStretch);
                _blendShape.AccumulateValue(Keys.MouthDimpleRight, mouth.RightDimple);

                _blendShape.AccumulateValue(Keys.MouthClose, mouth.Close);
                _blendShape.AccumulateValue(Keys.MouthFunnel, mouth.Funnel);
                _blendShape.AccumulateValue(Keys.MouthPucker, mouth.Pucker);
                _blendShape.AccumulateValue(Keys.MouthShrugUpper, mouth.ShrugUpper);
                _blendShape.AccumulateValue(Keys.MouthShrugLower, mouth.ShrugLower);
                _blendShape.AccumulateValue(Keys.MouthRollUpper, mouth.RollUpper);
                _blendShape.AccumulateValue(Keys.MouthRollLower, mouth.RollLower);

                //あご
                _blendShape.AccumulateValue(Keys.JawOpen, source.Jaw.Open);
                _blendShape.AccumulateValue(Keys.JawForward, source.Jaw.Forward);
                _blendShape.AccumulateValue(Keys.JawLeft, source.Jaw.Left);
                _blendShape.AccumulateValue(Keys.JawRight, source.Jaw.Right);

                //舌
                _blendShape.AccumulateValue(Keys.TongueOut, source.Tongue.TongueOut);

                //ほお
                _blendShape.AccumulateValue(Keys.CheekPuff, source.Cheek.Puff);
                _blendShape.AccumulateValue(Keys.CheekSquintLeft, source.Cheek.LeftSquint);
                _blendShape.AccumulateValue(Keys.CheekSquintRight, source.Cheek.RightSquint);
            }
            else
            {
                //リップシンクの維持は一旦ゼロに戻してから同じ値を入れ直すことで実現する。
                //こうするとClip間でブレンドシェイプが干渉していても正しく動くので、
                //特にVRoid + VRoidデフォルト設定の組み合わせで動きがよい。
                _blendShape.AccumulateValue(LipSyncValues.AKey, lipSyncValues.A);
                _blendShape.AccumulateValue(LipSyncValues.IKey, lipSyncValues.I);
                _blendShape.AccumulateValue(LipSyncValues.UKey, lipSyncValues.U);
                _blendShape.AccumulateValue(LipSyncValues.EKey, lipSyncValues.E);
                _blendShape.AccumulateValue(LipSyncValues.OKey, lipSyncValues.O);
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
            _blendShapeInitializer.InitializeBlendShapes(false);
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
                    var key = new BlendShapeKey(c.BlendShapeName, c.Preset);
                    return !overwriteClipKeys.Contains(key);
                })
                .Concat(_vroidDefaultClips)
                .ToList();
        }
        
        /// <summary> 決め打ちされた、パーフェクトシンクで使うブレンドシェイプの一覧 </summary>
        static class Keys
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
            
            //TODO: 名前はあとで調べて直すこと！絶対に間違った名前が入ってるぞ！
            
            //目
            public static readonly BlendShapeKey EyeBlinkLeft = new BlendShapeKey(nameof(EyeBlinkLeft));
            public static readonly BlendShapeKey EyeLookUpLeft = new BlendShapeKey(nameof(EyeLookUpLeft));
            public static readonly BlendShapeKey EyeLookDownLeft = new BlendShapeKey(nameof(EyeLookDownLeft));
            public static readonly BlendShapeKey EyeLookInLeft = new BlendShapeKey(nameof(EyeLookInLeft));
            public static readonly BlendShapeKey EyeLookOutLeft = new BlendShapeKey(nameof(EyeLookOutLeft));
            public static readonly BlendShapeKey EyeWideLeft = new BlendShapeKey(nameof(EyeWideLeft));
            public static readonly BlendShapeKey EyeSquintLeft = new BlendShapeKey(nameof(EyeSquintLeft));

            public static readonly BlendShapeKey EyeBlinkRight = new BlendShapeKey(nameof(EyeBlinkRight));
            public static readonly BlendShapeKey EyeLookUpRight = new BlendShapeKey(nameof(EyeLookUpRight));
            public static readonly BlendShapeKey EyeLookDownRight = new BlendShapeKey(nameof(EyeLookDownRight));
            public static readonly BlendShapeKey EyeLookInRight = new BlendShapeKey(nameof(EyeLookInRight));
            public static readonly BlendShapeKey EyeLookOutRight = new BlendShapeKey(nameof(EyeLookOutRight));
            public static readonly BlendShapeKey EyeWideRight = new BlendShapeKey(nameof(EyeWideRight));
            public static readonly BlendShapeKey EyeSquintRight = new BlendShapeKey(nameof(EyeSquintRight));

            //口(多い)
            public static readonly BlendShapeKey MouthLeft = new BlendShapeKey(nameof(MouthLeft));
            public static readonly BlendShapeKey MouthSmileLeft = new BlendShapeKey(nameof(MouthSmileLeft));
            public static readonly BlendShapeKey MouthFrownLeft = new BlendShapeKey(nameof(MouthFrownLeft));
            public static readonly BlendShapeKey MouthPressLeft = new BlendShapeKey(nameof(MouthPressLeft));
            public static readonly BlendShapeKey MouthUpperUpLeft = new BlendShapeKey(nameof(MouthUpperUpLeft));
            public static readonly BlendShapeKey MouthLowerDownLeft = new BlendShapeKey(nameof(MouthLowerDownLeft));
            public static readonly BlendShapeKey MouthStretchLeft = new BlendShapeKey(nameof(MouthStretchLeft));
            public static readonly BlendShapeKey MouthDimpleLeft = new BlendShapeKey(nameof(MouthDimpleLeft));

            public static readonly BlendShapeKey MouthRight = new BlendShapeKey(nameof(MouthRight));
            public static readonly BlendShapeKey MouthSmileRight = new BlendShapeKey(nameof(MouthSmileRight));
            public static readonly BlendShapeKey MouthFrownRight = new BlendShapeKey(nameof(MouthFrownRight));
            public static readonly BlendShapeKey MouthPressRight = new BlendShapeKey(nameof(MouthPressRight));
            public static readonly BlendShapeKey MouthUpperUpRight = new BlendShapeKey(nameof(MouthUpperUpRight));
            public static readonly BlendShapeKey MouthLowerDownRight = new BlendShapeKey(nameof(MouthLowerDownRight));
            public static readonly BlendShapeKey MouthStretchRight = new BlendShapeKey(nameof(MouthStretchRight));
            public static readonly BlendShapeKey MouthDimpleRight = new BlendShapeKey(nameof(MouthDimpleRight));
            
            public static readonly BlendShapeKey MouthClose = new BlendShapeKey(nameof(MouthClose));
            public static readonly BlendShapeKey MouthFunnel = new BlendShapeKey(nameof(MouthFunnel));
            public static readonly BlendShapeKey MouthPucker = new BlendShapeKey(nameof(MouthPucker));
            public static readonly BlendShapeKey MouthShrugUpper = new BlendShapeKey(nameof(MouthShrugUpper));
            public static readonly BlendShapeKey MouthShrugLower = new BlendShapeKey(nameof(MouthShrugLower));
            public static readonly BlendShapeKey MouthRollUpper = new BlendShapeKey(nameof(MouthRollUpper));
            public static readonly BlendShapeKey MouthRollLower = new BlendShapeKey(nameof(MouthRollLower));
            
            //あご
            public static readonly BlendShapeKey JawOpen = new BlendShapeKey(nameof(JawOpen));
            public static readonly BlendShapeKey JawForward = new BlendShapeKey(nameof(JawForward));
            public static readonly BlendShapeKey JawLeft = new BlendShapeKey(nameof(JawLeft));
            public static readonly BlendShapeKey JawRight = new BlendShapeKey(nameof(JawRight));
            
            //鼻
            public static readonly BlendShapeKey NoseSneerLeft = new BlendShapeKey(nameof(NoseSneerLeft));
            public static readonly BlendShapeKey NoseSneerRight = new BlendShapeKey(nameof(NoseSneerRight));

            //ほお
            public static readonly BlendShapeKey CheekPuff = new BlendShapeKey(nameof(CheekPuff));
            public static readonly BlendShapeKey CheekSquintLeft = new BlendShapeKey(nameof(CheekSquintLeft));
            public static readonly BlendShapeKey CheekSquintRight = new BlendShapeKey(nameof(CheekSquintRight));
            
            //舌
            public static readonly BlendShapeKey TongueOut = new BlendShapeKey(nameof(TongueOut));
            
            //まゆげ
            public static readonly BlendShapeKey BrowDownLeft = new BlendShapeKey(nameof(BrowDownLeft));
            public static readonly BlendShapeKey BrowOuterUpLeft = new BlendShapeKey(nameof(BrowOuterUpLeft));
            public static readonly BlendShapeKey BrowDownRight = new BlendShapeKey(nameof(BrowDownRight));
            public static readonly BlendShapeKey BrowOuterUpRight = new BlendShapeKey(nameof(BrowOuterUpRight));
            public static readonly BlendShapeKey BrowInnerUp = new BlendShapeKey(nameof(BrowInnerUp));
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
            
            public static readonly BlendShapeKey AKey = new BlendShapeKey(BlendShapePreset.A);
            public static readonly BlendShapeKey IKey = new BlendShapeKey(BlendShapePreset.I);
            public static readonly BlendShapeKey UKey = new BlendShapeKey(BlendShapePreset.U);
            public static readonly BlendShapeKey EKey = new BlendShapeKey(BlendShapePreset.E);
            public static readonly BlendShapeKey OKey = new BlendShapeKey(BlendShapePreset.O);
        }
    }
}
