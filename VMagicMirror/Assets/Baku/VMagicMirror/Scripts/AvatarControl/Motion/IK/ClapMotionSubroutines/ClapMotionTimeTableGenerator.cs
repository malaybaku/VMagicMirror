using Baku.VMagicMirror.IK.Utils;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    public static class ClapMotionPhase
    {
        public const int FirstEntry = 0;
        public const int FirstClap = 1;
        public const int MainClap = 2;
        public const int EndWait = 3;
    }

    public class ClapMotionTimeTableGenerator
    {
        //拍手状態じゃない状態から手を上げるときの所要時間を考えるときに参照する、手が等速運動してよい最高速度[m/s]
        private const float MaxHandRaiseSpeed = 0.7f;
        //NOTE: 遅すぎず早すぎずの拍手が3回/sくらいであり、遅すぎるとザツに見えるので、気持ち早め
        private const float ClapDuration = 0.33f;

        private const float EndWait = 0.1f;
        
        /// <summary> 手を持ち上げはじめる部分から起算した、拍手のトータルのモーションでかかる時間 </summary>
        public float TotalDuration { get; private set; }
        
        /// <summary> 最初に手を上げて、やや離れた位置に構えるまでの所要時間 </summary>
        public float FirstEntryDuration { get; private set; }
        /// <summary> 最初の拍手の所要時間 </summary>
        public float FirstClapDuration { get; private set; }
        /// <summary> 最初以外の拍手1回あたりの所要時間 </summary>
        public float SingleClapDuration { get; private set; }
        
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
            FirstEntryDuration = diff / MaxHandRaiseSpeed;
            //NOTE: 結果的に同じ値だが、揃うという必然的な理由がないので分けている
            FirstClapDuration = ClapDuration * motionScale;
            SingleClapDuration = ClapDuration * motionScale;
            TotalDuration = FirstEntryDuration + FirstClapDuration + SingleClapDuration * (clapCount - 1);
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

            if (t > TotalDuration - EndWait)
            {
                return (ClapMotionPhase.EndWait, 0f);
            }

            var rate = Mathf.Repeat(
                (t - FirstEntryDuration - FirstClapDuration) / SingleClapDuration,
                1
            );
            return (ClapMotionPhase.MainClap, rate);
        }
    }
}
