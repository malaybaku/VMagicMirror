using Baku.VMagicMirror.Installer;
using mattatz.TransformControl;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ペンタブの座標計算をするやつ
    /// 基本はタッチパッドと一緒だが、こっちの方が設計がちょっとミニマルというか、
    /// IKとの切り分けがいい感じになっている。
    /// その代わり「ペンタブを実際に使ってるときだけペンを表示したい」というヘンテコな機能がついてます
    /// </summary>
    public class PenTabletProvider : MonoBehaviour
    {
        private MousePositionProvider _mousePositionProvider = null;

        [SerializeField] private TransformControl transformControl = null;
        public TransformControl TransformControl => transformControl;
        
        [SerializeField] private Vector3 basePosition = new Vector3(.15f, .98f, 0.12f);
        [SerializeField] private Vector3 baseRotation = new Vector3(60f, 0f, 0f);
        [SerializeField] private Vector3 baseScale = new Vector3(.3f, .2f, 1f);
        
        private PenController _penController = null;
        
        [Inject]
        public void Initialize(MousePositionProvider mousePositionProvider, PenController penController, IDevicesRoot parent)
        {
            _mousePositionProvider = mousePositionProvider;
            _penController = penController;
            transform.parent = parent.Transform;
        }

        private void Start()
        {
            foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = HIDMaterialUtil.Instance.GetPenTabletMaterial();
            }
            _penController.GetComponentInChildren<MeshRenderer>().material = HIDMaterialUtil.Instance.GetPenMaterial();
        }

        /// <summary>
        /// Windows上のマウスの位置がこのタブレットで言うとどこにあたるかを取得します。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetPosFromScreenPoint()
        {
            var cursorPosInVirtualScreen = _mousePositionProvider.NormalizedCursorPosition; 
            //NOTE: 外側エリアは大きく持って避ける。タブレットってそういうマージンありがちだし、破綻も減るので
            return transform.TransformPoint(cursorPosInVirtualScreen * 0.7f);
        }

        /// <summary> タブレットの表面に沿って右に行く方向のベクトルを取得します。 </summary>
        public Vector3 Right => transform.right;
        /// <summary> タブレットの表面に沿って上に行く方向のベクトルを取得します。 </summary>
        public Vector3 Up => transform.up;
        /// <summary> タブレットの表面と垂直に、タブレットから手を離す方向のベクトルを取得します。 </summary>
        public Vector3 Normal => -transform.forward;

        /// <summary>
        /// タブレットの現在の回転を、水平上向きを基準として取得します。
        /// </summary>
        /// <returns></returns>
        public Quaternion GetBaseRotation() =>
            transform.rotation * Quaternion.AngleAxis(-90, Vector3.right);   
        
        /// <summary>
        /// 単にオブジェクト自身の回転を取得します。設定によってはパーティクルの位置合わせに便利なやつです
        /// </summary>
        /// <returns></returns>
        public Quaternion GetRawRotation() => transform.rotation;

        /// <summary>
        /// 体格パラメータに即したペンタブの位置、姿勢を適用します。
        /// </summary>
        /// <param name="parameters"></param>
        public void SetLayoutParameter(DeviceLayoutAutoAdjustParameters parameters)
        {
            var t = transform;
            t.localRotation = Quaternion.Euler(baseRotation);
            t.localPosition = new Vector3(
                basePosition.x * parameters.ArmLengthFactor,
                basePosition.y * parameters.HeightFactor,
                basePosition.z * parameters.ArmLengthFactor
            );
            //スケールもいじったほうがいいかも…というのはあるが、いったん放置
            t.localScale = baseScale;
        }

        /// <summary>
        /// いま右手がペンタブの上にあるかどうかを設定します。
        /// ペンの表示判定に使います。
        /// </summary>
        /// <param name="isOn"></param>
        public void SetHandOnPenTablet(bool isOn)
        {
            _penController.SetHandIsOnPenTablet(isOn);
        }
    }
}
