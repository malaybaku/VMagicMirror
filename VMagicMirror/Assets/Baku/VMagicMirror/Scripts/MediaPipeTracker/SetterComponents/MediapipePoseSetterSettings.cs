using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    [CreateAssetMenu(fileName = "PoseSetterSettings", menuName = "MediaPipe/PoseSetterSettings")]
    public class MediapipePoseSetterSettings : ScriptableObject
    {
        // (通常は起きないはずだが) この秒数だけMediaPipeの結果が受信できなかった場合、
        // MediaPipeのタスク結果とは無関係にトラッキングロストと見なす
        [SerializeField] private float trackingLostTimeThreshold = 0.3f;

        // xy軸の移動の大きさの係数
        [SerializeField] private float bodyHorizontalMoveScale = 1.0f;
        // z軸の移動の大きさの係数。この値は Face Landmarker BS を使っているときだけ使われる。0にすると奥行きの移動をナシにする
        [SerializeField] private float faceDepthMoveScale = 1.0f;

        [SerializeField] private float handHorizontalMoveScale = 1.0f;
        // 手が体から前に出ている度合いを、係数として指定する値。大きくするとアバターの手が正面にピンと伸びていく
        [SerializeField] private float handDepthMoveScale = 1.0f;
        
        // 手首の回転をパントマイムにならないように追加で補正する計算の強度
        [Range(0f, 1f)]
        [SerializeField] private float handRotationModifyWeight = 0.5f;
        
        [SerializeField] private float handIkSmoothRate = 18f;
        [SerializeField] private float fingerBoneSmoothRate = 18f;

        // NOTE: アバターが極端に大きい場合は上限を緩和してもいいが、あんまり細かくはケアしたくない…
        // 手のIKの1secあたりの最大移動距離で、とくにトラッキング中の動作に対して適用される。
        [SerializeField] private float handMoveSpeedMax = 1.4f;

        public float HandIkSmoothRate => handIkSmoothRate;
        public float FingerBoneSmoothRate => fingerBoneSmoothRate;
        public float HandMoveSpeedMax => handMoveSpeedMax;
        
        // NOTE: 使わないでいいはずなので無し
        // public float RawHorizontalMoveScale => horizontalMoveScale;

        // NOTE: FaceLandmarker の出力がおおむねcm単位であることが分かっているので、こういう変換を行っている
        public float Face6DofHorizontalScale => 0.01f * bodyHorizontalMoveScale;

        // NOTE: horizontalMoveScaleをさらに乗算する方式も考えられる。
        // が、独立なほうが柔軟ではある (= 「前後には動けるが横は動かない」を実現するには独立にしておかないとムズい)
        public float Face6DofDepthScale => 0.01f * faceDepthMoveScale;
        
        // NOTE: こっちは高度に決め打ちで、「顔をカメラ画面の左～右に移動したときの移動量が0.6mくらい」というのをエイヤで基準にする
        public float Body2DoFScale => 0.3f * bodyHorizontalMoveScale;

        // NOTE: HandLandmarkでは(今のところ) 6DoFをいい感じに抽出できないことに注意
        public float Hand2DofNormalizedHorizontalScale => handHorizontalMoveScale;
        public float Hand2DofWorldHorizontalScale => 0.3f * handHorizontalMoveScale;
        public float Hand2DofDepthScale => handDepthMoveScale;
        public float HandRotationModifyWeight => handRotationModifyWeight;
        public float TrackingLostTimeThreshold => trackingLostTimeThreshold;
    }
}
