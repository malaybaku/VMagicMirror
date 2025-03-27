using System;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    [Serializable]
    public class MediapipePoseSetterSettings
    {
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
        
        // NOTE: 使わないでいいはずなので
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
    }
}
