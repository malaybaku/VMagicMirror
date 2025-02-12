using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    // NOTE: 名前は変えるかも
    // 純粋な数学演算というよりは「計算っぽい処理」を押し込んでおきたいやつ
    public class MathApi
    {
        private readonly Camera _mainCamera;
        
        public MathApi(Camera mainCamera)
        {
            _mainCamera = mainCamera;
        }

        /// <summary>
        /// ワールド上の位置をスクリーン座標(左下=(0,0), 右上=(1,1))に変換する
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 GetScreenPositionOfWorldPoint(Vector3 position)
        {
            var pos = _mainCamera.WorldToScreenPoint(position);
            return new Vector2(
                pos.x / (_mainCamera.pixelWidth - 1),
                pos.y / (_mainCamera.pixelHeight - 1)
            );
        }
    }
}
