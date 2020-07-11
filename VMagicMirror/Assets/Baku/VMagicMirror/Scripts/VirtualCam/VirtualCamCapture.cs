using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// UnityCaptureをVMagicMirrorの挙動に合わせてちょっと改造したすごいやつだよ
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class VirtualCamCapture : MonoBehaviour
    {
        private const int CaptureDeviceIndex = 0;
        private const int TimeoutMillisec = 1000;
        private const EMirrorMode MirrorMode = EMirrorMode.Disabled;
        private const EResizeMode ResizeMode = EResizeMode.Disabled;
        private const bool UseDoubleBuffering = true;

        private const int RenderTextureDepth = 24;

        //VMagicMirrorはひとまずGammaのままでいいので
        private const bool IsLinearColorSpace = false;

        [SerializeField] private bool enableCaptureWrite = false;
        [SerializeField] private int width = 640;
        [SerializeField] private int height = 480;

        [SerializeField] private bool showWarnings = false;

        public bool EnableCaptureWrite
        {
            get => enableCaptureWrite;
            set => enableCaptureWrite = value;
        }

        public int Width
        {
            get => width;
            set => width = value;
        }

        public int Height
        {
            get => height;
            set => height = value;
        }

        private enum EResizeMode
        {
            Disabled = 0,
            LinearResize = 1,
        }

        private enum EMirrorMode
        {
            Disabled = 0,
            MirrorHorizontally = 1
        }

        private enum ECaptureSendResult
        {
            SUCCESS = 0,
            WARNING_FRAMESKIP = 1,
            WARNING_CAPTUREINACTIVE = 2,
            ERROR_UNSUPPORTEDGRAPHICSDEVICE = 100,
            ERROR_PARAMETER = 101,
            ERROR_TOOLARGERESOLUTION = 102,
            ERROR_TEXTUREFORMAT = 103,
            ERROR_READTEXTURE = 104,
            ERROR_INVALIDCAPTUREINSTANCEPTR = 200,
        }

        private NativeCaptureInterface _captureInterface;

        private RenderTexture _resizedTexture = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            var _ = new VirtualCamReceiver(receiver, this);
        }
        
        private void Start()
        {
            //Alphaは仮想カメラの場合は不要かな～と思うので意識的に切ります。
            _resizedTexture = new RenderTexture(Width, Height, RenderTextureDepth);
            _captureInterface = new NativeCaptureInterface(CaptureDeviceIndex);
            DisableWindowGhosting();
        }

        private void OnDestroy()
        {
            _captureInterface.Close();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);

            //送信不要扱いなので何もしない
            if (!EnableCaptureWrite)
            {
                return;
            }

            try
            {
                Texture srcTexture = null;
                if (source.width == Width && source.height == Height)
                {
                    srcTexture = source;
                }
                else
                {
                    if (_resizedTexture.width != Width || _resizedTexture.height != Height)
                    {
                        _resizedTexture.Release();
                        _resizedTexture = new RenderTexture(Width, Height, RenderTextureDepth);
                    }

                    //NOTE: これ通るっけ？？まあわかんないし見るしかないか。
                    Graphics.Blit(source, _resizedTexture);
                    srcTexture = _resizedTexture;
                }

                switch (_captureInterface.SendTexture(srcTexture, TimeoutMillisec, UseDoubleBuffering, ResizeMode,
                    MirrorMode))
                {
                    case ECaptureSendResult.SUCCESS:
                        break;
                    case ECaptureSendResult.WARNING_FRAMESKIP:
                        if (showWarnings)
                        {
                            WriteLog(
                                "[UnityCapture] Capture device did skip a frame read, capture frame rate will not match render frame rate.");
                        }

                        break;
                    case ECaptureSendResult.WARNING_CAPTUREINACTIVE:
                        if (showWarnings)
                        {
                            WriteLog("[UnityCapture] Capture device is inactive");
                        }

                        break;
                    case ECaptureSendResult.ERROR_UNSUPPORTEDGRAPHICSDEVICE:
                        WriteLog("[UnityCapture] Unsupported graphics device (only D3D11 supported)");
                        break;
                    case ECaptureSendResult.ERROR_PARAMETER:
                        WriteLog("[UnityCapture] Input parameter error");
                        break;
                    case ECaptureSendResult.ERROR_TOOLARGERESOLUTION:
                        WriteLog("[UnityCapture] Render resolution is too large to send to capture device");
                        break;
                    case ECaptureSendResult.ERROR_TEXTUREFORMAT:
                        WriteLog(
                            "[UnityCapture] Render texture format is unsupported (only basic non-HDR (ARGB32) and HDR (FP16/ARGB Half) formats are supported)");
                        break;
                    case ECaptureSendResult.ERROR_READTEXTURE:
                        WriteLog("[UnityCapture] Error while reading texture image data");
                        break;
                    case ECaptureSendResult.ERROR_INVALIDCAPTUREINSTANCEPTR:
                        WriteLog("[UnityCapture] Invalid Capture Instance Pointer");
                        break;
                    default:
                        WriteLog("[UnityCapture] 不明な状態です");
                        //NOTE: ここには来ないはず
                        break;

                }

            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void WriteLog(string msg) => LogOutput.Instance.Write("VCamCapture Error: " + msg);

        private class NativeCaptureInterface
        {
            [DllImport("UnityCapturePlugin")]
            private static extern IntPtr CaptureCreateInstance(int capNum);

            [DllImport("UnityCapturePlugin")]
            private static extern void CaptureDeleteInstance(IntPtr instance);

            [DllImport("UnityCapturePlugin")]
            private static extern ECaptureSendResult CaptureSendTexture(
                IntPtr instance, IntPtr nativeTexture, int timeout, bool useDoubleBuffering,
                EResizeMode resizeMode, EMirrorMode mirrorMode, bool isLinearColorSpace
            );

            private IntPtr _handle;

            public NativeCaptureInterface(int captureDeviceIndex)
            {
                _handle = CaptureCreateInstance(captureDeviceIndex);
            }

            ~NativeCaptureInterface()
            {
                Close();
            }

            public void Close()
            {
                if (_handle != IntPtr.Zero)
                {
                    CaptureDeleteInstance(_handle);
                }
                _handle = IntPtr.Zero;
            }

            public ECaptureSendResult SendTexture(
                Texture source, int timeout, bool doubleBuffering, EResizeMode resizeMode, EMirrorMode mirrorMode
                )
            {
                if (_handle == IntPtr.Zero)
                {
                    return ECaptureSendResult.ERROR_INVALIDCAPTUREINSTANCEPTR;
                }
                else
                {
                    return CaptureSendTexture(
                        _handle, source.GetNativeTexturePtr(), timeout, 
                        doubleBuffering, resizeMode, mirrorMode, IsLinearColorSpace
                        );
                }
            }
        }

        //NOTE: 仮想カメラ有効時にアプリフォーカスが切れるとウィンドウが止まることがあるのを対策するために叩くWin32API
        [DllImport("user32.dll")]  
        private static extern void DisableProcessWindowsGhosting();
        
        private static void DisableWindowGhosting()
        {
            if (!Application.isEditor)
            {
                DisableProcessWindowsGhosting();
            }
        }
    }
}
