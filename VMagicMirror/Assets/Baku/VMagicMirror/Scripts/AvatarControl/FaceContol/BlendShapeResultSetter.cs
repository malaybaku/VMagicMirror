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

        private BlendShapeInitializer _initializer = null;
        private WordToMotionBlendShape _wtmBlendShape = null;

        private bool _hasModel = false;
        private VRMBlendShapeProxy _blendShape = null;


        //NOTE: ここのコンポーネントの書き順は実は優先度を表している: 後ろのやつほど上書きの権利が強い
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            BlendShapeInitializer initializer,
            WordToMotionBlendShape wtmBlendShape
            )
        {
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
        }

        
        private void LateUpdate()
        {
            if (!_hasModel)
            {
                return;
            }
            
            WriteClips();
            _blendShape.Apply();                        
        }

        private void WriteClips()
        {
            //Word to Motionが適用 > FaceSwitch、PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (_wtmBlendShape.HasBlendShapeToApply)
            {
                //NOTE: VRoidのデフォルト設定クリップが上乗せされた場合、
                //それはwtmBlendShapeでは考慮してもらえないので、ここで0上書きする
                if (perfectSync.IsActive && perfectSync.UseVRoidDefaultSetting)
                {
                    _initializer.InitializeBlendShapes(perfectSync.ProgramaticallyAddedVRoidClipKeys);
                }
                
                //TODO: このAccumulateでリップシンク続行オプションがついてるとき、
                //AIUEOだけじゃなくてパーフェクトシンクのクリップも書くのをスキップしたいよね
                _wtmBlendShape.Accumulate(_blendShape);
                if (_wtmBlendShape.SkipLipSyncKeys)
                {
                    if (perfectSync.IsActive && perfectSync.PreferWriteMouthBlendShape)
                    {
                        perfectSync.Accumulate(_blendShape, false, true, false);
                    }
                    else
                    {
                        lipSync.Accumulate(_blendShape);
                    }
                }
                return;
            }
            
            //FaceSwitchが適用 > PerfectSync、Blinkは確定で無視。
            //リップシンクは設定しだいで適用。
            if (faceSwitch.HasClipToApply)
            {
                //NOTE: この場合、InitializerでぜんぶInitializeしたあと高々6個だけが重複で適用される。
                //これはパフォーマンス影響が十分小さそうなのでOKとする…と思ったが、パーフェクトシンクの口が適用される場合はちょっと重いよ。
                _initializer.InitializeBlendShapes();
                faceSwitch.Accumulate(_blendShape);
                if (faceSwitch.KeepLipSync)
                {
                    if (perfectSync.IsActive && perfectSync.PreferWriteMouthBlendShape)
                    {
                        perfectSync.Accumulate(_blendShape, false, true, false);
                    }
                    else
                    {
                        lipSync.Accumulate(_blendShape);
                    }
                }
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
                    _blendShape, 
                    true, 
                    perfectSync.PreferWriteMouthBlendShape, 
                    true
                );

                //外部トラッキングの口形状を使わない: このときはlipSyncのほうでも
                //マイクベースのリップシンクが優先になっているので、それを適用
                if (!perfectSync.PreferWriteMouthBlendShape)
                {
                    lipSync.Accumulate(_blendShape);
                }

                return;
            }
            
            //上記いずれでもない: ここも分岐はあって
            // - 口: パーフェクトシンクの画像 or マイク
            // - 目: パーフェクトシンクの目 or webカメラ or AutoBlink
            // という使い分けがあるが、この分岐は各コンポーネントのレベルで面倒を見てもらえる

            //NOTE: リップシンクの値も0埋めする。
            //半端に飛ばそうとするとBlendShapeKeyのEquality計算が走ってめちゃくちゃ遅くなるため。
            _initializer.InitializeBlendShapes();
            eyes.Accumulate(_blendShape);
            lipSync.Accumulate(_blendShape);
        }
    }
}
