using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// モードによらず(自動まばたき / iFacialMocap + パーフェクトシンクon/off)、まばたきの終了を検出するやつ
    /// </summary>
    public class BlinkTriggerDetector : IInitializable, ITickable
    {
        private const float OpenToCloseValue = 0.8f;
        private const float CloseToOpenValue = 0.6f;

        //閉じた後でゆっくり開くのはまばたきではない、とする。(これを通すのもアリだけどね)
        private const float BlinkCountLimit = 0.5f;
        
        private readonly FaceControlConfiguration _config;
        private readonly ExpressionAccumulator _accumulator;

        private static readonly ExpressionKey BlinkLeft = ExpressionKey.BlinkLeft;
        private static readonly ExpressionKey BlinkRight = ExpressionKey.BlinkRight;
        private static readonly ExpressionKey PerfectSyncBlinkLeft = ExpressionKey.CreateCustom("EyeBlinkLeft");
        private static readonly ExpressionKey PerfectSyncBlinkRight = ExpressionKey.CreateCustom("EyeBlinkRight");

        //目が閉じてるかどうか。チャタリング防止のため、閾値だけではなくフラグも必要になっている
        private bool _isEyesClosed;
        private float _eyeCloseCount;

        private Subject<Unit> _blinkDetected = new Subject<Unit>();
        public IObservable<Unit> BlinkDetected => _blinkDetected;

        public BlinkTriggerDetector(FaceControlConfiguration config, ExpressionAccumulator accumulator)
        {
            _config = config;
            _accumulator = accumulator;
        }
        
        //NOTE: めんどくさいのでつけっぱなしでいい
        void IInitializable.Initialize()
        {
            _accumulator.PreApply += OnPreApplyBlendShape;
        }

        void ITickable.Tick()
        {
            if (_isEyesClosed)
            {
                _eyeCloseCount += Time.deltaTime;
            }
        }

        private void OnPreApplyBlendShape(IReadOnlyDictionary<ExpressionKey, float> values)
        {
            var left = 0f;
            var right = 0f;
            if (_config.PerfectSyncActive)
            {
                left = _accumulator.GetValue(PerfectSyncBlinkLeft);
                right = _accumulator.GetValue(PerfectSyncBlinkRight);
            }
            else
            {
                left = _accumulator.GetValue(BlinkLeft);
                right = _accumulator.GetValue(BlinkRight);
            }

            if (!_isEyesClosed)
            {
                if (left > OpenToCloseValue && right > OpenToCloseValue)
                {
                    _isEyesClosed = true;
                    _eyeCloseCount = 0f;
                }
            }
            else
            {
                if (left < CloseToOpenValue && right < CloseToOpenValue)
                {
                    _isEyesClosed = false;
                    if (_eyeCloseCount < BlinkCountLimit)
                    {
                        _blinkDetected.OnNext(Unit.Default);
                    }
                }
            }
            
        }
    }
}
