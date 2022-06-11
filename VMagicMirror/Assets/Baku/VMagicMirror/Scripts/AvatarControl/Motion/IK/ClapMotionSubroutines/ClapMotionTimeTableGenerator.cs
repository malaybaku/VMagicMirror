using Baku.VMagicMirror.IK.Utils;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    public static class ClapMotionPhase
    {
        public const int FirstEntry = 0;
        public const int FirstClap = 1;
        public const int MainClap = 2;
        public const int LastClap = 3;
    }

    public class ClapMotionTimeTableGenerator
    {
        //拍手状態じゃない状態から手を上げるときの所要時間を考えるうえで、
        //首の高さをベースに「その値の何倍くらいの速度が[m/s]単位で出せるか」を指定するファクター
        private const float HeadHeightToHandRaiseSpeedFactor = 0.7f;
        //NOTE: 遅すぎず早すぎずの拍手が3回/sくらい。
        private const float ClapDuration = 0.4f;

        private const float EndWait = 0.2f;
        private const float LastClapOpenMotionSlowFactor = 1.7f;
        private const float LastClapOpenStateQuitFactor = 0.5f;

        //NOTE:
        public float HeadHeight { get; set; } = 1f;
        
        /// <summary> モーション全体の時間。ステートを抜けたあとにもフォロー動作がある。 </summary>
        public float TotalDuration { get; private set; }

        /// <summary> 拍手以外のステートに遷移し始めてよい時間 </summary>
        public float MotionStateDuration { get; private set; }
        
        /// <summary> 最初に手を上げて、やや離れた位置に構えるまでの所要時間 </summary>
        public float FirstEntryDuration { get; private set; }
        /// <summary> 最初の拍手の所要時間 </summary>
        public float FirstClapDuration { get; private set; }
        /// <summary> 最初以外の拍手1回あたりの所要時間 </summary>
        public float SingleClapDuration { get; private set; }
        

        private float _lastClapStartTime;
        private float _lastClapDuration;
        
        /// <summary>
        /// 初期アプローチの状態と拍手の回数を指定することで、拍手全体の所要時間や、
        /// </summary>
        /// <param name="start"></param>
        /// <param name="first"></param>
        /// <param name="clapCount"></param>
        /// <param name="motionScale"></param>
        public void Calculate(HandPoses start, HandPoses first, int clapCount, float motionScale)
        {
            var leftDiff = Vector3.Distance(start.Left.Position, first.Left.Position);
            var rightDiff = Vector3.Distance(start.Right.Position, first.Right.Position);
            var diff = Mathf.Max(leftDiff, rightDiff);

            //加減速を考えたほうが更に妥当な値になるが、まあOKということで
            FirstEntryDuration = diff / (HeadHeight * HeadHeightToHandRaiseSpeedFactor);
            //NOTE: 結果的に同じ値だが、揃うという必然的な理由がないので分けている
            FirstClapDuration = ClapDuration * motionScale;
            SingleClapDuration = ClapDuration * motionScale;

            //NOTE: 最後の拍手は「等速で手を合わす + 停止 + ゆっくり戻す」という処理を入れるので計算が独特
            _lastClapDuration =
                SingleClapDuration * ClapMotionPoseInterpolator.ClapApproachRate +
                EndWait +
                SingleClapDuration * (1 - ClapMotionPoseInterpolator.ClapApproachRate) * LastClapOpenMotionSlowFactor;
            
            var lastClapStateQuitDuration = 
                SingleClapDuration * ClapMotionPoseInterpolator.ClapApproachRate +
                EndWait +
                SingleClapDuration * (1 - ClapMotionPoseInterpolator.ClapApproachRate) * LastClapOpenStateQuitFactor;

            MotionStateDuration =
                FirstEntryDuration + FirstClapDuration +
                SingleClapDuration * (clapCount - 2) +
                lastClapStateQuitDuration;

            TotalDuration = 
                FirstEntryDuration + FirstClapDuration +
                SingleClapDuration * (clapCount - 2) +
                _lastClapDuration;

            _lastClapStartTime = TotalDuration - _lastClapDuration;
        }

        /// <summary>
        /// 拍手開始からの秒数に対して、どのモーションが実行中で進行率がいくらなのかを返します。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public (int motionPhase, float rate) GetPhaseAndRate(float t)
        {
            if (t < FirstEntryDuration)
            {
                return (
                    ClapMotionPhase.FirstEntry, 
                    FirstEntryDuration > 0 ? t / FirstEntryDuration : 0
                    );
            }

            if (t < FirstEntryDuration + FirstClapDuration)
            {
                return (
                    ClapMotionPhase.FirstClap,
                    Mathf.Clamp01((t - FirstEntryDuration) / FirstClapDuration)
                );
            }

            if (t > _lastClapStartTime)
            {
                var localTime = t - _lastClapStartTime;
                var approachTime = SingleClapDuration * ClapMotionPoseInterpolator.ClapApproachRate;
                if (localTime < approachTime)
                {
                    return (ClapMotionPhase.LastClap, localTime / SingleClapDuration);
                }
                else if (localTime < approachTime + EndWait)
                {
                    return (ClapMotionPhase.LastClap, ClapMotionPoseInterpolator.ClapApproachRate);
                }
                else
                {
                    var localRate = 
                        (localTime - approachTime - EndWait) / (LastClapOpenMotionSlowFactor * SingleClapDuration);
                    return (ClapMotionPhase.LastClap, localRate + ClapMotionPoseInterpolator.ClapApproachRate);
                }
            }

            var rate = Mathf.Repeat(
                (t - FirstEntryDuration - FirstClapDuration) / SingleClapDuration,
                1
            );
            return (ClapMotionPhase.MainClap, rate);
        }
    }
}
