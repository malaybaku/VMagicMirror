using UnityEngine;

namespace Baku.VMagicMirror.MotionExporter
{
    
    /// <summary>
    /// Humanoid Animationのパラメータをフィールドで受け取るクラス。
    /// </summary>
    public class HumanoidAnimationSetter : MonoBehaviour
    {
        #region IK系のパラメータ
        
        //NOTE: 現行実装では活用できてないパラメータですが、データ上は存在するはずなので今のうちから用意しています
        
        public Vector3 RootT;
        public Quaternion RootQ;

        public Vector3 MotionT;
        public Quaternion MotionQ;

        public Vector3 LeftFootT;
        public Quaternion LeftFootQ;
        public Vector3 RightFootT;
        public Quaternion RightFootQ;

        public Vector3 LeftHandT;
        public Quaternion LeftHandQ;
        public Vector3 RightHandT;
        public Quaternion RightHandQ;

        #endregion
        
        /// <summary>
        /// 必要になった時点でHumanPoseのmuscleに書き込むマッスル配列です。
        /// </summary>
        public float[] MuscleArray { get; } = new float[95];
        private readonly bool[] _usedMuscleFlags = new bool[95];

        /// <summary>
        /// どのマッスル情報を<see cref="WriteToPose"/>で転写すべきかのフラグ一覧をセットします。
        /// </summary>
        /// <param name="flags"></param>
        public void SetUsedFlags(bool[] flags)
        {
            if (flags == null || flags.Length != _usedMuscleFlags.Length)
            {
                return;
            }
            
            for (int i = 0; i < _usedMuscleFlags.Length; i++)
            {
                _usedMuscleFlags[i] = flags[i];
            }
        }
        
        /// <summary>
        /// 現在のマッスル情報を実際のアバターの姿勢に書き込みます。
        /// </summary>
        /// <param name="pose"></param>
        public void WriteToPose(ref HumanPose pose)
        {
            for (int i = 0; i < MuscleArray.Length; i++)
            {
                if (_usedMuscleFlags[i])
                {
                    pose.muscles[i] = MuscleArray[i];
                }
            }
        }
        
        /// <summary>
        /// 現在のマッスル情報を、指定した比率で実際のアバター姿勢に書き込みます。
        /// 0を指定すると何もせず、1を指定するとrateを指定しない場合と同じ動きになります。
        /// </summary>
        /// <param name="pose"></param>
        /// <param name="rate"></param>
        public void WriteToPose(ref HumanPose pose, float rate)
        {
            for (int i = 0; i < MuscleArray.Length; i++)
            {
                if (_usedMuscleFlags[i])
                {
                    pose.muscles[i] = Mathf.Lerp(pose.muscles[i], MuscleArray[i], rate);
                }
            }
        }
    }
}
