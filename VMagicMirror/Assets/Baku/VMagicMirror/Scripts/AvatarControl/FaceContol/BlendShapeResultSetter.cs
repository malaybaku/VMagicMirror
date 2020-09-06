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
        private bool _hasModel = false;
        private VRMBlendShapeProxy _blendShape = null;

        private BlendShapeInitializer _initializer = null;
        private LipSyncIntegrator _lipSync = null;
        private FaceControlManager _eyes = null;
        private ExternalTrackerPerfectSync _perfectSync = null;
        private ExternalTrackerFaceSwitchApplier _faceSwitch = null;
        private WordToMotionBlendShape _wtmBlendShape = null;

        //NOTE: ここのコンポーネントの書き順は実は優先度を表している: 後ろのやつほど上書きの権利が強い
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            BlendShapeInitializer initializer,
            LipSyncIntegrator lipSync, 
            FaceControlManager faceControl, 
            ExternalTrackerPerfectSync perfectSync, 
            ExternalTrackerFaceSwitchApplier faceSwitch,
            WordToMotionBlendShape wtmBlendShape
            )
        {
            _initializer = initializer;
            _lipSync = lipSync;
            _eyes = faceControl;
            _perfectSync = perfectSync;
            _faceSwitch = faceSwitch;
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
        }

        
        private void LateUpdate()
        {
            if (!_hasModel)
            {
                return;
            }
            
            //Word to Motionが適用 > FaceSwitch、PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (_wtmBlendShape.HasBlendShapeToApply)
            {
                //NOTE: VRoidのデフォルト設定クリップが上乗せされた場合、
                //それはwtmBlendShapeでは考慮してもらえないので、ここで0上書きする
                if (_perfectSync.IsActive && _perfectSync.UseVRoidDefaultSetting)
                {
                    _initializer.InitializeBlendShapes(_perfectSync.ProgramaticallyAddedVRoidClipKeys);
                }
                
                _wtmBlendShape.Accumulate(_blendShape);
                if (_wtmBlendShape.SkipLipSyncKeys)
                {
                    _lipSync.Accumulate(_blendShape);
                }
                return;
            }
            
            //FaceSwitchが適用 > PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (_faceSwitch.HasClipToApply)
            {
                //NOTE: この場合、InitializerでぜんぶInitializeしたあと高々6個だけが重複で適用される。
                //これはパフォーマンス影響が十分小さそうなのでOKとする
                _initializer.InitializeBlendShapes(false);
                _faceSwitch.Accumulate(_blendShape);
                if (_faceSwitch.KeepLipSync)
                {
                    _lipSync.Accumulate(_blendShape);
                }

                return;
            }
            
            //Perfect Syncが適用 > Blinkは確定で無視。
            //リップシンクは…ここも設定しだい。
            if (_perfectSync.IsReadyToAccumulate)
            {
                //パーフェクトシンクじゃないクリップを0埋め
               _initializer.InitializeBlendShapes(_perfectSync.NonPerfectSyncKeys);

                //パーフェクトシンクのクリップを埋め: このとき口まわりは設定次第で0埋めか有効値で埋めるかが変化
                _perfectSync.Accumulate(
                    _blendShape, 
                    true, 
                    _perfectSync.PreferWriteMouthBlendShape, 
                    true
                );

                //外部トラッキングの口形状を使わない: このときはlipSyncのほうでマイクベースのリップシンクを取ってるはずなので、それを当てる
                if (!_perfectSync.PreferWriteMouthBlendShape)
                {
                    _lipSync.Accumulate(_blendShape);
                }

                return;
            }
            
            //上記いずれでもない: ここも分岐はあるんだけど
            // - 口: パーフェクトシンクの画像 or マイク
            // - 目: パーフェクトシンクの目 or webカメラ or AutoBlink
            // となっていて、ここの切り分けは別コンポーネントがお世話してくれる

            //BlinkL/Rだけ2回書き込む: 他は1回ずつになる。
            _initializer.InitializeBlendShapes(true);
            _eyes.Accumulate(_blendShape);
            _lipSync.Accumulate(_blendShape);
            
        }
    }
}
