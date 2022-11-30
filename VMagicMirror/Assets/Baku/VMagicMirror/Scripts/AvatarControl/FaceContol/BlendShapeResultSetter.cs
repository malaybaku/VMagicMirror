using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// BlendShapeProxyに関する処理を集約し、毎フレームここを起点にAccumulateValueとかApplyを呼ぶ
    /// </summary>
    public class BlendShapeResultSetter : MonoBehaviour
    {
        [SerializeField] private LipSyncIntegrator lipSync = null;
        [SerializeField] private FaceControlManager eyes = null;
        [SerializeField] private ExternalTrackerPerfectSync perfectSync = null;
        [SerializeField] private ExternalTrackerFaceSwitchApplier faceSwitch = null;
        [SerializeField] private NeutralClipSettings neutralClipSettings = null;
        [SerializeField] private BlendShapeInterpolator blendShapeInterpolator = null;
        
        private ExternalTrackerDataSource _exTracker = null;
        private WordToMotionBlendShape _wtmBlendShape = null;
        private ExpressionAccumulator _accumulator = null;
        private bool _hasModel = false;
        
        //NOTE: ここのコンポーネントの書き順は実は優先度を表している: 後ろのやつほど上書きの権利が強い
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable, 
            ExternalTrackerDataSource exTracker,
            WordToMotionBlendShape wtmBlendShape,
            ExpressionAccumulator accumulator
            )
        {
            _exTracker = exTracker;
            _wtmBlendShape = wtmBlendShape;
            _accumulator = accumulator;
            
            vrmLoadable.VrmLoaded += info => _hasModel = true;
            vrmLoadable.VrmDisposing += () => _hasModel = false;
            
            blendShapeInterpolator.Setup(faceSwitch, wtmBlendShape);
        }

        
        private void LateUpdate()
        {
            faceSwitch.UpdateCurrentValue();
            _wtmBlendShape.UpdateCurrentValue();
            blendShapeInterpolator.UpdateWeight();
            
            if (!_hasModel)
            {
                return;
            }

            _accumulator.ResetValues();

            //NOTE: 関数をわざわざ分けるのは後方互換性のため + ウェイトに配慮しないほうが処理が一回り軽い見込みのため
            if (blendShapeInterpolator.NeedToInterpolate)
            {
                WriteClipsWithInterpolation();
            }
            else
            {
                WriteClips();
            }
            
            _accumulator.Apply();
        }

        private void WriteClips()
        {
            //Word to Motionが適用 > FaceSwitch、PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (_wtmBlendShape.HasBlendShapeToApply)
            {
                //NOTE: リップシンク + パーフェクトシンクのときに同じキーを何回もセットすると計算が勿体ない。
                //その余分なコストを減らすため、ちょっと凝った書き方をしてます
                if (!_wtmBlendShape.KeepLipSync)
                {
                    //そもそもリップシンクは切ってよいケース: シンプルにゼロ埋め + WtMを適用
                    _wtmBlendShape.Accumulate(_accumulator);
                }
                else if (_exTracker.Connected && perfectSync.IsActive && perfectSync.PreferWriteMouthBlendShape)
                {
                    _wtmBlendShape.Accumulate(_accumulator);
                    perfectSync.Accumulate(_accumulator, false, true, false);
                }
                else
                {
                    //WtM + AIUEOの口を適用するケース: 重複がAIUEOの5個だけなのでザツにやっちゃう
                    _wtmBlendShape.Accumulate(_accumulator);
                    lipSync.Accumulate(_accumulator);
                }
                
                neutralClipSettings.AccumulateOffsetClip(_accumulator);
                return;
            }
            
            //FaceSwitchが適用 > PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (faceSwitch.HasClipToApply)
            {
                //NOTE: WtMと同じく、パーフェクトシンクの口と組み合わす場合のコストに多少配慮した書き方。
                if (!faceSwitch.KeepLipSync)
                {
                    faceSwitch.Accumulate(_accumulator);
                }
                else if (_exTracker.Connected && perfectSync.IsActive && perfectSync.PreferWriteMouthBlendShape)
                {
                    faceSwitch.Accumulate(_accumulator);
                    perfectSync.Accumulate(_accumulator, false, true, false);
                }
                else
                {
                    //FaceSwitch + AIUEOを適用するケース: 重複がAIUEOの5個だけなのでザツにやっちゃう
                    faceSwitch.Accumulate(_accumulator);
                    lipSync.Accumulate(_accumulator);
                }

                neutralClipSettings.AccumulateOffsetClip(_accumulator);
                return;
            }
            
            //Perfect Syncが適用 > Blinkは確定で無視。
            //リップシンクは…ここも設定しだいで適用。
            if (perfectSync.IsReadyToAccumulate)
            {
                //パーフェクトシンクのクリップを埋め: このとき口まわりは設定次第で0埋めか有効値で埋めるかが変化
                perfectSync.Accumulate(
                    _accumulator, true, perfectSync.PreferWriteMouthBlendShape, true
                    );

                //外部トラッキングの口形状を使わない: このときはlipSyncのほうでも
                //マイクベースのリップシンクが優先になっているので、それを適用
                if (!perfectSync.PreferWriteMouthBlendShape)
                {
                    lipSync.Accumulate(_accumulator);
                }

                neutralClipSettings.AccumulateNeutralClip(_accumulator);
                neutralClipSettings.AccumulateOffsetClip(_accumulator);
                return;
            }
            
            //上記いずれでもない: ここも分岐はあって
            // - 口: パーフェクトシンクの画像 or マイク
            // - 目: パーフェクトシンクの目 or webカメラ or AutoBlink
            // という使い分けがあるが、この分岐は各コンポーネントのレベルで面倒を見てもらえる
            eyes.Accumulate(_accumulator);
            lipSync.Accumulate(_accumulator);
            
            neutralClipSettings.AccumulateNeutralClip(_accumulator);
            neutralClipSettings.AccumulateOffsetClip(_accumulator);
        }
        
        private void WriteClipsWithInterpolation()
        {
            // 補間があるバージョンのポイント:
            // - FaceSwitch / WtMの現在値は(KeepLipSyncも含めて)使わず、Interpolatorが代行してくれる
            // - ゼロ埋めの冗長化回避は無理なので諦めて、全部ゼロ埋めしておく
            // - ふだんの動きを適用するが、そこでは必ずweightが入る
            blendShapeInterpolator.Accumulate(_accumulator);
            
            //Perfect Syncが適用 > Blinkは確定で無視。
            //リップシンクは…ここも設定しだいで適用。
            if (perfectSync.IsReadyToAccumulate)
            {
                //パーフェクトシンクのクリップを埋め: このとき口まわりは設定次第で0埋めか有効値で埋めるかが変化
                perfectSync.Accumulate(
                    _accumulator, 
                    true, 
                    perfectSync.PreferWriteMouthBlendShape, 
                    true,
                    blendShapeInterpolator.MouthWeight,
                    blendShapeInterpolator.NonMouthWeight
                );

                //外部トラッキングの口形状を使わない: このときはlipSyncのほうでも
                //マイクベースのリップシンクが優先になっているので、それを適用
                if (!perfectSync.PreferWriteMouthBlendShape)
                {
                    lipSync.Accumulate(_accumulator, blendShapeInterpolator.MouthWeight);
                }

                neutralClipSettings.AccumulateNeutralClip(_accumulator, blendShapeInterpolator.NonMouthWeight);
                neutralClipSettings.AccumulateOffsetClip(_accumulator);
                return;
            }
            
            //上記いずれでもない: ここも分岐はあって
            // - 口: パーフェクトシンクの画像 or マイク
            // - 目: パーフェクトシンクの目 or webカメラ or AutoBlink
            // という使い分けがあるが、この分岐は各コンポーネントのレベルで面倒を見てもらえる
            eyes.Accumulate(_accumulator, blendShapeInterpolator.NonMouthWeight);
            lipSync.Accumulate(_accumulator, blendShapeInterpolator.MouthWeight);
            
            neutralClipSettings.AccumulateNeutralClip(_accumulator, blendShapeInterpolator.NonMouthWeight);
            neutralClipSettings.AccumulateOffsetClip(_accumulator);
        }
    }
}
