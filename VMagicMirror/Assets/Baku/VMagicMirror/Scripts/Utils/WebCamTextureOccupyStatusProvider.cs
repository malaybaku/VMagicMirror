using System.Threading;
using Cysharp.Threading.Tasks;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// WebCamTextureの占有状態を管理するクラス。
    /// WebCamTexture自体は管理しない & FaceTrackerクラスを廃止してこのクラスが不要になる可能性もけっこう高いので、
    /// 凝ったことはせず、class自体もstaticにしている
    /// </summary>
    public static class WebCamTextureOccupyStatusProvider
    {
        public enum WebCamTextureUser
        {
            None,
            Dlib,
            MediaPipe,
        }
        
        public static WebCamTextureUser CurrentUser { get; private set; }

        public static bool TryOccupyDlib()
        {
            if (CurrentUser is WebCamTextureUser.None or WebCamTextureUser.Dlib)
            {
                CurrentUser = WebCamTextureUser.Dlib;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ReleaseDlib()
        {
            if (CurrentUser == WebCamTextureUser.Dlib)
            {
                CurrentUser = WebCamTextureUser.None;
            }
        }
        
        public static bool TryOccupyMediaPipe()
        {
            if (CurrentUser is WebCamTextureUser.None or WebCamTextureUser.MediaPipe)
            {
                CurrentUser = WebCamTextureUser.MediaPipe;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ReleaseMediaPipe()
        {
            if (CurrentUser == WebCamTextureUser.MediaPipe)
            {
                CurrentUser = WebCamTextureUser.None;
            }
        }
        
        // NOTE: dlib側は初期化がCoroutineで記述されてるので無し(別に実装してもよい)
        public static async UniTask OccupyMediaPipeAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (TryOccupyMediaPipe())
                {
                    // 占有できたらおしまい
                    return;
                }

                await UniTask.NextFrame(cancellationToken: ct);
            }
        }
    }
}
