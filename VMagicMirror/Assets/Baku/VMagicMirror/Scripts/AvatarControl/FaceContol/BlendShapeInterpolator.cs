using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="BlendShapeResultSetter"/>で適用する表情のうち、
    /// FaceSwitchとWord to Motionを適用するときのフェードを考慮できるようにするクラス
    /// NOTE: VRM 1.0の排他がどうとかはあまり考えてない実装であることに注意
    /// </summary>
    public class BlendShapeInterpolator : MonoBehaviour
    {
        private const int FaceApplyCountMax = 8;

        private const int StatePriorityNone = 0;
        private const int StatePriorityFaceSwitch = 1;
        private const int StatePriorityWordToMotion = 2;

        //補間する場合のフレーム単位で考慮するウェイト。メンドいのでdeltaTimeには依存させない。
        //理想的には0.1secで表情が切り替わる
        private static readonly float[] WeightCurve = {
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
            IMessageReceiver receiver, IVRMLoadable vrmLoadable, EyeBoneAngleSetter eyeBoneAngleSetter)
        {
            _eyeBoneAngleSetter = eyeBoneAngleSetter;
            
            receiver.AssignCommandHandler(
                VmmCommands.DisableBlendShapeInterpolate,
                command => _interpolateBlendShapeWeight = !command.ToBoolean());

            vrmLoadable.VrmLoaded += info =>
            {
                _expressionMap = info.instance.Vrm.Expression.LoadExpressionMap();
                //CheckBinaryClips(_expressionMap);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _expressionMap = null;
            };
        }

        private bool _hasModel;
        private VRM10ExpressionMap _expressionMap;
        private EyeBoneAngleSetter _eyeBoneAngleSetter;

        private bool _interpolateBlendShapeWeight = true;
        
        public bool NeedToInterpolate { get; private set; }

        //いわゆる普段の表情のウェイト: だいたい1で、たまに0になる
        public float NonMouthWeight { get; private set; } = 1f;
        public float MouthWeight { get; private set; } = 1f;

        //NOTE: 何も適用してない状態はIsEmpty == trueになることで表現される
        private readonly State _fromState = new();
        private readonly State _toState = new();
        //FaceSwitchはWtMより優先度が低く、「from/toどちらでも無いけど適用するかも」という状態になることがあるので、
        //必要になったら使えるようにするために値をキャッシュするやつ
        private readonly State _faceSwitchState = new();
        
        //WtMかFace Switchが適用されると0からプラスの値に推移していく。
        //30fpsの場合、1フレームごとに2加算され、最大値は6。
        private int _faceAppliedCount;

        private readonly ReactiveProperty<bool> _hasWordToMotionOutput = new(false);
        private readonly ReactiveProperty<bool> _hasFaceSwitchOutput = new(false);

        public void Setup(
            FaceSwitchUpdater faceSwitchUpdater,
            ExternalTrackerFaceSwitchApplier faceSwitch,
            WordToMotionBlendShape wtmBlendShape)
        {
            Debug.LogError("ちゃんと動いてそうになったらfaceSwitch引数を削除したい");
            faceSwitchUpdater.CurrentValue
                .Subscribe(v =>
                {
                    _hasFaceSwitchOutput.Value = v.HasValue;
                    if (v.HasValue)
                    {
                        SetFaceSwitch(v.Key, v.KeepLipSync);
                    }
                    else
                    {
                        _faceSwitchState.OverwriteToEmpty();
                    }
                })
                .AddTo(this);
            
            wtmBlendShape.CurrentValue
                .Subscribe(v =>
                {
                    _hasWordToMotionOutput.Value = v.HasValue;
                    if (v.HasValue)
                    {
                        SetWordToMotion(v.Keys, v.KeepLipSync, v.IsPreview);
                    }
                    else if (!_faceSwitchState.IsEmpty)
                    {
                        //WtMが終わった瞬間に有効なFaceSwitchがあったらそれに遷移
                        SetLowPriorFaceSwitchToActive();
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
        public void Accumulate(ExpressionAccumulator accumulator)
        {
            foreach (var item in _fromState.Keys)
            {
                accumulator.Accumulate(item.Item1, item.Item2 * _fromState.Weight);
            }

            foreach (var item in _toState.Keys)
            {
                accumulator.Accumulate(item.Item1, item.Item2 * _toState.Weight);
            }

            //WtM / Face Switchが効いているときは目ボーンをデフォルト位置に引っ張り戻す
            _eyeBoneAngleSetter.ReserveWeight = NonMouthWeight;
        }

        private void SetFaceSwitch(ExpressionKey key, bool keepLipSync)
        {
            Write(_faceSwitchState, key, keepLipSync);

            //NOTE: WtMが適用中の場合、優先度が低いので実際には何もしない
            if (_toState.Priority > StatePriorityFaceSwitch)
            {
                return;
            }
            
            _toState.CopyTo(_fromState);
            Write(_toState, key, keepLipSync);
            _faceAppliedCount = 0;

            void Write(State target, ExpressionKey bsKey, bool bsKeepLipSync)
            {
                target.IsEmpty = false;
                target.Weight = 0f;
                target.Priority = StatePriorityFaceSwitch;
                target.Keys.Clear();
                target.Keys.Add((bsKey, 1f));
                target.KeepLipSync = bsKeepLipSync;
                target.IsBinary =
                    _hasModel && _expressionMap.TryGet(bsKey, out var clip) && clip.IsBinary;
            }
        }

        private void SetWordToMotion(List<(ExpressionKey, float)> blendShapes, bool keepLipSync, bool isPreview)
        {
            _toState.CopyTo(_fromState);
            
            _toState.IsEmpty = false;
            _toState.Weight = 0f;
            _toState.Priority = StatePriorityWordToMotion;
            _toState.Keys.Clear();
            _toState.Keys.AddRange(blendShapes);
            _toState.KeepLipSync = keepLipSync;
            //NOTE: プレビュー表示の場合、補間は考えないでOK
            _toState.IsBinary = _hasModel && (isPreview || blendShapes
                .Any(pair =>
                {
                    var (key, value) = pair;
                    if (!(value > 0f))
                    {
                        return false;
                    }
                    return _expressionMap.TryGet(key, out var clip) && clip.IsBinary;
                }));

            _faceAppliedCount = 0;
        }

        private void SetLowPriorFaceSwitchToActive()
        {
            _toState.CopyTo(_fromState);
            //適用待ちの値が書き込み済みなのでそのまま使う
            _faceSwitchState.CopyTo(_toState);
            _faceSwitchState.OverwriteToEmpty();
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
        private void CheckBinaryClips(VRM10ObjectExpression expressions)
        {
            foreach (var clip in expressions.Clips)
            {
                Debug.Log($"BlendShapeClip.Key=({clip.Preset}){clip.Clip.name}, IsBinary={clip.Clip.IsBinary}");
            }
        }
        
        //NOTE: 「FaceSwitchもWtMも必要ない」という状態はIsEmpty == trueにすることで表現する
        class State
        {
            public bool IsEmpty { get; set; }
            public List<(ExpressionKey, float)> Keys { get; } = new List<(ExpressionKey, float)>(8);
            public bool KeepLipSync { get; set; }
            public bool IsBinary { get; set; }
            public float Weight { get; set; }
            public int Priority { get; set; }

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
                other.Priority = Priority;
            }

            public void OverwriteToEmpty()
            {
                IsEmpty = true;
                Keys.Clear();
                KeepLipSync = false;
                IsBinary = false;
                Priority = StatePriorityNone;
            }
        }
    }
}
