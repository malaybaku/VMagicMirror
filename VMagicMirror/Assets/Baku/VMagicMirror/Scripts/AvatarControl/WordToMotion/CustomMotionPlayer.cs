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
    /// カスタムモーションをいい感じに管理するクラス。初期化から何から結構トリッキーなので要注意
    /// </summary>
    public class CustomMotionPlayer : MonoBehaviour
    {
        //NOTE: キーはファイル名から拡張子を抜いて小文字にしたやつ。
        //ただし、WPF側には小文字化してない文字を渡すので、その食い違いにはちょっと注意。
        private readonly Dictionary<string, CustomMotionItem> _clips = new Dictionary<string, CustomMotionItem>();

        [SerializeField] private HumanoidAnimationSetter source = null;

        private bool _hasModel = false;
        private HumanPoseHandler _humanPoseHandler = null;
        private HumanPose _humanPose;
        
        //アニメーション中の位置をどうにかせんといけないので…
        private Transform _vrmRoot;
        private Transform _hips;

        private Vector3 _originHipsPos;
        private Quaternion _originHipsRot;

        //いま実行中のモーション名。不要かもしれないけど一応
        private string _currentMotionName = "";
        private CustomMotionItem _currentMotionItem = null;
        private float _currentMotionElapsedTime = 0f;
        
        private bool _shouldUpdate = false;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _humanPoseHandler = new HumanPoseHandler(info.animator.avatar, info.vrmRoot);
                _vrmRoot = info.vrmRoot;
                _hips = info.animator.GetBoneTransform(HumanBodyBones.Hips);
                _originHipsPos = _hips.localPosition;
                _originHipsRot = _hips.localRotation;
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _humanPoseHandler = null;
            };
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

        /// <summary>
        /// 現在アニメーションをしているかどうかによらず、デフォルト状態になるようにします。
        /// 特に終了時のモーションブレンドをうまくするために呼び出す事を想定しています。
        /// </summary>
        /// <param name="duration"></param>
        public void FadeToDefaultPose(float duration)
        {
            _clipEraseDuration = duration;
            _clipEraseCount = 0f;
            _isErasingCurrentClip = true;
        }

        //現在割当たってるクリップの値を何割くらいMuscleに適用するか、というのを1から0に下げてくときに使う値。
        //フェードアウトをコードベースでやるのに使う
        private float _clipEraseCount;
        private float _clipEraseDuration;
        private bool _isErasingCurrentClip;

        /// <summary> 今やっているモーションを直ちに完全に停止します。 </summary>
        public void StopCurrentMotion()
        {
            _currentMotionItem = null;
            _currentMotionName = "";
            _currentMotionElapsedTime = 0f;
            _shouldUpdate = false;

            _isErasingCurrentClip = false;            
        }

        //NOTE: 1.0fを返すのは0だとなんかヤバそうだからだけど、そもそも普通は呼ばれないはず。
        public float GetMotionDuration(string motionName)
            => _clips.TryGetValue(motionName.ToLower(), out var item) ? item.Motion.Duration : 1.0f;

        //名前は固定しておく、ランダムになってると困るので
        public string[] LoadAvailableCustomMotionNames() => _clips
            .Values
            .Select(v => v.MotionName)
            .OrderBy(x => x)
            .ToArray();

        
        private void Start()
        {
            //エディタの場合はStreamingAssets以下で代用(無ければ無いでOK)
            var dirPath = SpecialFiles.MotionsDirectory;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }            

            var importer = new MotionImporter();
            foreach(var filePath in Directory.GetFiles(dirPath).Where(p => Path.GetExtension(p) == ".vmm_motion"))
            {
                try
                {
                    var motion = importer.LoadSerializedMotion(File.ReadAllText(filePath));
                    if (motion == null)
                    {
                        continue;
                    }
                    var motionName = Path.GetFileNameWithoutExtension(filePath);
                    var flags = motion.LoadMuscleFlags();
                    SerializeMuscleNameMapper.MaskUsedMuscleFlags(flags, MuscleFlagMaskStyle.OnlyUpperBody);
                    _clips[motionName.ToLower()] = new CustomMotionItem(
                        motionName,
                        flags,
                        importer.Deserialize(motion)
                    );
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }
        
        private void LateUpdate()
        {
            if (!_shouldUpdate)
            {
                return;
            }
            
            if (_currentMotionElapsedTime < _currentMotionItem.Motion.Duration)
            {
                _currentMotionItem.Motion.Evaluate(_currentMotionElapsedTime);
                _currentMotionElapsedTime += Time.deltaTime;
            }

            if (_isErasingCurrentClip && _clipEraseCount < _clipEraseDuration)
            {
                _clipEraseCount += Time.deltaTime;
            }
            
            //クリップで再生されてセットされたはずの姿勢を当て込んでいく
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            //ブレンド処理のフラグが経ってる場合、適用率が0になったり100%未満になったりする
            if (_isErasingCurrentClip)
            {
                //NOTE: ブレンド中だけちゃんとブレンドする。時間が十分経ったらそもそも一切のブレンドが不要
                if (_clipEraseCount < _clipEraseDuration)
                {
                    var rate = 1f - _clipEraseCount / _clipEraseDuration;
                    source.WriteToPose(ref _humanPose, rate);
                }
            }
            else
            {
                source.WriteToPose(ref _humanPose); 
            }
 
            _humanPoseHandler.SetHumanPose(ref _humanPose);

            //NOTE: hipsは固定しないとどんどんズレる事があるのを確認したため、安全のために固定してます
            _hips.localPosition = _originHipsPos;
            _hips.localRotation = _originHipsRot;
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

            source.SetUsedFlags(item.UsedFlags);
            _currentMotionItem = item;
            _currentMotionElapsedTime = 0f;
            _currentMotionName = item.MotionLowerName;
            _currentMotionItem.Motion.Target = source;
            _shouldUpdate = true;

            return true;
        }

        class CustomMotionItem
        {
            public CustomMotionItem(string motionName, bool[] usedFlags, DeserializedMotionClip motion)
            {
                MotionName = motionName;
                MotionLowerName = motionName.ToLower();
                UsedFlags = usedFlags;
                Motion = motion;
            }
            
            /// <summary> カスタムモーションを一意に指定するのに使う文字列 </summary>
            public string MotionLowerName { get; }
            
            /// <summary> WPF側に渡す文字列で、実態はファイル名から拡張子を抜いたもの </summary>
            public string MotionName { get; }
            
            /// <summary> マッスルごとに、そのマッスルがアニメーション対象かどうかを示したフラグ </summary>
            public bool[] UsedFlags { get; }
            
            /// <summary> 実際に再生するモーション </summary>
            public DeserializedMotionClip Motion { get; }
        }

    }
    
}
