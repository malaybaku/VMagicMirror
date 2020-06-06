using System;
using System.Collections.Generic;
using System.Linq;
using Baku.VMagicMirror.ExternalTracker.iFacialMocap;
using UnityEngine;
using Zenject;
using VRM;

namespace Baku.VMagicMirror.ExternalTracker
{
    public class FaceTrackToPose : MonoBehaviour
    {
        //NOTE: とりあえずデフォルトセットは適用しますよ、というコンセプトだが、AIUEOとかは鬼門なので触りたくないんですよね正直。
        private static readonly BlendShapePreset[] _targetBlendShapePresets = new[]
        {
            BlendShapePreset.Blink_L,
            BlendShapePreset.Blink_R,

            BlendShapePreset.LookLeft,
            BlendShapePreset.LookRight,
            BlendShapePreset.LookUp,
            BlendShapePreset.LookDown,
            
            BlendShapePreset.A,
            BlendShapePreset.I,
            BlendShapePreset.U,
            BlendShapePreset.E,
            BlendShapePreset.O,

            BlendShapePreset.Joy,
            BlendShapePreset.Angry,
            BlendShapePreset.Sorrow,
            BlendShapePreset.Fun,
        };

        private static readonly Dictionary<BlendShapePreset, BlendShapeKey> _targetBlendShapes
            = _targetBlendShapePresets.ToDictionary(k => k, k => new BlendShapeKey(k));

        //特別な2つのキーです
        private static readonly BlendShapeKey _surprisedKey = new BlendShapeKey("Surprised");
        private static readonly BlendShapeKey _jitomeKey = new BlendShapeKey("Jitome");


        //NOTE: ここらへん実装クラスの取り扱いどうするか考えたい所。MessageReceiverの層で世話してもらう構成もアリかな？
        [SerializeField] private iFacialMocapReceiver iFacialMocapReceiver = null;
        


        #region 事前に設定しておくやつら
        
        [Tooltip("瞳の動きをブレンドシェイプじゃなくてボーンでやってほしい場合はtrueにする")]
        [SerializeField] private bool preferEyeBone = false;
        [SerializeField] private Vector2 eyeBoneRotFactor = new Vector2(5.0f, 5.0f);

        [Range(0f, 1f)] [SerializeField] private float joyMouthThreshold = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float sorrowBrowThreshold = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float angryBrowDownThreshold = 0.4f;
        //NOTE: funはな…joyと区別がつかないのじゃ…
        [Range(0f, 1f)] [SerializeField] private float surpriseEyeWideThreshold = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float jitomeEyeSquintThreshold = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float cheekPuffThreshold = 0.6f;
        
        #endregion
        
        #region 顔情報でばきばき更新されるやつら

        // NOTE: ここの7つは後述のスイッチが立ってないときに適用される感じのやつ

        [Range(0f, 1f)] [SerializeField] private float latestLeftBlink = 0f;
        [Range(0f, 1f)] [SerializeField] private float latestRightBlink = 0f;
        [Range(0f, 1f)] [SerializeField] private float latestMouthOpen = 0f;

        [Range(0f, 1f)] [SerializeField] private float latestLookLeft = 0f;
        [Range(0f, 1f)] [SerializeField] private float latestLookRight = 0f;
        [Range(0f, 1f)] [SerializeField] private float latestLookUp = 0f;
        [Range(0f, 1f)] [SerializeField] private float latestLookDown = 0f;

        [SerializeField] private bool joySwitch = false;
        [SerializeField] private bool angrySwitch = false;
        [SerializeField] private bool sorrowSwitch = false;
        [SerializeField] private bool funSwitch = false;
        [SerializeField] private bool surprisedSwitch = false;
        [SerializeField] private bool jitomeSwitch = false;
        
        [SerializeField] private Vector3 facePosition = Vector3.zero;
        //NOTE: なぜangle/axisかというと、angleがRadian単位だったりaxisの軸方向がUnityと違ったりして
        [SerializeField] private float faceRotationAngle = 0;
        [SerializeField] private Vector3 faceRotationAxis = Vector3.right;
        
        #endregion
        
        public Vector3 FaceRelativePosition => facePosition;
        public float FaceRotationAngle => faceRotationAngle;
        public Vector3 FaceRotationAxis => faceRotationAxis;

        public VRMBlendShapeProxy BlendShapeProxy
        {
            get => blendShapeProxy;
            set => blendShapeProxy = value;
        }

        private VRMBlendShapeProxy blendShapeProxy = null;

        private bool _hasSurprisedClip = false;
        private bool _hasJitomeClip = false;
        private bool _isEyeBoneReady = false;
        private Transform _leftEyeBone = null;
        private Transform _rightEyeBone = null;
        
        private void Start()
        {
            _hasSurprisedClip = BlendShapeProxy
                .BlendShapeAvatar
                .Clips
                .Any(
                    c => string.Compare(
                        c.BlendShapeName,
                        "surprised",
                        StringComparison.InvariantCultureIgnoreCase
                    ) == 0
                );
            
            _hasJitomeClip = BlendShapeProxy
                .BlendShapeAvatar
                .Clips
                .Any(
                    c => string.Compare(
                        c.BlendShapeName,
                        "jitome",
                        StringComparison.InvariantCultureIgnoreCase
                    ) == 0
                );

            if (preferEyeBone &&
                blendShapeProxy.gameObject.GetComponent<Animator>() is Animator animator)
            {
                _leftEyeBone = animator.GetBoneTransform(HumanBodyBones.LeftEye);
                _rightEyeBone = animator.GetBoneTransform(HumanBodyBones.RightEye);
                _isEyeBoneReady = (_leftEyeBone != null) && (_rightEyeBone != null);
            }
        }

        
        private void Update()
        {
            var proxy = BlendShapeProxy;
            if (proxy == null)
            {
                Debug.Log("non blend shape");
                return;
            }
            
            if (TryApplyExclusiveBlendShape(proxy))
            {
                AccumulateZeroForNormalBlendShape(proxy);
            }
            else
            {
                AccumulateNormalBlendShape(proxy);
            }
            proxy.Apply();

            UpdateEyeBones();
        }

        //排他適用すべき表情の適用フラグが立っているとき、それにパッと切り替える
        private bool TryApplyExclusiveBlendShape(VRMBlendShapeProxy proxy)
        {
            bool result = false;
            
            //POINT:
            //複数の条件を同時に満たしているとき、最初のだけ適用し、それ以外は適用しない状態に落とし込む。
            //0を明示的にAccumulateするために少し凝った書き方になっています。
            
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Joy], (!result && joySwitch) ? 1 : 0);
            result = result || joySwitch;
            
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Sorrow], (!result && sorrowSwitch) ? 1 : 0);
            result = result || sorrowSwitch;

            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Angry], (!result && angrySwitch) ? 1 : 0);
            result = result || angrySwitch;

            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Fun], (!result && funSwitch) ? 1 : 0);
            result = result || funSwitch;

            if (_hasSurprisedClip)
            {
                proxy.AccumulateValue(_surprisedKey, (!result && surprisedSwitch) ? 1 : 0);
                result = result || surprisedSwitch;
            }

            if (_hasJitomeClip)
            {
                proxy.AccumulateValue(_jitomeKey, (!result && jitomeSwitch) ? 1 : 0);
                result = result || jitomeSwitch;
            }

            return result;
        }

        //同時に適用できるタイプの表情を指定する
        private void AccumulateNormalBlendShape(VRMBlendShapeProxy proxy)
        {
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Blink_L], latestLeftBlink);
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Blink_R], latestRightBlink);
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.A], latestMouthOpen);

            //NOTE: ボーンなら排他ブレンドシェイプと同時に適用しても害になりづらそうだけどブレンドシェイプは怖いな～という意識です
            if (!preferEyeBone)
            {
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookLeft], latestLookLeft);
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookRight], latestLookRight);
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookUp], latestLookUp);
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookDown], latestLookDown);
            }
        }

        //排他適用ブレンドシェイプのために、Normalのほうのブレンドシェイプ値をゼロに戻す
        private void AccumulateZeroForNormalBlendShape(VRMBlendShapeProxy proxy)
        {
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Blink_L], 0);
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.Blink_R], 0);
            proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.A], 0);

            //NOTE: ボーンなら排他ブレンドシェイプと同時に適用しても害になりづらそうだけどブレンドシェイプは怖いな～という意識です
            if (!preferEyeBone)
            {
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookLeft], 0);
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookRight], 0);
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookUp], 0);
                proxy.AccumulateValue(_targetBlendShapes[BlendShapePreset.LookDown], 0);
            }
        }
        
        //目のブレンドシェイプ値をボーン回転として適用する
        private void UpdateEyeBones()
        {
            if (!_isEyeBoneReady)
            {
                return;
            }

            //x回転 = タテの上下、下向きがプラスなのでLookUpを引くことに注意
            //y回転 = ヨコ、右向きプラスなので普通にRight-Leftでよい
            var localRot = Quaternion.Euler(
                (latestLookDown - latestLookUp) * eyeBoneRotFactor.y,
                (latestLookRight - latestLookLeft) * eyeBoneRotFactor.x,
                0
            );

            _leftEyeBone.localRotation = localRot;
            _rightEyeBone.localRotation = localRot;
        }

    }
}
