using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror.MotionExporter
{
    public class MotionTestPlay : MonoBehaviour
    {
        [SerializeField] private string fileName;
        [SerializeField] private HumanoidAnimationSetter source;
        [SerializeField] private Animator target;
        [SerializeField] private bool onlyUpperBody = false;

        private HumanPoseHandler _humanPoseHandler = null;
        private HumanPose _humanPose;

        private Transform _hips;
        private Vector3 _originHipPos;
        private Quaternion _originHipRot;

        private DeserializedMotionClip _motion;
        private float _motionElapsedTime = 0f;

        private void Start()
        {
            _humanPoseHandler = new HumanPoseHandler(target.avatar, target.transform);
            _hips = target.GetBoneTransform(HumanBodyBones.Hips);
            _originHipPos = _hips.localPosition;
            _originHipRot = _hips.localRotation;

            var fileNameWithExt = fileName.EndsWith(".vmm_motion") ? fileName : fileName + ".vmm_motion";
            var filePath = Path.Combine(Application.streamingAssetsPath, fileNameWithExt);
            var json = File.ReadAllText(filePath);
            
            var importer = new MotionImporter();
            var serializedMotion = importer.LoadSerializedMotion(json);
            var muscleFlags = serializedMotion.LoadMuscleFlags();
            if (onlyUpperBody)
            {
                SerializeMuscleNameMapper.MaskUsedMuscleFlags(muscleFlags, MuscleFlagMaskStyle.OnlyUpperBody);
            }

            _motion = importer.Deserialize(serializedMotion);
            _motion.Target = source;
            source.SetUsedFlags(muscleFlags);
        }

        private void Update()
        {
            if (_motionElapsedTime > _motion.Duration)
            {
                //何もしない: モーションのclamp相当の状態を保持するのに相当する。
            }
            else
            {
                _motion.Evaluate(_motionElapsedTime);
                _motionElapsedTime += Time.deltaTime;
            }

            _humanPoseHandler.GetHumanPose(ref _humanPose);
            source.WriteToPose(ref _humanPose);
            _humanPoseHandler.SetHumanPose(ref _humanPose);

            //何も対策しないとhipsが徐々にずれることが多いので、それを防ぐ
            _hips.localPosition = _originHipPos;
            //_hips.localRotation = _originHipRot;
        }
    }
}
