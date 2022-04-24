using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using VRM;
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
        private BlendShapeInitializer _initializer = null;
        private WordToMotionBlendShape _wtmBlendShape = null;

        private bool _hasModel = false;
        private VRMBlendShapeProxy _blendShape = null;
        
        //NOTE: ここのコンポーネントの書き順は実は優先度を表している: 後ろのやつほど上書きの権利が強い
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable, 
            ExternalTrackerDataSource exTracker,
            BlendShapeInitializer initializer,
            WordToMotionBlendShape wtmBlendShape
            )
        {
            _exTracker = exTracker;
            _initializer = initializer;
            _wtmBlendShape = wtmBlendShape;
            
            vrmLoadable.VrmLoaded += info =>
            {
                _blendShape = info.blendShape;
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _blendShape = null;
            };
            
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

            //NOTE: 関数をわざわざ分けるのは後方互換性のため + ウェイトに配慮しないほうが処理が一回り軽い見込みのため
            if (blendShapeInterpolator.NeedToInterpolate)
            {
                WriteClipsWithInterpolation();
            }
            else
            {
                WriteClips();
            }
            _blendShape.Apply();
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
                    _initializer.InitializeBlendShapes();
                    _wtmBlendShape.Accumulate(_blendShape);
                }
                else if (_exTracker.Connected && perfectSync.IsActive && perfectSync.PreferWriteMouthBlendShape)
                {
                    //WtM + パーフェクトシンクの口周りを適用するケース: 口周りのゼロ埋めをサボれるのでサボる
                    _initializer.InitializeBlendShapes(perfectSync.NonPerfectSyncKeys);
                    _initializer.InitializeBlendShapes(ExternalTrackerPerfectSync.Keys.PerfectSyncNonMouthKeys);
                    _wtmBlendShape.Accumulate(_blendShape);
                    perfectSync.Accumulate(_blendShape, false, true, false);
                }
                else
                {
                    //WtM + AIUEOの口を適用するケース: 重複がAIUEOの5個だけなのでザツにやっちゃう
                    _initializer.InitializeBlendShapes();
                    _wtmBlendShape.Accumulate(_blendShape);
                    lipSync.Accumulate(_blendShape);
                }
                
                neutralClipSettings.ApplyOffsetClip(_blendShape);
                return;
            }
            
            //FaceSwitchが適用 > PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (faceSwitch.HasClipToApply)
            {
                //NOTE: WtMと同じく、パーフェクトシンクの口と組み合わす場合のコストに多少配慮した書き方。
                if (!faceSwitch.KeepLipSync)
                {
                    _initializer.InitializeBlendShapes();
                    faceSwitch.Accumulate(_blendShape);
                }
                else if (_exTracker.Connected && perfectSync.IsActive && perfectSync.PreferWriteMouthBlendShape)
                {
                    //Face Switch + パーフェクトシンクの口周りを適用: 口周りのパーフェクトシンクのゼロ埋めをサボれるのでサボる
                    _initializer.InitializeBlendShapes(perfectSync.NonPerfectSyncKeys);
                    _initializer.InitializeBlendShapes(ExternalTrackerPerfectSync.Keys.PerfectSyncNonMouthKeys);
                    faceSwitch.Accumulate(_blendShape);
                    perfectSync.Accumulate(_blendShape, false, true, false);
                }
                else
                {
                    //FaceSwitch + AIUEOを適用するケース: 重複がAIUEOの5個だけなのでザツにやっちゃう
                    _initializer.InitializeBlendShapes();
                    faceSwitch.Accumulate(_blendShape);
                    lipSync.Accumulate(_blendShape);
                }

                neutralClipSettings.ApplyOffsetClip(_blendShape);
                return;
            }
            
            //Perfect Syncが適用 > Blinkは確定で無視。
            //リップシンクは…ここも設定しだいで適用。
            if (perfectSync.IsReadyToAccumulate)
            {
                //パーフェクトシンクじゃないクリップを0埋め
               _initializer.InitializeBlendShapes(perfectSync.NonPerfectSyncKeys);

                //パーフェクトシンクのクリップを埋め: このとき口まわりは設定次第で0埋めか有効値で埋めるかが変化
                perfectSync.Accumulate(
                    _blendShape, true, perfectSync.PreferWriteMouthBlendShape, true
                    );

                //外部トラッキングの口形状を使わない: このときはlipSyncのほうでも
                //マイクベースのリップシンクが優先になっているので、それを適用
                if (!perfectSync.PreferWriteMouthBlendShape)
                {
                    lipSync.Accumulate(_blendShape);
                }

                neutralClipSettings.ApplyNeutralClip(_blendShape);
                neutralClipSettings.ApplyOffsetClip(_blendShape);
                return;
            }
            
            //上記いずれでもない: ここも分岐はあって
            // - 口: パーフェクトシンクの画像 or マイク
            // - 目: パーフェクトシンクの目 or webカメラ or AutoBlink
            // という使い分けがあるが、この分岐は各コンポーネントのレベルで面倒を見てもらえる
            _initializer.InitializeBlendShapes();
            eyes.Accumulate(_blendShape);
            lipSync.Accumulate(_blendShape);
            
            neutralClipSettings.ApplyNeutralClip(_blendShape);
            neutralClipSettings.ApplyOffsetClip(_blendShape);
        }
        
        private void WriteClipsWithInterpolation()
        {
            // 補間があるバージョンのポイント:
            // - FaceSwitch / WtMの現在値は(KeepLipSyncも含めて)使わず、Interpolatorが代行してくれる
            // - ゼロ埋めの冗長化回避は無理なので諦めて、全部ゼロ埋めしておく
            // - ふだんの動きを適用するが、そこでは必ずweightが入る

            _initializer.InitializeBlendShapes();
            blendShapeInterpolator.Accumulate(_blendShape);
            
            //Perfect Syncが適用 > Blinkは確定で無視。
            //リップシンクは…ここも設定しだいで適用。
            if (perfectSync.IsReadyToAccumulate)
            {
                //パーフェクトシンクのクリップを埋め: このとき口まわりは設定次第で0埋めか有効値で埋めるかが変化
                perfectSync.Accumulate(
                    _blendShape, 
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
                    lipSync.Accumulate(_blendShape, blendShapeInterpolator.MouthWeight);
                }

                neutralClipSettings.ApplyNeutralClip(_blendShape, blendShapeInterpolator.NonMouthWeight);
                neutralClipSettings.ApplyOffsetClip(_blendShape, blendShapeInterpolator.NonMouthWeight);
                return;
            }
            
            //上記いずれでもない: ここも分岐はあって
            // - 口: パーフェクトシンクの画像 or マイク
            // - 目: パーフェクトシンクの目 or webカメラ or AutoBlink
            // という使い分けがあるが、この分岐は各コンポーネントのレベルで面倒を見てもらえる
            eyes.Accumulate(_blendShape, blendShapeInterpolator.NonMouthWeight);
            lipSync.Accumulate(_blendShape, blendShapeInterpolator.MouthWeight);
            
            neutralClipSettings.ApplyNeutralClip(_blendShape, blendShapeInterpolator.NonMouthWeight);
            neutralClipSettings.ApplyOffsetClip(_blendShape, blendShapeInterpolator.NonMouthWeight);
        }
    }
}
