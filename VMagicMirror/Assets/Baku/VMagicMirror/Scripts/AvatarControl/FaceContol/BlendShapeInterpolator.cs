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
        private const int FaceApplyCountMax = 8;
        
        //補間する場合のフレーム単位で考慮するウェイト。メンドいのでdeltaTimeには依存させない。
        //理想的には0.1secで表情が切り替わる
        private static readonly float[] WeightCurve = new float[]
        {
            0f,
            0.1f,
            0.1f,
            0.2f,
            0.5f,
            0.8f,
            0.9f,
            0.9f,
            1f,
        };
        
        [Inject]
        public void Initialize(
            IMessageReceiver receiver, IVRMLoadable vrmLoadable, EyeBonePostProcess eyeBonePostProcess)
        {
            _eyeBonePostProcess = eyeBonePostProcess;
            
            receiver.AssignCommandHandler(
                VmmCommands.DisableBlendShapeInterpolate,
                command => _interpolateBlendShapeWeight = !command.ToBoolean());

            vrmLoadable.VrmLoaded += info =>
            {
                _blendShapeAvatar = info.blendShape.BlendShapeAvatar;
                //CheckBinaryClips(_blendShapeAvatar);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _blendShapeAvatar = null;
            };
        }

        private bool _hasModel;
        private BlendShapeAvatar _blendShapeAvatar;
        private EyeBonePostProcess _eyeBonePostProcess;

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
        private int _faceAppliedCount;

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
                _toState.Weight = 1f;
                _fromState.Weight = 0f;
                return;
            }

            //遷移元か遷移先がIsBinary: 補間を完全にスキップ
            if (_toState.IsBinary || _fromState.IsBinary)
            {
                _faceAppliedCount = FaceApplyCountMax;
            }

            //補間が終了済み: 実態に沿うようにして終わり
            if (_faceAppliedCount >= FaceApplyCountMax)
            {
                NeedToInterpolate = false;
                NonMouthWeight = _toState.NonMouthWeight;
                MouthWeight = _toState.MouthWeight;
                _toState.Weight = 1f;
                _fromState.Weight = 0f;
                return;
            }
            
            //上記どれでもない: 補間が必要
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

            //WtM / Face Switchが効いているときは目ボーンをデフォルト位置に引っ張り戻す
            _eyeBonePostProcess.ReserveWeight = NonMouthWeight;
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

        private void SetWordToMotion(List<(BlendShapeKey, float)> blendShapes, bool keepLipSync)
        {
            _toState.CopyTo(_fromState);
            
            _toState.IsEmpty = false;
            _toState.Weight = 0f;
            _toState.Keys.Clear();
            _toState.Keys.AddRange(blendShapes);
            _toState.KeepLipSync = keepLipSync;
            _toState.IsBinary = _hasModel && blendShapes
                .Any(pair =>
                {
                    var (key, value) = pair;
                    if (!(value > 0f))
                    {
                        return false;
                    }
                    var clip = _blendShapeAvatar.GetClip(key);
                    return (clip != null) && clip.IsBinary;
                });

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

        //DEBUG用: 読み込んだモデルでIsBinaryなBlendShapeを確認する
        private void CheckBinaryClips(BlendShapeAvatar blendShapeAvatar)
        {
            foreach (var clip in blendShapeAvatar.Clips)
            {
                Debug.Log($"BlendShapeClip.Key={clip.Key}, IsBinary={clip.IsBinary}");
            }
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
