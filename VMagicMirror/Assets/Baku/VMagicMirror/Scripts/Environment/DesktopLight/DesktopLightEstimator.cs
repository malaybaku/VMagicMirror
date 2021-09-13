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

        [SerializeField] private UwcWindowTexture windowTexture;
        [SerializeField] private float desktopIndexCheckInterval = 10f;
        [SerializeField] private float textureReadInterval = 0.1f;
        [SerializeField] private float factorLerpFactor = 12f;
        [SerializeField] private ComputeShader colorMeanShader;
            
        public Vector3 RgbFactor { get; private set; } = Vector3.one;
        private Vector3 _rawFactor = Vector3.one;

        private RenderTexture _rt;
        private float _colorReadCount;
        private float _desktopIndexCheckCount;

        private int _colorMeanKernelIndex;
        private ComputeBuffer _colorMeanResultBuffer;
        private readonly float[] _colorMeanResult = new float[3];
            
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
                    _desktopIndexCheckCount = desktopIndexCheckInterval;
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
            //_rtReader = new Texture2D(Width, Height, TextureFormat.BGRA32, false);
            _colorMeanKernelIndex = colorMeanShader.FindKernel("CalcMeanColor");
            _colorMeanResultBuffer = new ComputeBuffer(3, sizeof(float));
            colorMeanShader.SetTexture(_colorMeanKernelIndex, "inputTexture", _rt);
            colorMeanShader.SetBuffer(_colorMeanKernelIndex, "resultColor", _colorMeanResultBuffer);
        }

        private void Update()
        {
            UpdateRawFactor();
            UpdateDesktopIndexValidity();
            RgbFactor = IsEnabled
                ? Vector3.Lerp(RgbFactor, _rawFactor, factorLerpFactor * Time.deltaTime)
                : Vector3.one;
        }

        private void UpdateRawFactor()
        {
            if (!IsEnabled)
            {
                _colorReadCount = textureReadInterval;
                return;
            }

            if (windowTexture.window == null || !windowTexture.window.texture)
            {
                return;
            }

            _colorReadCount += Time.deltaTime;
            if (_colorReadCount < textureReadInterval)
            {
                return;
            }
            _colorReadCount -= textureReadInterval;

            var source = windowTexture.window.texture;
            //GetColorWithRenderTexture(source);
            GetColorByComputeShader(source);
        }

        private void UpdateDesktopIndexValidity()
        {
            if (!IsEnabled)
            {
                return;
            }
            
            _desktopIndexCheckCount += Time.deltaTime;
            if (_desktopIndexCheckCount < desktopIndexCheckInterval)
            {
                return;
            }

            //デスクトップの情報が出揃ってないうちは待つ(数フレーム程度)
            //ここで待たされる間は結果的にデスクトップ0が参照される
            if (!CheckUwcDesktopsArePrepared())
            {
                return;
            }
            
            _desktopIndexCheckCount = 0f;
            CheckDesktopIndexValidity();
        }
        
        private void CheckDesktopIndexValidity()
        {
            int count = UwcManager.desktopCount;
            if (count == 0 || count == 1)
            {
                //そこそこ起きるケース: シングルモニターの場合は深く考えない
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
                    return;
                }
            }

            //何か検出に失敗した場合
            LogOutput.Instance.Write("failed to detect correct monitor about light...");
            windowTexture.desktopIndex = 0;
        }

        private bool CheckUwcDesktopsArePrepared()
        {
            return UwcManager.desktopCount == NativeMethods.LoadAllMonitorRects().Count;
        }

        private void GetColorByComputeShader(Texture2D source)
        {
            //リサイズ
            Graphics.Blit(source, _rt);

            //リサイズしたテクスチャに対してGPUベースで色計算を行い、
            colorMeanShader.Dispatch(_colorMeanKernelIndex, 1, 1, 1);
            //CPUに引っ張り出す: このGetDataがちょっと重いことに留意すべし。
            _colorMeanResultBuffer.GetData(_colorMeanResult);
            
            var factor = new Vector3(_colorMeanResult[0], _colorMeanResult[1], _colorMeanResult[2]);
            _rawFactor = GetLightFactor(factor);
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

        private static Vector3 GetLightFactor(Vector3 values)
        {
            //まず全ての色の値を引き上げ
            var r = LightFactorCurve(values.x);
            var g = LightFactorCurve(values.y);
            var b = LightFactorCurve(values.z);
            var brightness = CalcBrightness(r, g, b);
            
            //brightnessが高い場合、色をぜんぶ引き上げる。コレにより、特に緑とか水色の環境で白寄りにする
            return new Vector3(
                Mathf.Clamp01(r + brightness * 0.5f),
                Mathf.Clamp01(g + brightness * 0.5f),
                Mathf.Clamp01(b + brightness * 0.5f)
            );
        }

        private static float LightFactorCurve(float value)
        {
            //真っ暗にするのはあまり価値がないこと、および
            //そこそこ明るい場合は白に倒したいこと、および
            //アバター自身の映り込みによって黒方向に倒れやすいバイアスを消したいことなどを考慮したカーブです
            //x: 0.0 - 0.5 - 1.0
            //y: 0.1 - 1.0 - 1.0
            if (value > 0.5f)
            {
                return 1f;
            }
            else
            {
                return Mathf.Lerp(0.1f, 1f, value * 2f);
            }
        }

        private static float CalcBrightness(float r, float g, float b)
            => r * 0.3f + g * 0.59f + b * 0.11f;
    }
}
