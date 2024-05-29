using mattatz.TransformControl;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 車のハンドルに相当するオブジェクトの位置とか回転とかサイズを表現するクラス.
    /// 横着で「ハンドルの回転に対して体が傾く比率」とかもこのクラスが持っているが、別に分けてもよい
    /// </summary>
    public class CarHandleProvider : MonoBehaviour
    {
        //NOTE: Transformごとの扱い
        // - RootTransform => ユーザーがフリーレイアウトで編集することによって移動し、それ以外では移動しない
        //   - OffsetTransform => 揺れとか前後傾の動きによって多少オフセットすることがある
        //     - Steering => ハンドルの回転量に応じて、Z軸のみに回転する。ハンドルのMeshRendererがつくのもココ
        [SerializeField] private Transform offset;
        [SerializeField] private Transform steering;
        [SerializeField] private AnimationCurve angleToHeadYawRateCurve;

        [SerializeField] private TransformControl transformControl = default;
        public TransformControl TransformControl => transformControl;
        
        public Transform RootTransform => transform;
        public Transform OffsetAddedTransform => offset;
        //NOTE: 実際はこの値が更にフリーレイアウトモードでスケールする…はず
        public float CarHandleRadius => 0.18f * transform.localScale.x;
        
        /// <summary>
        /// ステアリングの回転量をdegree単位で指定することで、表示状態を更新する
        /// </summary>
        /// <param name="angle"></param>
        public void SetSteeringRotation(float angle)
        {
            steering.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public float GetHeadYawRateFromAngleRate(float rateAbs) 
            => angleToHeadYawRateCurve.Evaluate(rateAbs);

        public void SetLayoutParameter(DeviceLayoutAutoAdjustParameters parameters)
        {
            var height = 1.2f * parameters.HeightFactor;
            var forward = 0.35f * parameters.ArmLengthFactor;

            var t = transform;
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            t.localPosition = new Vector3(0f, height, forward);
        }
    }
}
