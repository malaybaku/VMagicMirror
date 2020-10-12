using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部トラッキングが効いてないとき、頭の回転から目の回転量のsuggestをできるようにするやつ
    /// コレ単体では計算するだけで済ます(EyeJitterが出力値を実際に目ボーンにあてる)
    /// </summary>
    public class FaceAngleToEyeRot : MonoBehaviour
    {
        [SerializeField] private float pitchRotFactor = 0.02f;
        [SerializeField] private float yawRotFactor = 0.04f;

        [Inject]
        public void Initialize(FaceAttitudeController faceAttitude)
        {
            faceAttitude.ImageBaseFaceRotationUpdated += eulerAngle =>
            {
                SuggestedRotation = Quaternion.Euler(
                    eulerAngle.x * pitchRotFactor,
                    eulerAngle.y * yawRotFactor,
                    0
                    );
            };
        }

        public Quaternion SuggestedRotation { get; private set; } = Quaternion.identity;

    }
}
