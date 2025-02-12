using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //TODO: Unityのウィンドウが非アクティブなときにRawDiffが正しく取れなくなってそう?
    //これによって「FPS対策モード」のオプションが死んでそうなため要チェック。

    /// <summary>
    /// このクラスは何とマウスの現在位置を教えてくれるすごいクラスです
    /// </summary>
    /// <remarks>
    /// マウスの絶対位置に対してRawInputの差分情報を載せることで、FPSゲーで遊んでいるときの動作を補償するのが狙いです
    /// </remarks>
    [RequireComponent(typeof(RawInputChecker))]
    public class MousePositionProvider : MonoBehaviour
    {
        //1秒あたりにポインタが移動できる量の上限: 画面の対角線のだいたい5倍くらい
        private const float MaxMouseSpeedPerSec = 7f;
        
        [Tooltip("差分値を徐々にゼロ方向に近づけていく係数")]
        [SerializeField] private float diffValueDiminishRate = 2f;

        [Tooltip("端末のスクリーンサイズをこの秒数ごとにチェックする")]
        [SerializeField] private float monitorLayoutRefreshInterval = 10f;

        /// <summary>
        /// _x, _yを対スクリーン比率で[-0.5, 0.5]の区間に収まる値になおしたもの
        /// </summary>
        public Vector2 NormalizedCursorPosition { get; private set; }

        /// <summary>
        /// _x, _yを対スクリーン比率にしているもの。[-0.5, 0.5]の区間に収まっていると画面内をさす。画面外のこともある
        /// </summary>
        public Vector2 RawNormalizedPositionNotClamped { get; private set; } 

        public Vector2Int RawDiff { get; private set; }

        public Vector2Int RawPosition { get; private set; }
        
        private Vector2 _rawNormalizedPosition;

        private RawInputChecker _rawMouseMoveChecker = null;
        private Vector2Int _prevCursorPos;

        private float _monitorLeft;
        private float _monitorTop;
        //幅、高さはあらかじめ割っておくと除算より乗算で使う頻度が増えてハッピー
        private float _monitorWidthInv = 1;
        private float _monitorHeightInv = 1;
        private float _monitorLayoutRefreshCount = -1f;
        //NOTE: NativeMethod側のリストからコピーした値を入れる
        private readonly List<NativeMethods.RECT> _monitorRects = new List<NativeMethods.RECT>(8);
        
        //絶対位置に対して「いやユーザーはこのくらいマウス動かしてるが？」という積分値ベースの差分。徐々に減衰させて用いる。
        private float _dx = 0;
        private float _dy = 0;

        //_dx, _dyを加味して求めたマウス位置
        private int _x = 0;
        private int _y = 0;


        private void Start()
        {
            _rawMouseMoveChecker = GetComponent<RawInputChecker>();
            _prevCursorPos = NativeMethods.GetWindowsMousePosition();
            RefreshMonitorRects();
        }

        private void Update()
        {
            UpdateRawPosition();
            var distanceMax = MaxMouseSpeedPerSec * Time.deltaTime;
            if (Vector2.Distance(_rawNormalizedPosition, NormalizedCursorPosition) < distanceMax)
            {
                NormalizedCursorPosition = _rawNormalizedPosition;
            }
            else
            {
                NormalizedCursorPosition +=
                    (_rawNormalizedPosition - NormalizedCursorPosition).normalized * distanceMax;
            }
        }

        private void UpdateRawPosition()
        {
            (int dx, int dy) = _rawMouseMoveChecker.GetAndReset();
            RawDiff = new Vector2Int(dx, dy);
            var p = NativeMethods.GetWindowsMousePosition();
            RawPosition = p;
            
            //NOTE: (dx, dy)とabsDifが異なるケース == マウスがプログラム的に動かされちゃってるケース
            //FPSとかで遊んでない限りは2つの値は一致する
            var absDif = p - _prevCursorPos;
            _prevCursorPos = p;

            if (_rawMouseMoveChecker.EnableFpsAssumedRightHand)
            {
                //FPS対策モードの場合、マウスの情報を信用してやるぞ、的な補正をする
                _dx += dx - absDif.x;
                _dy += dy - absDif.y;
            }
            else
            {
                //FPS対策モードでない場合、マウスの移動量とマウスの位置はズレてないよ、という扱いにする
                _dx = 0;
                _dy = 0;
            }

#if UNITY_EDITOR
            //エディタではdx, dyが取れない(常時0になってしまう)ので_dx, _dyは捨てて、絶対座標だけに頼る
            _dx = 0;
            _dy = 0;
#endif
            
            float rate = 1.0f - diffValueDiminishRate * Time.deltaTime;
            _dx *= rate;
            _dy *= rate;
            
            int x = (int)(p.x + _dx);
            int y = (int)(p.y + _dy);

            RefreshMonitorRects();

            //カーソルが動いてないときは放置
            if (_x == x && _y == y)
            {
                return;
            }

            _x = x;
            _y = y;
            //NOTE: モニターの判別には生のカーソル位置を使う。
            //dx/dyの影響でモニター外の座標を見に行っちゃうのを防ぐのが狙い
            FindCursorIncludedMonitor(p.x, p.y);

            //NOTE: 右方向を+X, 上方向を+Y, 値域の基準を [-0.5, 0.5] にする。
            RawNormalizedPositionNotClamped = new Vector2(
                (_x - _monitorLeft) * _monitorWidthInv - 0.5f,
                0.5f - (_y - _monitorTop) * _monitorHeightInv
            );
            _rawNormalizedPosition = new Vector2(
                Mathf.Clamp(RawNormalizedPositionNotClamped.x, -0.5f, 0.5f),
                Mathf.Clamp(RawNormalizedPositionNotClamped.y, -0.5f, 0.5f)
                );
        }

        private void RefreshMonitorRects()
        {
            _monitorLayoutRefreshCount -= Time.deltaTime;
            if (_monitorLayoutRefreshCount > 0)
            {
                return;
            }
            
            _monitorLayoutRefreshCount = monitorLayoutRefreshInterval;
            var monitorRects = NativeMethods.LoadAllMonitorRects();
            foreach (var rect in monitorRects)
            {
                _monitorRects.Add(rect);
            }
        }

        private void FindCursorIncludedMonitor(int x, int y)
        {
            foreach (var rect in _monitorRects)
            {
                if (x >= rect.left && x < rect.right &&
                    y >= rect.top && y < rect.bottom)
                {
                    _monitorLeft = rect.left;
                    _monitorTop = rect.top;
                    _monitorWidthInv = 1.0f / (rect.right - rect.left);
                    _monitorHeightInv = 1.0f / (rect.bottom - rect.top);
                    return;
                }
            }
        }
    }
}
