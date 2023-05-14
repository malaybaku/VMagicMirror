using mattatz.TransformControl;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// スクリーン上のカメラ状態に関知せず、通常のCameraメソッドっぽい挙動を提供するためのラッパークラス。
    /// </summary>
    /// <remarks>
    /// 真のMainCameraは「見た目はキープしたままFoVを書き換えて使う」という使われ方をするので、
    /// そのへんの実装詳細を隠すのがこのクラス
    /// </remarks>
    public class CameraUtilWrapper : PresenterBase
    {
        private readonly Camera _camera;

        public CameraUtilWrapper([Inject(Id = "RefCameraForRay")] Camera camera)
        {
            _camera = camera;
        }

        public Ray ScreenPointToRay(Vector3 point) => _camera.ScreenPointToRay(point);
        public bool PixelRectContains(Vector3 point) => _camera.pixelRect.Contains(point);

        public override void Initialize()
        {
            //NOTE: TransformControlとVMM本体の独立性ということで一応こういう渡し方にしておく
            TransformControlCameraStore.Set(_camera);    
        }
    }
}
