using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Muscleベースで指の値をHumanPoseに書き込む処理
    /// </summary>
    /// <remarks>
    /// ref: https://gist.github.com/neon-izm/0637dac7a29682de916cecc0e8b037b0
    /// </remarks>
    public static class FingerMuscleSetter
    {
        /// <summary>
        /// 指定したポーズ構造体に、指定した指の曲げ率を差分ベースで指定してポーズ値を書き込みます。
        /// </summary>
        /// <param name="sourcePose"></param>
        /// <param name="pose"></param>
        /// <param name="fingerNumber"></param>
        /// <param name="rate"></param>
        public static void BendFinger(in HumanPose sourcePose, ref HumanPose pose, int fingerNumber, float rate)
        {
            //めちゃ呼び出し頻度が高いはずの関数なので気持ち早くなりそうにfor文を省いてます
            int[] indices = _fingerNumberToBendMuscles[fingerNumber];
            pose.muscles[indices[0]] = sourcePose.muscles[indices[0]] + rate;
            pose.muscles[indices[1]] = sourcePose.muscles[indices[1]] + rate;
            pose.muscles[indices[2]] = sourcePose.muscles[indices[2]] + rate;
        }

        //NOTE: FingerConstsが0 ~ 9に割り当たっている前提でジャグ配列にしてます。
        //呼び出し頻度が多いのでDictionary使うのはもったいない
        private static readonly int[][] _fingerNumberToBendMuscles = new int[][]
        {
            //LeftThumb
            new[] { 55, 57, 58, },
            new[] { 59, 61, 62, },
            new[] { 63, 65, 66, },
            new[] { 67, 69, 70, },
            new[] { 71, 73, 74, },

            //RightThumb
            new[] { 75, 77, 78, },
            new[] { 79, 81, 82, },
            new[] { 83, 85, 86, },
            new[] { 87, 89, 90, },
            new[] { 91, 93, 94, },
        };
    }
}
