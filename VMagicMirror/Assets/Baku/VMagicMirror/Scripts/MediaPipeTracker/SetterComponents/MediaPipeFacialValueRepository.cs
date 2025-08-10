using System.Collections.Generic;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    using Keys = MediaPipeBlendShapeKeys;

    /// <summary>
    /// Mediapipe Face Landmarkerで取得した表情の情報を保持するクラス。データが古くなると自動で捨てる実装も入っている
    /// </summary>
    public class MediaPipeFacialValueRepository : PresenterBase, ITickable
    {
        // NOTE: PoseSetterSettingで定義してもいいけど、あんまり可変にするビジョンが見えないのでconstで
        private const float FaceResetLerpFactor = 6f;

        private readonly FaceControlConfiguration _faceControlConfig;
        private readonly MediapipePoseSetterSettings _settings;
        private readonly object _dataLock = new();

        private readonly CounterBoolState _isTracked = new(3, 5);
        private float _trackLostTime;

        private readonly RecordFaceTrackBlendShapes _blendShapes = new();
        // NOTE: getterにマルチスレッド対策がないのは意図的。最悪ケースでも値が多少古い程度で済むはずなのでいい加減にしてある
        public IFaceTrackBlendShapes BlendShapes => _blendShapes;
        
        private readonly MediaPipeCorrectedBlendShapes _correctedBlendShapes;
        // NOTE: PerfectSyncをアバターに適用するとき用にオプションの補正を適用した値が入っている
        public IFaceTrackBlendShapes CorrectedBlendShapes => _correctedBlendShapes;
        
        [Inject]
        public MediaPipeFacialValueRepository(
            FaceControlConfiguration faceControlConfig,
            MediapipePoseSetterSettings settings,
            MediaPipeTrackerRuntimeSettingsRepository runtimeSettings)
        {
            _faceControlConfig = faceControlConfig;
            _settings = settings;
            
            _correctedBlendShapes = new MediaPipeCorrectedBlendShapes(_blendShapes, runtimeSettings);
        }

        public bool IsTracked
        {
            get
            {
                lock (_dataLock)
                {
                    return _isTracked.Value;
                }
            }
        }
        
        /// <summary>
        /// NOTE: <see cref="MediaPipeBlendShapeKeys"/> で定義されたキーを指定すること。
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public void SetValues(IEnumerable<KeyValuePair<string, float>> values)
        {
            lock (_dataLock)
            {
                // NOTE: 前提として (valuesのキー一覧) == (_valuesのキー一覧) であることを期待してるが、
                // パフォーマンス的にそのチェックをしたくないので省く
                foreach (var (keyName, value) in values)
                {
                    SetValue(keyName, value);
                }
                _correctedBlendShapes.UpdateEye();
                
                _isTracked.Set(true);
                _trackLostTime = 0f;
            }
        }

        /// <summary>
        /// NOTE: 画像の解析結果として顔が検出できなかったときに呼び出す。
        /// このメソッドを続けて何度か呼び出すことで、実際にトラッキングロストしたものと見なされる。
        /// </summary>
        public void RequestReset()
        {
            lock (_dataLock)
            {
                _isTracked.Set(false);
            }
        }

        public override void Initialize()
        {
            // 書いてる通りだが、MediaPipeを使わない状態に切り替わる場合は直ちにブレンドシェイプをリセットしておく。
            // これにより、webカメラを使ってないときのTickの処理がちょっと減る
            _faceControlConfig.FaceControlMode
                .Where(mode => mode is FaceControlModes.WebCamHighPower)
                .Subscribe(_ =>
                {
                    ResetFacialValues(0f);
                    _correctedBlendShapes.UpdateEye();
                })
                .AddTo(this);
        }

        void ITickable.Tick()
        {
            lock (_dataLock)
            {
                _trackLostTime += Time.deltaTime;
                if (_isTracked.Value && _trackLostTime >= _settings.TrackingLostTimeThreshold)
                {
                    _isTracked.Reset(false);
                }

                // トラッキングロス時はゆっくり値をゼロに戻す
                if (_faceControlConfig.ControlMode is FaceControlModes.WebCamHighPower &&
                    !_isTracked.Value &&
                    _trackLostTime > _settings.TrackingLostPoseAndFacialResetWait)
                {
                    ResetFacialValues(1 - FaceResetLerpFactor * Time.deltaTime);
                    _correctedBlendShapes.UpdateEye();
                }
            }
        }

        // NOTE: ここから下をいじる場合、手作業というよりは正規表現でいじったほうがよい
        
        private void ResetFacialValues(float breakRate)
        {
            _blendShapes.Eye.LeftBlink *= breakRate;
            _blendShapes.Eye.LeftLookUp *= breakRate;
            _blendShapes.Eye.LeftLookDown *= breakRate;
            _blendShapes.Eye.LeftLookIn *= breakRate;
            _blendShapes.Eye.LeftLookOut *= breakRate;
            _blendShapes.Eye.LeftWide *= breakRate;
            _blendShapes.Eye.LeftSquint *= breakRate;

            _blendShapes.Eye.RightBlink *= breakRate;
            _blendShapes.Eye.RightLookUp *= breakRate;
            _blendShapes.Eye.RightLookDown *= breakRate;
            _blendShapes.Eye.RightLookIn *= breakRate;
            _blendShapes.Eye.RightLookOut *= breakRate;
            _blendShapes.Eye.RightWide *= breakRate;
            _blendShapes.Eye.RightSquint *= breakRate;

            //口(多い)
            _blendShapes.Mouth.Left *= breakRate;
            _blendShapes.Mouth.LeftSmile *= breakRate;
            _blendShapes.Mouth.LeftFrown *= breakRate;
            _blendShapes.Mouth.LeftPress *= breakRate;
            _blendShapes.Mouth.LeftUpperUp *= breakRate;
            _blendShapes.Mouth.LeftLowerDown *= breakRate;
            _blendShapes.Mouth.LeftStretch *= breakRate;
            _blendShapes.Mouth.LeftDimple *= breakRate;

            _blendShapes.Mouth.Right *= breakRate;
            _blendShapes.Mouth.RightSmile *= breakRate;
            _blendShapes.Mouth.RightFrown *= breakRate;
            _blendShapes.Mouth.RightPress *= breakRate;
            _blendShapes.Mouth.RightUpperUp *= breakRate;
            _blendShapes.Mouth.RightLowerDown *= breakRate;
            _blendShapes.Mouth.RightStretch *= breakRate;
            _blendShapes.Mouth.RightDimple *= breakRate;

            _blendShapes.Mouth.Close *= breakRate;
            _blendShapes.Mouth.Funnel *= breakRate;
            _blendShapes.Mouth.Pucker *= breakRate;
            _blendShapes.Mouth.ShrugUpper *= breakRate;
            _blendShapes.Mouth.ShrugLower *= breakRate;
            _blendShapes.Mouth.RollUpper *= breakRate;
            _blendShapes.Mouth.RollLower *= breakRate;

            //あご
            _blendShapes.Jaw.Open *= breakRate;
            _blendShapes.Jaw.Forward *= breakRate;
            _blendShapes.Jaw.Left *= breakRate;
            _blendShapes.Jaw.Right *= breakRate;

            //鼻
            _blendShapes.Nose.LeftSneer *= breakRate;
            _blendShapes.Nose.RightSneer *= breakRate;

            //ほお
            _blendShapes.Cheek.Puff *= breakRate;
            _blendShapes.Cheek.LeftSquint *= breakRate;
            _blendShapes.Cheek.RightSquint *= breakRate;

            //まゆげ
            _blendShapes.Brow.LeftDown *= breakRate;
            _blendShapes.Brow.LeftOuterUp *= breakRate;
            _blendShapes.Brow.RightDown *= breakRate;
            _blendShapes.Brow.RightOuterUp *= breakRate;
            _blendShapes.Brow.InnerUp *= breakRate;  
        }

        private void SetValue(string keyName, float value)
        {
            switch (keyName)
            {
                case Keys.eyeBlinkLeft: _blendShapes.Eye.LeftBlink = value; break;
                case Keys.eyeLookUpLeft: _blendShapes.Eye.LeftLookUp = value; break;
                case Keys.eyeLookDownLeft: _blendShapes.Eye.LeftLookDown = value; break;
                case Keys.eyeLookInLeft: _blendShapes.Eye.LeftLookIn = value; break;
                case Keys.eyeLookOutLeft: _blendShapes.Eye.LeftLookOut = value; break;
                case Keys.eyeWideLeft: _blendShapes.Eye.LeftWide = value; break;
                case Keys.eyeSquintLeft: _blendShapes.Eye.LeftSquint = value; break;

                case Keys.eyeBlinkRight: _blendShapes.Eye.RightBlink = value; break;
                case Keys.eyeLookUpRight: _blendShapes.Eye.RightLookUp = value; break;
                case Keys.eyeLookDownRight: _blendShapes.Eye.RightLookDown = value; break;
                case Keys.eyeLookInRight: _blendShapes.Eye.RightLookIn = value; break;
                case Keys.eyeLookOutRight: _blendShapes.Eye.RightLookOut = value; break;
                case Keys.eyeWideRight: _blendShapes.Eye.RightWide = value; break;
                case Keys.eyeSquintRight: _blendShapes.Eye.RightSquint = value; break;

                //口(多い)
                case Keys.mouthLeft: _blendShapes.Mouth.Left = value; break;
                case Keys.mouthSmileLeft: _blendShapes.Mouth.LeftSmile = value; break;
                case Keys.mouthFrownLeft: _blendShapes.Mouth.LeftFrown = value; break;
                case Keys.mouthPressLeft: _blendShapes.Mouth.LeftPress = value; break;
                case Keys.mouthUpperUpLeft: _blendShapes.Mouth.LeftUpperUp = value; break;
                case Keys.mouthLowerDownLeft: _blendShapes.Mouth.LeftLowerDown = value; break;
                case Keys.mouthStretchLeft: _blendShapes.Mouth.LeftStretch = value; break;
                case Keys.mouthDimpleLeft: _blendShapes.Mouth.LeftDimple = value; break;

                case Keys.mouthRight: _blendShapes.Mouth.Right = value; break;
                case Keys.mouthSmileRight: _blendShapes.Mouth.RightSmile = value; break;
                case Keys.mouthFrownRight: _blendShapes.Mouth.RightFrown = value; break;
                case Keys.mouthPressRight: _blendShapes.Mouth.RightPress = value; break;
                case Keys.mouthUpperUpRight: _blendShapes.Mouth.RightUpperUp = value; break;
                case Keys.mouthLowerDownRight: _blendShapes.Mouth.RightLowerDown = value; break;
                case Keys.mouthStretchRight: _blendShapes.Mouth.RightStretch = value; break;
                case Keys.mouthDimpleRight: _blendShapes.Mouth.RightDimple = value; break;

                case Keys.mouthClose: _blendShapes.Mouth.Close = value; break;
                case Keys.mouthFunnel: _blendShapes.Mouth.Funnel = value; break;
                case Keys.mouthPucker: _blendShapes.Mouth.Pucker = value; break;
                case Keys.mouthShrugUpper: _blendShapes.Mouth.ShrugUpper = value; break;
                case Keys.mouthShrugLower: _blendShapes.Mouth.ShrugLower = value; break;
                case Keys.mouthRollUpper: _blendShapes.Mouth.RollUpper = value; break;
                case Keys.mouthRollLower: _blendShapes.Mouth.RollLower = value; break;
                
                //あご
                case Keys.jawOpen: _blendShapes.Jaw.Open = value; break;
                case Keys.jawForward: _blendShapes.Jaw.Forward = value; break;
                case Keys.jawLeft: _blendShapes.Jaw.Left = value; break;
                case Keys.jawRight: _blendShapes.Jaw.Right = value; break;
                
                //鼻
                case Keys.noseSneerLeft: _blendShapes.Nose.LeftSneer = value; break;
                case Keys.noseSneerRight: _blendShapes.Nose.RightSneer = value; break;

                //ほお
                case Keys.cheekPuff: _blendShapes.Cheek.Puff = value; break;
                case Keys.cheekSquintLeft: _blendShapes.Cheek.LeftSquint = value; break;
                case Keys.cheekSquintRight: _blendShapes.Cheek.RightSquint = value; break;
                
                //まゆげ
                case Keys.browDownLeft: _blendShapes.Brow.LeftDown = value; break;
                case Keys.browOuterUpLeft: _blendShapes.Brow.LeftOuterUp = value; break;
                case Keys.browDownRight: _blendShapes.Brow.RightDown = value; break;
                case Keys.browOuterUpRight: _blendShapes.Brow.RightOuterUp = value; break;
                case Keys.browInnerUp: _blendShapes.Brow.InnerUp = value; break;  

                // "_neutral" を無視する
                default: break;
            }
        }
    }
}