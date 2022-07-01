using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Renderer))]
    public class ShadowBoardMotion : MonoBehaviour
    {
        [SerializeField] private Transform cam = null;
        [SerializeField] private Renderer[] shadowStabilizerRenderers = null;

        public float ShadowBoardWaistDepthOffset { get; set; } = 0.4f;
        
        private bool _enableShadowRenderer = true;
        public bool EnableShadowRenderer
        {
            get => _enableShadowRenderer;
            set
            {
                if (_enableShadowRenderer == value)
                {
                    return;
                }
                _enableShadowRenderer = value;
                RefreshRendererEnable();
            }
        }

        private bool _forceKillShadowRenderer;
        /// <summary>
        /// どうしてもシャドウを無効化してほしいときに使う。背景画像をロードした場合はオンになっててほしい。
        /// </summary>
        public bool ForceKillShadowRenderer
        {
            get => _forceKillShadowRenderer;
            set
            {
                if (_forceKillShadowRenderer == value)
                {
                    return;
                }
                _forceKillShadowRenderer = value;
                RefreshRendererEnable();
            }
        }

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            RefreshRendererEnable();
        }

        private void Update()
        {
            //コード通りだが、以下のうち奥側に影の影ポリが来るようにしたい
            // - 腰よりちょっと奥 : 正面～浅く見下ろした角度ではコレを使いたい
            // - 足元 : 深く見下ろした角度ではコレを使いたい
            float depthByWaist = 
                cam.transform.InverseTransformPoint(new Vector3(0, 1, 0)).z +
                ShadowBoardWaistDepthOffset;

            float depthByFoot = cam.transform.InverseTransformPoint(Vector3.zero).z;

            transform.localPosition = Mathf.Max(depthByWaist, depthByFoot) * Vector3.forward;
        }

        private void RefreshRendererEnable()
        {
            var rendererEnabled = EnableShadowRenderer && !ForceKillShadowRenderer;
            _renderer.enabled = rendererEnabled;
            foreach (var r in shadowStabilizerRenderers)
            {
                r.enabled = rendererEnabled;
            }
        }
    }
}
