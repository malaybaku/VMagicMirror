using System.Collections.Generic;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    public class AnimMorphEasedTarget : MonoBehaviour
    {
        [Tooltip("主要な母音音素(aa, E, ih, oh, ou)に対してBlendShapeを動かすカーブ")]
        public AnimationCurve transitionCurves = new AnimationCurve(new[]
        {
            new Keyframe(0.0f, 0.0f),
            new Keyframe(0.1f, 1.0f),
        });

        [Tooltip("発音しなくなった母音のBlendShapeをゼロに近づける際の速度を表す値")]
        public float cancelSpeedFactor = 8.0f;

        [Range(0.0f, 100.0f), Tooltip("この閾値未満の音素の重みは無視する")]
        public float weightThreshold = 2.0f;

        [Tooltip("OVRLipSyncに渡すSmoothing amountの値")]
        public int smoothAmount = 100;
        
        private bool _shouldReceiveData = true;
        public bool ShouldReceiveData
        {
            get => _shouldReceiveData;
            set
            {
                if (_shouldReceiveData == value)
                {
                    return;
                }

                _shouldReceiveData = value;
                if (!value)
                {
                    UpdateToClosedMouth();
                }
            }
        }

        private readonly RecordLipSyncSource _lipSyncSource = new RecordLipSyncSource();
        public IMouthLipSyncSource LipSyncSource => _lipSyncSource;

        private readonly Dictionary<BlendShapeKey, float> _blendShapeWeights = new Dictionary<BlendShapeKey, float>
        {
            [BlendShapeKey.CreateFromPreset(BlendShapePreset.A)] = 0.0f,
            [BlendShapeKey.CreateFromPreset(BlendShapePreset.E)] = 0.0f,
            [BlendShapeKey.CreateFromPreset(BlendShapePreset.I)] = 0.0f,
            [BlendShapeKey.CreateFromPreset(BlendShapePreset.O)] = 0.0f,
            [BlendShapeKey.CreateFromPreset(BlendShapePreset.U)] = 0.0f,
        };

        private readonly BlendShapeKey[] _keys = new[]
        {
            BlendShapeKey.CreateFromPreset(BlendShapePreset.A),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.E),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.I),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.O),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.U),
        };

        private OVRLipSyncContextBase _context;
        private OVRLipSync.Viseme _previousViseme = OVRLipSync.Viseme.sil;
        private float _transitionTimer = 0.0f;
        
        private void Start()
        {
            _context = GetComponent<OVRLipSyncContextBase>();
            if (_context == null)
            {
                LogOutput.Instance.Write("同じGameObjectにOVRLipSyncContextBaseを継承したクラスが見つかりません。");
            }
            _context.Smoothing = smoothAmount;
        }

        private void Update()
        {
            //口閉じの場合: とにかく閉じるのが良いので閉じて終わり
            if (!ShouldReceiveData)
            {
                UpdateToClosedMouth();
                return;
            }

            if (_context == null || 
                !_context.enabled || 
                !(_context.GetCurrentPhonemeFrame() is OVRLipSync.Frame frame)
                )
            {
                return;
            }

            _transitionTimer += Time.deltaTime;

            // 最大の重みを持つ音素を探す
            int maxVisemeIndex = 0;
            float maxVisemeWeight = 0.0f;
            // 子音は無視する
            for (var i = (int)OVRLipSync.Viseme.aa; i < frame.Visemes.Length; i++)
            {
                if (frame.Visemes[i] > maxVisemeWeight)
                {
                    maxVisemeWeight = frame.Visemes[i];
                    maxVisemeIndex = i;
                }
            }

            // 音素の重みが小さすぎる場合は口を閉じる
            if (maxVisemeWeight * 100.0f < weightThreshold)
            {
                _transitionTimer = 0.0f;
            }

            // 音素の切り替わりでタイマーをリセットする
            if (_previousViseme != (OVRLipSync.Viseme)maxVisemeIndex)
            {
                _transitionTimer = 0.0f;
                _previousViseme = (OVRLipSync.Viseme)maxVisemeIndex;
            }

            int visemeIndex = maxVisemeIndex - (int)OVRLipSync.Viseme.aa;
            bool hasValidMaxViseme = (visemeIndex >= 0);

            for(int i = 0; i < _keys.Length; i++)
            {
                var key = _keys[i];

                _blendShapeWeights[key] = Mathf.Lerp(
                    _blendShapeWeights[key],
                    0.0f,
                    Time.deltaTime * cancelSpeedFactor
                    );

                //減衰中の値のほうが大きければそっちを採用する。
                //「あぁあぁぁあ」みたいな声の出し方した場合にEvaluateだけ使うとヘンテコになる可能性が高い為。
                if (hasValidMaxViseme && i == visemeIndex)
                {
                    _blendShapeWeights[key] = Mathf.Max(
                        _blendShapeWeights[key],
                        transitionCurves.Evaluate(_transitionTimer)
                        );
                }
            }

            //順番に注意: visemeのキーに合わせてます
            _lipSyncSource.A = _blendShapeWeights[_keys[0]];
            _lipSyncSource.E = _blendShapeWeights[_keys[1]];
            _lipSyncSource.I = _blendShapeWeights[_keys[2]];
            _lipSyncSource.O = _blendShapeWeights[_keys[3]];
            _lipSyncSource.U = _blendShapeWeights[_keys[4]];
        }

        private void UpdateToClosedMouth()
        {
            foreach(var key in _keys)
            {
                _blendShapeWeights[key] = 0.0f;
            }

            _lipSyncSource.A = 0;
            _lipSyncSource.I = 0;
            _lipSyncSource.U = 0;
            _lipSyncSource.E = 0;
            _lipSyncSource.O = 0;
        }
    }
}
