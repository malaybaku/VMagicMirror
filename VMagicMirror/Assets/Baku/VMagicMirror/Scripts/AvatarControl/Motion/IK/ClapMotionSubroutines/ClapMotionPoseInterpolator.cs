using Baku.VMagicMirror.IK.Utils;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// 拍手モーションで、計算されたキーになるポーズどうしの姿勢の補間を行う。
    /// </summary>
    public class ClapMotionPoseInterpolator
    {
        public const float ClapApproachRate = 0.3f;
        public const float ClapStopEndRate = 0.4f;

        private readonly ClapMotionKeyPoseCalculator _keyPoseCalculator;

        //遷移順: 
        //  start -> firstDistant -> center -> distant -> center -> distant -> ...
        public HandPoses StartPoses { get; private set; }
        public HandPoses FirstDistantPoses { get; private set; }
        private HandPoses _centerPoses;
        private HandPoses _distantPoses;
        
        public ClapMotionPoseInterpolator(ClapMotionKeyPoseCalculator keyPoseCalculator)
        {
            _keyPoseCalculator = keyPoseCalculator;
        }

        /// <summary>
        /// 開始位置を指定することで、それ以降のキー姿勢を生成して補間可能にする
        /// </summary>
        /// <param name="startPose"></param>
        public void Refresh(HandPoses startPose)
        {
            StartPoses = startPose;
            var basePose = _keyPoseCalculator.GetClapBasePose();
            
            var (centerPoses, yaw) = _keyPoseCalculator.GetClapCenterPoses(basePose);
            _centerPoses = centerPoses;
            FirstDistantPoses =
                _keyPoseCalculator.GetClapDistantPoses(centerPoses, yaw, _keyPoseCalculator.LongDistance);
            _distantPoses = 
                _keyPoseCalculator.GetClapDistantPoses(centerPoses, yaw, _keyPoseCalculator.ShortDistance);
        }

        //拍手の動作スタートから、1回目の拍手の予備動作の位置に手を持っていくまでの補間
        public HandPoses GetEntryPose(float t)
        {
            //NOTE: 直線的な補間処理でいいか、というのはあるが一旦気にしない。必要な場合、何か2次ベジエとかで頑張りましょう
            var rate = DefaultEase(t);
            var from = StartPoses;
            var to = FirstDistantPoses;
            return HandPoses.Lerp(from, to, rate);
        }

        //1回目の予備動作の位置に手を持っていったあとに最初の「パチ」をやり、その手を離すまでの補間
        public HandPoses GetFirstClap(float t)
        {
            //NOTE: 時間配分的に、最初の手を叩くまでの動きだけが素早すぎる動きになるかも
            var rate = ClapEase(t);
            var from = t < 0.5f ? FirstDistantPoses : _distantPoses;
            var to = _centerPoses;
            return HandPoses.Lerp(from, to, rate);
        }

        //2回目以降の「パチ」の動き
        public HandPoses GetClap(float t)
        {
            var rate = ClapEase(t);
            var from = _distantPoses;
            var to = _centerPoses;
            return HandPoses.Lerp(from, to, rate);
        }
        
        //0 -> 1 -> 0と遷移する補間
        private static float ClapEase(float t)
        {
            // 拍手は前半が素早い動きでピシャっと手が合い、後半では加減速のある動きで戻す。
            // 前後半はミラーリングできない動きになっており、easingも違う。

            // 30% -> 手をあわせる加速
            // 10% -> 合わせた手のまま静止
            // 60% -> 加減速つきで手を戻していく
            if (t < ClapApproachRate)
            {
                //tで値が1になるような2次補間
                return Mathf.Clamp01((1f / ClapApproachRate / ClapApproachRate) * t * t);
            }
            if (t < ClapStopEndRate)
            {
                return 1f;
            }
            else
            {
                return Mathf.SmoothStep(1f, 0f, (t - ClapStopEndRate) / (1 - ClapStopEndRate));
            }
        }

        //0 -> 1と遷移する補間
        private static float DefaultEase(float t) => Mathf.SmoothStep(0f, 1f, t);
    }
}
