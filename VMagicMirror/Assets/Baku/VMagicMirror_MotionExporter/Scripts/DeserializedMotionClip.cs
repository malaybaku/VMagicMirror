using UnityEngine;

namespace Baku.VMagicMirror.MotionExporter
{
    /// <summary>
    /// シリアライズされた情報を再生する前提で保持する、AnimationClipもどきのようなクラス。
    /// </summary>
    /// <remarks>
    /// Playable系の仕組みに乗っからないのはそうした処理を使わない応用用途を想定しているため
    /// </remarks>
    public class DeserializedMotionClip
    {
        //muscleの全てのアニメーション。
        private readonly AnimationCurve[] _muscleCurves = new AnimationCurve[95];

        private bool _hasTarget = false;
        private HumanoidAnimationSetter _target;
        public HumanoidAnimationSetter Target
        {
            get => _target;
            set
            {
                _target = value;
                _hasTarget = (_target != null);
            } 
        }

        /// <summary> 再生時間を取得します。 </summary>
        public float Duration { get; private set; } = 0f;
        
        /// <summary>
        /// <see cref="HumanoidAnimationSetter"/>のフィールド名を指すプロパティとカーブ情報を設定します。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="curve"></param>
        public void SetCurve(string propertyName, AnimationCurve curve)
        {
            if (propertyName.StartsWith("p") && 
                int.TryParse(propertyName.Substring(1), out var index) && 
                index >= 0 && index < 95
                )
            {
                _muscleCurves[index] = curve;
                //SetCurveによって今よりDurationが伸びないかな、というのをチェック
                Duration = Mathf.Max(Duration, curve.keys[curve.keys.Length - 1].time);
            }

            //TODO: "Root.x"とかもパースして保持すればマッスル以外の値も転写できるので、必要なら足すこと
        }

        /// <summary>
        /// 指定した時間におけるアニメーション情報をHumanoidAnimationSetterに書き込みます。
        /// 時間がマイナスだったりDurationより大きかったりする場合はそれらの範囲内に収まるように制限されます。
        /// </summary>
        /// <param name="time"></param>
        public void Evaluate(float time)
        {
            if (!_hasTarget)
            {
                return;
            }

            time = Mathf.Clamp(time, 0, Duration);

            var array = _target.MuscleArray;
            
            //マッスルをひたすらチェックしていく
            for (int i = 0; i < _muscleCurves.Length; i++)
            {
                if (_muscleCurves[i] != null)
                {
                    array[i] = _muscleCurves[i].Evaluate(time);
                }
            }
        }
    }
}
