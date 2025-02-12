using System;
using Baku.VMagicMirror.ExternalTracker;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: BlinkTriggerDetectorとやってることが被ってるので直したい
    // たぶんBlinkDetectorのほうが洗練されている…ような気がする。何ならBlinkTriggerDetector側の仕様を無しにしてもいい、かも
    
    // NOTE:
    // VRMAutoBlinkとかに対して本クラスがやる処理は冗長である (Blinkのクラス側が「まばたき開始」イベントを直接発火できる) が、
    // パーフェクトシンクとかでも動いてほしい…となると汎用計算がないと厳しいため、全体的に汎用の計算に帰着させる

    /// <summary>
    /// サブキャラAPIに提供するために「アバターのまばたきの発生」をイベントとして発火してくれるすごいやつだよ
    /// </summary>
    /// <remarks>
    /// まばたきは以下のような動作である…と考えたうえで、cooldownとかも設けて「2連続瞬き」みたいなのは適宜無視する
    /// 
    /// - 目をある程度以上閉じる
    /// - その後、一定時間内に開き直す
    /// </remarks>
    public class BlinkDetector : PresenterBase, ITickable
    {
        // NOTE: 値が一緒だとチャタリングとか考える必要があるので、開閉でしきい値は変える
        private const float EyeOpenToCloseThreshold = 0.8f;
        private const float EyeCloseToOpenThreshold = 0.6f;
        // 目を閉じてる時間が一定以上長い場合、まばたきではないものと判定する
        private const float EyeCloseTimeUpperLimit = 0.5f;
        // 発火頻度を制限するやつ
        private const float CoolDownTime = 1.0f;

        private readonly IVRMLoadable _vrmLoadable;
        private readonly FaceControlConfiguration _faceControlConfiguration;
        private readonly ExpressionAccumulator _expressionAccumulator;
        private readonly ExternalTrackerBlink _externalTrackerBlink;
            
        private bool _isLoaded;
        private bool _isEyeClosed;
        private float _eyeCloseElapsedTime;

        public BlinkDetector(
            IVRMLoadable vrmLoadable,
            FaceControlConfiguration faceControlConfiguration,
            ExpressionAccumulator expressionAccumulator,
            ExternalTrackerBlink externalTrackerBlink)
        {
            _vrmLoadable = vrmLoadable;
            _faceControlConfiguration = faceControlConfiguration;
            _expressionAccumulator = expressionAccumulator;
            _externalTrackerBlink = externalTrackerBlink;
        }

        private readonly Subject<Unit> _blinked = new();
        public IObservable<Unit> Blinked() => _blinked.ThrottleFirst(TimeSpan.FromSeconds(CoolDownTime));
        
        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += _ => _isLoaded = true;
            _vrmLoadable.VrmDisposing += () =>
            {
                _isEyeClosed = false;
                _eyeCloseElapsedTime = 0f;
                _isLoaded = false;
            };
        }

        void ITickable.Tick()
        {
            if (!_isLoaded)
            {
                return;
            }

            var blinkValue = GetBlinkBlendShapeValue();
            if (!_isEyeClosed && blinkValue > EyeOpenToCloseThreshold)
            {
                _isEyeClosed = true;
                _eyeCloseElapsedTime = 0f;
            }
            else if (_isEyeClosed && blinkValue < EyeCloseToOpenThreshold)
            {
                _isEyeClosed = false;
                if (_eyeCloseElapsedTime < EyeCloseTimeUpperLimit)
                {
                    _blinked.OnNext(Unit.Default);
                }
            }

            if (_isEyeClosed)
            {
                _eyeCloseElapsedTime += Time.deltaTime;
            }
        }

        private float GetBlinkBlendShapeValue()
        {
            if (_faceControlConfiguration.PerfectSyncActive)
            {
                return GetPerfectSyncBlinkBlendShapeValue();
            }
            else
            {
                return GetStandardBlinkBlendShapeValue();
            }
        }

        private float GetStandardBlinkBlendShapeValue()
        {
            // NOTE: 通常のVMMではL/Rを別々に動かしてるが、ただのBlinkで目を閉じるケースもケアしておく
            var blink = _expressionAccumulator.GetValue(ExpressionKey.Blink);
            var blinkLeft = _expressionAccumulator.GetValue(ExpressionKey.BlinkLeft);
            var blinkRight = _expressionAccumulator.GetValue(ExpressionKey.BlinkRight);
            return Mathf.Max(blink, (blinkLeft + blinkRight) * 0.5f);
        }

        private float GetPerfectSyncBlinkBlendShapeValue()
        {
            var rightBlink = _expressionAccumulator.GetValue(ExternalTrackerPerfectSync.Keys.EyeBlinkRight);
            var rightSquint = _expressionAccumulator.GetValue(ExternalTrackerPerfectSync.Keys.EyeSquintRight);
            var leftBlink = _expressionAccumulator.GetValue(ExternalTrackerPerfectSync.Keys.EyeBlinkLeft);
            var leftSquint = _expressionAccumulator.GetValue(ExternalTrackerPerfectSync.Keys.EyeSquintLeft);
            
            // NOTE:
            // 「ExTrackerを使っているがパーフェクトシンクはオフのときにBlink(Left|Right)の値を求めるときの式」を流用してる
            var (left, right) = _externalTrackerBlink.CalculateBlinkValues(
                rightBlink, rightSquint, leftBlink, leftSquint
            );

            // NOTE: WordToMotionとかFaceSwitch由来のまばたき動作もケアしたいので、パーフェクトシンクじゃないほうの値も見ておく
            return Mathf.Max(
                (left + right) * 0.5f,
                GetStandardBlinkBlendShapeValue()
            );
        }
    }
}
