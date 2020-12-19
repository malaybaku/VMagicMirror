using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baku.VMagicMirror.MotionExporter;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// カスタムモーションをいい感じに管理するクラス。
    /// 初期化から何から結構トリッキーなので要注意
    /// </summary>
    public class CustomMotionPlayer : MonoBehaviour
    {
        //NOTE: キーはファイル名から拡張子を抜いて小文字にしたやつ。
        //ただし、WPF側には小文字化してない文字を渡すので、その食い違いにはちょっと注意。
        private readonly Dictionary<string, CustomMotionItem> _clips = new Dictionary<string, CustomMotionItem>();

        [SerializeField] private SimpleAnimation simpleAnimaton = null;
        [SerializeField] private HumanoidAnimationSetter source = null;

        private bool _hasModel = false;
        private HumanPoseHandler _humanPoseHandler = null;
        private HumanPose _humanPose;

        //いま実行中のモーション名。不要かもしれないけど一応
        private string _currentMotionName = "";
        private float _currentMotionPlayCountDown = 0f;
        
        private bool _shouldUpdate = false;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _humanPoseHandler = new HumanPoseHandler(info.animator.avatar, info.vrmRoot);
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _humanPoseHandler = null;
            };
        }
        
        public void LoadMotionsFromFile()
        {
            _clips.Clear();
            
            //エディタの場合はStreamingAssets以下で代用(無ければ無いでOK)
            string path = Application.isEditor 
                ? Path.Combine(
                    Application.streamingAssetsPath, "Motions")
                : Path.Combine(
                    Path.GetDirectoryName(Application.dataPath),
                    "Motions"
                );
                
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }            

            var importer = new MotionImporter();
            foreach(var filePath in Directory.GetFiles(path).Where(p => Path.GetExtension(p) == ".vmm_motion"))
            {
                try
                {
                    var motion = importer.LoadSerializedMotion(File.ReadAllText(filePath));
                    var motionName = Path.GetFileNameWithoutExtension(path);
                    _clips[motionName.ToLower()] = new CustomMotionItem(
                        motionName,
                        motion.LoadMuscleFlags(),
                        importer.Deserialize(motion)
                    );
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }
        
        /// <summary>
        /// NOTE: プレビューが有効な限り、毎回呼び出してもOK
        /// </summary>
        /// <param name="motionName"></param>
        public bool PlayClipForPreview(string motionName)
        {
            if (!_hasModel || _currentMotionName == motionName.ToLower())
            {
                return false;
            }
            
            StopCurrentMotion();
            return StartMotion(motionName);
        }

        /// <summary>
        /// NOTE: プレビューと異なり、同じモーションを指定された場合は無視せずに最初からもう一回やる
        /// </summary>
        /// <param name="motionName"></param>
        public bool PlayClip(string motionName)
        {
            StopCurrentMotion();    
            return StartMotion(motionName);
        }

        public void StopPreviewMotion() => StopCurrentMotion();

        public void StopCurrentMotion()
        {
            if (!string.IsNullOrEmpty(_currentMotionName))
            {
                simpleAnimaton.Stop(_currentMotionName);
            }
            _currentMotionName = "";
            _shouldUpdate = false;
        }

        //NOTE: 1.0fを返すのは0だとなんかヤバそうだからだけど、そもそも普通は呼ばれないはず。
        public float GetMotionDuration(string motionName)
            => _clips.TryGetValue(motionName.ToLower(), out var item) ? item.Clip.length : 1.0f;

        //名前は固定しておく、ランダムになってると困るので
        public string[] LoadAvailableCustomMotionNames() => _clips
            .Values
            .Select(v => v.MotionName)
            .OrderBy(x => x)
            .ToArray();

        private void Update()
        {
            if (!_shouldUpdate)
            {
                return;
            }
            
            //初期姿勢の案として、クリップで再生されてセットされたはずの姿勢を当て込んでいく
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            source.WriteToArray();
            source.WriteToPose(ref _humanPose);
            _humanPoseHandler.SetHumanPose(ref _humanPose);
            
            //TODO: 観察してた感じだとHipsの位置がずれる問題とかが要対策のような
        }

        private bool StartMotion(string motionName)
        {
            if (_currentMotionName == motionName.ToLower())
            {
                //すでに実行してる = 不要
                return false;
            }
        
            StopCurrentMotion();
            if (!_clips.TryGetValue(motionName.ToLower(), out var item))
            {
                return false;
            }

            if (simpleAnimaton.GetState(item.MotionLowerName) == null)
            {
                simpleAnimaton.AddState(item.Clip, item.MotionLowerName);
            }

            source.SetUsedFlags(item.UsedFlags);
            simpleAnimaton.Play(item.MotionLowerName);
            _currentMotionName = item.MotionLowerName;
            _currentMotionPlayCountDown = item.Clip.length;
            _shouldUpdate = true;

            return true;
        }

        class CustomMotionItem
        {
            public CustomMotionItem(string motionName, bool[] usedFlags, AnimationClip clip)
            {
                MotionName = motionName;
                MotionLowerName = motionName.ToLower();
                UsedFlags = usedFlags;
                Clip = clip;
            }
            
            /// <summary> カスタムモーションを一意に指定するのに使う文字列 </summary>
            public string MotionLowerName { get; }
            
            /// <summary> WPF側に渡す文字列で、実態はファイル名から拡張子を抜いたもの </summary>
            public string MotionName { get; }
            
            /// <summary> マッスルごとに、そのマッスルがアニメーション対象かどうかを示したフラグ </summary>
            public bool[] UsedFlags { get; }
            
            /// <summary> 実際に再生するアニメーションクリップ </summary>
            public AnimationClip Clip { get; }
        }

    }
    
}
