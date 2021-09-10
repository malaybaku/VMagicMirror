using UnityEngine;
using uWindowCapture;
using Zenject;

namespace Baku.VMagicMirror
{
    public class DesktopLightEstimator : MonoBehaviour
    {
        //やや画面アス比をリスペクトしつつ、ピクセル数を大幅に絞っていく
        private const int Width = 16;
        private const int Height = 9;
        //byteとして総和したRGB値を平均値にするためのファクタ
        private const float ColorMeanFactor = 1f / Width / Height / 255f;

        [SerializeField] private UwcWindowTexture windowTexture;
        [SerializeField] private float desktopIndexCheckInterval = 10f;
        [SerializeField] private float textureReadInterval = 0.1f;
        [SerializeField] private float factorLerpFactor = 12f;
            
        public Vector3 RgbFactor { get; private set; } = Vector3.one;
        private Vector3 _rawFactor = Vector3.one;

        private RenderTexture _rt;
        private Texture2D _rtReader;

        private float _count;
        
        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get => _isEnabled;
            private set
            {
                if (_isEnabled == value)
                {
                    return;
                }
                
                _isEnabled = value;
                windowTexture.enabled = value;
                
                if (value)
                {
                    CheckDesktopIndexValidity();
                }
                else
                {
                    RgbFactor = Vector3.one;
                    _rawFactor = Vector3.one;
                }
            }
        } 
        
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.UseDesktopLightAdjust, 
                c => IsEnabled = c.ToBoolean()
                );
        }
        
        private void Start()
        {
            _rt = new RenderTexture(Width, Height, 32, RenderTextureFormat.BGRA32, 0);
            _rtReader = new Texture2D(Width, Height, TextureFormat.BGRA32, false);
            
            InvokeRepeating(
                nameof(CheckDesktopIndexValidity), desktopIndexCheckInterval, desktopIndexCheckInterval
                );
        }

        private void Update()
        {
            UpdateRawFactor();
            RgbFactor = IsEnabled
                ? Vector3.Lerp(RgbFactor, _rawFactor, factorLerpFactor * Time.deltaTime)
                : Vector3.one;
        }

        private void UpdateRawFactor()
        {
            if (!IsEnabled)
            {
                _count = textureReadInterval;
                return;
            }

            if (windowTexture.window == null || !windowTexture.window.texture)
            {
                return;
            }

            _count += Time.deltaTime;
            if (_count < textureReadInterval)
            {
                return;
            }
            _count -= textureReadInterval;

            var source = windowTexture.window.texture;
            GetColorWithRenderTexture(source);
        }

        private void CheckDesktopIndexValidity()
        {
            if (!IsEnabled)
            {
                return;
            }

            int count = UwcManager.desktopCount;
            if (count == 0)
            {
                //そこそこ起きるケース: モニターでない環境の場合、わざわざ走査しない
                windowTexture.desktopIndex = 0;
                return;
            }

            var targetPos = GetTargetMonitorPos();
            
            for (int i = 0; i < count; i++)
            {
                var desktop = UwcManager.FindDesktop(i);
                if (desktop.rawX == targetPos.x && desktop.rawY == targetPos.y)
                {
                    windowTexture.desktopIndex = i;
                    LogOutput.Instance.Write($"desktop {i}, {desktop.width}x{desktop.height}");
                    return;
                }
            }

            //何か検出に失敗した場合
            LogOutput.Instance.Write("failed to detect correct monitor about light...");
            windowTexture.desktopIndex = 0;
        }
        
        //RenderTextureに書き写したのち、リサイズしたピクセルの色平均を取る
        private void GetColorWithRenderTexture(Texture2D source)
        {
            Graphics.Blit(source, _rt);

            var activeRt = RenderTexture.active;
            try
            {
                RenderTexture.active = _rt;
                _rtReader.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0, false);
                _rtReader.Apply();
            }
            finally
            {
                RenderTexture.active = activeRt;
            }

            var colors = _rtReader.GetRawTextureData<byte>();
            int r = 0;
            int g = 0;
            int b = 0;
            for (int i = 0; i < colors.Length; i += 4)
            {
                b += colors[i];
                g += colors[i + 1];
                r += colors[i + 2];
            }

            RgbFactor = new Vector3(r * ColorMeanFactor, g * ColorMeanFactor, b * ColorMeanFactor);
            resultColor = new Color(RgbFactor.x, RgbFactor.y, RgbFactor.z);
        }

        //uWindowCaptureで取得したいデスクトップの座標を、WinAPIから取得できるX,Y座標として取得する
        private Vector2Int GetTargetMonitorPos()
        {
            if (!NativeMethods.GetWindowRect(NativeMethods.GetUnityWindowHandle(), out var selfRect))
            {
                LogOutput.Instance.Write("Failed to get self window rect, could update desktop index");
                return Vector2Int.zero;
            }

            var monitorRects = NativeMethods.LoadAllMonitorRects();

            var selfCenter = new Vector2Int(
                (selfRect.left + selfRect.right) / 2, (selfRect.top + selfRect.bottom) / 2
            );
            
            //Unity画面の中央が入っているモニターがあれば、それで確定
            foreach (var monitor in monitorRects)
            {
                if (monitor.left <= selfCenter.x && selfCenter.x < monitor.right &&
                    monitor.top <= selfCenter.y && selfCenter.y < monitor.bottom)
                {
                    return new Vector2Int(monitor.left, monitor.top);
                }
            }
            
            //Unityウィンドウが縦長になって画面の下に潜った場合などで判定がうまく行かない場合、X座標のみで再判定
            foreach (var monitor in monitorRects)
            {
                if (monitor.left <= selfCenter.x && selfCenter.x < monitor.right)
                {
                    return new Vector2Int(monitor.left, monitor.top);
                }
            }

            //(かなり珍しいが)Unityウィンドウを全画面の右下とかに思い切り押し込んだ場合でも一応対応したいので、
            //ウィンドウの左上座標を便宜的に中心とみなして再判定
            selfCenter = new Vector2Int(selfRect.left, selfRect.top);
            foreach (var monitor in monitorRects)
            {
                if (monitor.left <= selfCenter.x && selfCenter.x < monitor.right &&
                    monitor.top <= selfCenter.y && selfCenter.y < monitor.bottom)
                {
                    return new Vector2Int(monitor.left, monitor.top);
                }
            }
            
            //全ての検出に失敗: 0,0を返すことで「プライマリモニタでお願いします」というニュアンスにする
            return Vector2Int.zero;
        }
    }
}
