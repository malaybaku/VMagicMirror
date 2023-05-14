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
    public class CameraUtilWrapper
    {
        private readonly Camera _camera;

        public CameraUtilWrapper([Inject(Id = "RefCameraForRay")] Camera camera)
        {
            _camera = camera;
        }

        //NOTE: どうしてもカメラ自体を渡したい場合だけ使う。普通はラッパーメソッドを使うほうがいい
        public Camera Camera => _camera;

        public Ray ScreenPointToRay(Vector3 point) => _camera.ScreenPointToRay(point);
        public bool PixelRectContains(Vector3 point) => _camera.pixelRect.Contains(point);
    }
}
