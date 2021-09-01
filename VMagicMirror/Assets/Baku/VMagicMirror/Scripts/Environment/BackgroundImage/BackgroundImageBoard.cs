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
        [Tooltip("メインのカメラ")]
        [SerializeField] private Camera cam = default;

        private Renderer _renderer;

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
            if (!_hasTexture)
            {
                return;
            }
            
            _renderer.enabled = false;
            _renderer.material.mainTexture = null;

            Destroy(_texture);
            _texture = null;
            _textureAspect = 1f;
            _hasTexture = false;
        }

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void Update() => FitImage();

        private void FitImage()
        {
            if (!_hasTexture)
            {
                return;
            }
 
            //FOVに即してスケールを整える
            float z = transform.localPosition.z;
            float yScale = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * z * 2f;
            float xScale = cam.aspect * yScale;
            
            if (cam.aspect > _textureAspect)
            {
                //画面が横長: 横方向に埋めて、タテは画像のアスペクトに合わせてはみ出させる
                transform.localScale = new Vector3(xScale, xScale / _textureAspect, 1f);
            }
            else
            {
                //画面が縦長: 縦方向に埋めて、ヨコは画像のアスペクトに合わせてはみ出させる
                transform.localScale = new Vector3(_textureAspect * yScale, yScale, 1f);
            }
        }
    }
}
