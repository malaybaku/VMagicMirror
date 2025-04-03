using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    using Keys = MediaPipeBlendShapeKeys;

    /// <summary>
    /// Mediapipe Face Landmarkerで取得した表情の情報を保持するクラス。データが古くなると自動で捨てる実装も入っている
    /// </summary>
    public class MediaPipeFacialValueRepository : ITickable
    {
        private readonly MediapipePoseSetterSettings _settings;
        private readonly object _dataLock = new();

        private readonly CounterBoolState _isTracked = new(3, 5);
        // private readonly Dictionary<string, float> _values = new();
        private float _trackLostTime;

        private readonly RecordFaceTrackBlendShapes _blendShapes = new();
        public IFaceTrackBlendShapes BlendShapes => _blendShapes;
        
        [Inject]
        public MediaPipeFacialValueRepository(MediapipePoseSetterSettings settings)
        {
            _settings = settings;
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
        /// <param name="key"></param>
        /// <returns></returns>
        //public float GetValue(string key) => _values.GetValueOrDefault(key, 0f);

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
                if (!_isTracked.Value)
                {
                    ResetFacialValues();
                }
            }
        }

        void ITickable.Tick()
        {
            lock (_dataLock)
            {
                if (!_isTracked.Value)
                {
                    return;
                }

                _trackLostTime += Time.deltaTime;
                if (_trackLostTime >= _settings.TrackingLostWaitDuration)
                {
                    _isTracked.Reset(false);
                    _trackLostTime = 0f;
                    ResetFacialValues();
                }
            }
        }

        // NOTE: ここから下をいじる場合、手作業というよりは正規表現でいじったほうがよい
        
        private void ResetFacialValues()
        {
            _blendShapes.Eye.LeftBlink = 0f;
            _blendShapes.Eye.LeftLookUp = 0f;
            _blendShapes.Eye.LeftLookDown = 0f;
            _blendShapes.Eye.LeftLookIn = 0f;
            _blendShapes.Eye.LeftLookOut = 0f;
            _blendShapes.Eye.LeftWide = 0f;
            _blendShapes.Eye.LeftSquint = 0f;

            _blendShapes.Eye.RightBlink = 0f;
            _blendShapes.Eye.RightLookUp = 0f;
            _blendShapes.Eye.RightLookDown = 0f;
            _blendShapes.Eye.RightLookIn = 0f;
            _blendShapes.Eye.RightLookOut = 0f;
            _blendShapes.Eye.RightWide = 0f;
            _blendShapes.Eye.RightSquint = 0f;

            //口(多い)
            _blendShapes.Mouth.Left = 0f;
            _blendShapes.Mouth.LeftSmile = 0f;
            _blendShapes.Mouth.LeftFrown = 0f;
            _blendShapes.Mouth.LeftPress = 0f;
            _blendShapes.Mouth.LeftUpperUp = 0f;
            _blendShapes.Mouth.LeftLowerDown = 0f;
            _blendShapes.Mouth.LeftStretch = 0f;
            _blendShapes.Mouth.LeftDimple = 0f;

            _blendShapes.Mouth.Right = 0f;
            _blendShapes.Mouth.RightSmile = 0f;
            _blendShapes.Mouth.RightFrown = 0f;
            _blendShapes.Mouth.RightPress = 0f;
            _blendShapes.Mouth.RightUpperUp = 0f;
            _blendShapes.Mouth.RightLowerDown = 0f;
            _blendShapes.Mouth.RightStretch = 0f;
            _blendShapes.Mouth.RightDimple = 0f;

            _blendShapes.Mouth.Close = 0f;
            _blendShapes.Mouth.Funnel = 0f;
            _blendShapes.Mouth.Pucker = 0f;
            _blendShapes.Mouth.ShrugUpper = 0f;
            _blendShapes.Mouth.ShrugLower = 0f;
            _blendShapes.Mouth.RollUpper = 0f;
            _blendShapes.Mouth.RollLower = 0f;

            //あご
            _blendShapes.Jaw.Open = 0f;
            _blendShapes.Jaw.Forward = 0f;
            _blendShapes.Jaw.Left = 0f;
            _blendShapes.Jaw.Right = 0f;

            //鼻
            _blendShapes.Nose.LeftSneer = 0f;
            _blendShapes.Nose.RightSneer = 0f;

            //ほお
            _blendShapes.Cheek.Puff = 0f;
            _blendShapes.Cheek.LeftSquint = 0f;
            _blendShapes.Cheek.RightSquint = 0f;

            //まゆげ
            _blendShapes.Brow.LeftDown = 0f;
            _blendShapes.Brow.LeftOuterUp = 0f;
            _blendShapes.Brow.RightDown = 0f;
            _blendShapes.Brow.RightOuterUp = 0f;
            _blendShapes.Brow.InnerUp = 0f;  
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