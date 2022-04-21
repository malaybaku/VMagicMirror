using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="BlendShapeResultSetter"/>で適用する表情のうち、
    /// FaceSwitchとWord to Motionを適用するときのフェードを考慮できるようにするクラス
    /// </summary>
    public class BlendShapeInterpolator : MonoBehaviour
    {
        private const int FaceApplyCountMax = 6;
        
        //補間する場合のフレーム単位で考慮するウェイト。メンドいのでdeltaTimeには依存させない。
        //理想的には0.1secで表情が切り替わる
        private static readonly float[] WeightCurve = new float[]
        {
            0f,
            0.1f,
            0.2f,
            0.5f,
            0.8f,
            0.9f,
            1f,
        };
        
        [Inject]
        public void Initialize(IMessageReceiver receiver, IVRMLoadable vrmLoadable)
        {
            receiver.AssignCommandHandler(
                VmmCommands.DisableBlendShapeInterpolate,
                command => _interpolateBlendShapeWeight = !command.ToBoolean());

            vrmLoadable.VrmLoaded += info =>
            {
                _blendShapeAvatar = info.blendShape.BlendShapeAvatar;
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _blendShapeAvatar = null;
                _hasModel = false;
            };
        }

        private bool _hasModel;
        private BlendShapeAvatar _blendShapeAvatar;

        private bool _interpolateBlendShapeWeight = true;
        
        public bool NeedToInterpolate { get; private set; }

        //いわゆる普段の表情のウェイト: だいたい1で、たまに0になる
        public float NonMouthWeight { get; private set; } = 1f;
        public float MouthWeight { get; private set; } = 1f;

        //NOTE: 何も適用してない状態はIsEmpty == trueになることで表現される
        private readonly State _fromState = new State();
        private readonly State _toState = new State();
        
        //WtMかFace Switchが適用されると0からプラスの値に推移していく。
        //30fpsの場合、1フレームごとに2加算され、最大値は6。
        private int _faceAppliedCount = 0;

        private readonly ReactiveProperty<bool> _hasWordToMotionOutput = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _hasFaceSwitchOutput = new ReactiveProperty<bool>(false);

        public void Setup(ExternalTrackerFaceSwitchApplier faceSwitch, WordToMotionBlendShape wtmBlendShape)
        {
            faceSwitch.CurrentValue
                .Subscribe(v =>
                {
                    _hasFaceSwitchOutput.Value = v.HasValue;
                    if (v.HasValue)
                    {
                        SetFaceSwitch(v.Key, v.KeepLipSync);
                    }
                })
                .AddTo(this);
            
            wtmBlendShape.CurrentValue
                .Subscribe(v =>
                {
                    _hasWordToMotionOutput.Value = v.HasValue;
                    if (v.HasValue)
                    {
                        SetWordToMotion(v.Keys, v.KeepLipSync);
                    }
                })
                .AddTo(this);

            Observable.CombineLatest(
                    _hasWordToMotionOutput,
                    _hasFaceSwitchOutput,
                    (a, b) => a || b
                )
                .Subscribe(hasOutput =>
                {
                    if (!hasOutput)
                    {
                        Clear();
                    }
                })
                .AddTo(this);
        }
        
        /// <summary>
        /// 毎フレーム呼び出すことで各weightを更新する。
        /// </summary>
        public void UpdateWeight()
        {
            if (!_hasModel || !_interpolateBlendShapeWeight)
            {
                NeedToInterpolate = false;
                _faceAppliedCount = 0;
                NonMouthWeight = 1f;
                MouthWeight = 1f;
                return;
            }

            //普段どおりの表情になって一定時間経った
            if (_toState.IsEmpty && _faceAppliedCount >= FaceApplyCountMax)
            {
                NeedToInterpolate = false;
                NonMouthWeight = 1f;
                MouthWeight = 1f;
                return;
            }

            //表情が適用され、補間が終了したので従来処理でもよくなったケース: 実態に沿うようにする
            if (!_toState.IsEmpty && _faceAppliedCount >= FaceApplyCountMax)
            {
                NeedToInterpolate = false;
                NonMouthWeight = 0f;
                MouthWeight = _toState.KeepLipSync ? 1f : 0f;
                return;
            }
            
            //上記どちらでもない: 補間が要るはず
            NeedToInterpolate = true;
            
            var weight = WeightCurve[_faceAppliedCount];
            MouthWeight = Mathf.Lerp(_fromState.MouthWeight, _toState.MouthWeight, weight);
            NonMouthWeight = Mathf.Lerp(_fromState.NonMouthWeight, _toState.NonMouthWeight, weight);
            _fromState.Weight = 1 - weight;
            _toState.Weight = weight;

            var fps = Application.targetFrameRate;
            _faceAppliedCount++;
            if (fps < 60)
            {
                _faceAppliedCount++;
            }
        }

        //NOTE: fromかtoにFaceSwitch / WtMいずれかの表情が入っている時だけ呼ぶ想定の関数。
        //ただし、それ以外のケースで呼んでも破綻はしないはず
        public void Accumulate(VRMBlendShapeProxy proxy)
        {
            foreach (var item in _fromState.Keys)
            {
                proxy.AccumulateValue(item.Item1, item.Item2 * _fromState.Weight);
            }

            foreach (var item in _toState.Keys)
            {
                proxy.AccumulateValue(item.Item1, item.Item2 * _toState.Weight);
            }
        }

        private void SetFaceSwitch(BlendShapeKey key, bool keepLipSync)
        {
            _toState.CopyTo(_fromState);
            
            _toState.IsEmpty = false;
            _toState.Weight = 0f;
            _toState.Keys.Clear();
            _toState.Keys.Add((key, 1f));
            _toState.KeepLipSync = keepLipSync;
            _toState.IsBinary = _hasModel && _blendShapeAvatar.GetClip(key)?.IsBinary == true;
            
            _faceAppliedCount = 0;
        }

        private void SetWordToMotion(List<(BlendShapeKey, float)> blendshapes, bool keepLipSync)
        {
            _toState.CopyTo(_fromState);
            
            _toState.IsEmpty = false;
            _toState.Weight = 0f;
            _toState.Keys.Clear();
            _toState.Keys.AddRange(blendshapes);
            _toState.KeepLipSync = keepLipSync;
            _toState.IsBinary =
                _hasModel && blendshapes.Any(b => _blendShapeAvatar.GetClip(b.Item1)?.IsBinary == true);

            _faceAppliedCount = 0;
        }

        /// <summary> 表情が未適用の状態に遷移させる。 </summary>
        private void Clear()
        {
            _toState.CopyTo(_fromState);
            _toState.OverwriteToEmpty();
            _toState.Weight = 0f;
            _faceAppliedCount = 0;
        }
        
        //NOTE: 「FaceSwitchもWtMも必要ない」という状態はIsEmpty == trueにすることで表現する
        class State
        {
            public bool IsEmpty { get; set; }
            public List<(BlendShapeKey, float)> Keys { get; } = new List<(BlendShapeKey, float)>(8);
            public bool KeepLipSync { get; set; }
            public bool IsBinary { get; set; }
            public float Weight { get; set; }

            //このステートのときにMouth/それ以外が最終的にWeightいくらで動いてほしいか
            public float MouthWeight => IsEmpty || KeepLipSync ? 1f : 0f;
            public float NonMouthWeight => IsEmpty ? 1f : 0f;

            public void CopyTo(State other)
            {
                other.IsEmpty = IsEmpty;
                other.Keys.Clear();
                other.Keys.AddRange(Keys);
                other.KeepLipSync = KeepLipSync;
                other.IsBinary = IsBinary;
                other.Weight = Weight;
            }

            public void OverwriteToEmpty()
            {
                IsEmpty = true;
                Keys.Clear();
                KeepLipSync = false;
                IsBinary = false;
            }
        }
    }
}
