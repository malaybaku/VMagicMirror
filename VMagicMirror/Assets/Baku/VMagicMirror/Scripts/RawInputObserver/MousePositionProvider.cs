using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// このクラスは何とマウスの現在位置を教えてくれるすごいクラスです
    /// </summary>
    /// <remarks>
    /// マウスの絶対位置に対してRawInputの差分情報を載せることで、FPSゲーで遊んでいるときの動作を補償するのが狙いです
    /// </remarks>
    [RequireComponent(typeof(RawMouseMoveChecker))]
    public class MousePositionProvider : MonoBehaviour
    {
        [Tooltip("差分値を徐々にゼロ方向に近づけていく係数")]
        [SerializeField] private float diffValueDiminishRate = 6f;

        /// <summary>
        /// _x, _yを対スクリーン比率で[-0.5, 0.5]の区間に収まる値になおしたもの
        /// </summary>
        public Vector2 NormalizedCursorPosition { get; private set; }
        
        private RawMouseMoveChecker _rawMouseMoveChecker = null;
        private Vector2Int _prevCursosPos;
        
        //絶対位置に対して「いやユーザーはこのくらいマウス動かしてるが？」という積分値ベースの差分。徐々に減衰させて用いる。
        private float _dx = 0;
        private float _dy = 0;

        //_dx, _dyを加味して求めたマウス位置
        private int _x = 0;
        private int _y = 0;

        private void Start()
        {
            _rawMouseMoveChecker = GetComponent<RawMouseMoveChecker>();
            _prevCursosPos = NativeMethods.GetWindowsMousePosition();
        }

        private void Update()
        {
            (int dx, int dy) = _rawMouseMoveChecker.GetAndReset();
            var p = NativeMethods.GetWindowsMousePosition();

            //NOTE: マウスがプログラム的に動かされちゃってる場合のみ、_dxや_dyに非ゼロの値が加算される
            //マウスの移動が制限されていない場合、absDifとdx, dyは同じ値になる
            var absDif = p - _prevCursosPos;
            _dx += dx - absDif.x;
            _dy += dy - absDif.y;
            
            float rate = 1.0f - diffValueDiminishRate * Time.deltaTime;
            _dx *= rate;
            _dy *= rate;
            
            int x = (int)(p.x + _dx);
            int y = (int)(p.y + _dy);

            //カーソルが動いてないときは放置
            if (_x == x && _y == y)
            {
                return;
            }

            _x = x;
            _y = y;
            
            int left = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_XVIRTUALSCREEN);
            int top = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_YVIRTUALSCREEN);
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_CYVIRTUALSCREEN);
            
            //NOTE: 右方向を+X, 上方向を+Y, 値域を(-0.5, 0.5)にするための変形をやって完成
            NormalizedCursorPosition = new Vector2(
                (_x - left) * 1.0f / width - 0.5f,
                0.5f - (_y - top) * 1.0f / height
            );           
        }
    }
}
