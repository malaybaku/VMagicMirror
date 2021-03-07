using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ワールド上に背景画像を置くためのQuadにアタッチするやつ。
    /// Screen Space UIだとシャドウを出しつつ背景画像もサポート、という事ができないため、
    /// わざわざワールドにnon-UIなオブジェクトとして配置するのがポイント
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class BackgroundImageBoard : MonoBehaviour
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        [Tooltip("メインのカメラ")]
        [SerializeField] private Camera cam = default;

        private Renderer _renderer;
        private Material _material;

        private bool _hasTexture;
        private Texture2D _texture;
        private float _textureAspect = 1f;

        /// <summary>
        /// 画像を適用します。
        /// </summary>
        /// <param name="image"></param>
        public void SetImage(Texture2D image)
        {
            DisposeImage();
            
            _texture = image;
            _hasTexture = true;
            _textureAspect = image.width * 1.0f / image.height;
            _renderer.material.mainTexture = image;            

            FitImage();
            _renderer.enabled = true;
        }

        /// <summary>
        /// 画像を削除し、背景画像を非表示の状態にします。
        /// </summary>
        public void DisposeImage()
        {
            if (_hasTexture)
            {
                _renderer.enabled = false;
                _renderer.material.mainTexture = null;

                Destroy(_texture);
                _texture = null;
                _textureAspect = 1f;
                _hasTexture = false;
            }
        }

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
            _material = _renderer.material;
        }

        private void Update() => FitImage();

        private void FitImage()
        {
            if (!_hasTexture)
            {
                return;
            }
            
            //要件は2つ
            // 1. Quadがカメラの視界を覆うこと
            //      -> Quadのスケールを調整
            // 2. Quadのスケールがテクスチャのアスペクトに合っていること
            //      -> マテリアルのtile/offsetで調整
            FitQuadScaleToFillView();
            FitTextureTileToKeepAspect();
        }

        private void FitQuadScaleToFillView()
        {
            //FOVに即してスケールを整える
            var t = cam.transform;
            float z = transform.localPosition.z;
            float yScale = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * z * 2f;
            float aspect = cam.aspect;
            
            //全体を覆うような正方形にする。正方形にするとテクスチャのスケール調整が楽なので。
            float scale = aspect > 1f ? yScale * aspect : yScale;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void FitTextureTileToKeepAspect()
        {
            if (_textureAspect > 1f)
            {
                //横長の画像: x方向の両端を切る
                _material.SetTextureScale(MainTex, new Vector2(1f / _textureAspect, 1f));
                _material.SetTextureOffset(
                    MainTex, new Vector2(0.5f * (_textureAspect - 1) / _textureAspect, 0f)
                    );
            }
            else
            {            
                //縦長の画像: y方向の両端を切る
                _material.SetTextureScale(MainTex, new Vector2(1f, _textureAspect));
                _material.SetTextureOffset(
                    MainTex, new Vector2(0f, 0.5f * (1 - _textureAspect))
                    );
            }

        }
    }
}
