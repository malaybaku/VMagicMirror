using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Canvas))]
    public class BackgroundImageLoaderLogic : MonoBehaviour
    {
        [SerializeField] private Image image = null;
        
        private void Start()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }

            //NOTE: カメラのFar Clipが100とか、そこそこ大きい値であることを前提にした書き方です。
            canvas.planeDistance = canvas.worldCamera.farClipPlane - 1.0f;
            
            var filePath = GetBackgroundImageFilePath();
            Texture2D texture = null;
            for (int i = 0; i < filePath.Length; i++)
            {
                texture = LoadTexture(filePath[i]);
                if (texture != null)
                {
                    break;
                }
            }
            
            if (texture == null)
            {
                Destroy(gameObject);
                return;
            }
            
            image.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height), 
                new Vector2(0, 0)
            );
            canvas.enabled = true;

            //HACK: 背景を読み込むと影機能はもはや使えない(使うと背景が映らなくなってしまう)ので、
            //UIからなんと言われても影を切るモードに落とす
            var shadowMotion = FindObjectOfType<ShadowBoardMotion>();
            if (shadowMotion != null)
            {
                shadowMotion.ForceKillShadowRenderer = true;
            }
        }
        

        private static Texture2D LoadTexture(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            byte[] bin = File.ReadAllBytes(filePath);
            var result = new Texture2D(16, 16); // Create new "empty" texture
            if (!result.LoadImage(bin))
            {
                Destroy(result);
                return null;
            }
            
            return result;
        }

        //NOTE: 2つ返すけど拡張子違いです
        private static string[] GetBackgroundImageFilePath()
        {
            string pngPath = Application.isEditor
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Background.png"
                ) 
                : Path.Combine(
                    Path.GetDirectoryName(Application.dataPath),
                    "Background.png"
                );
            string jpgPath = Path.ChangeExtension(pngPath, "jpg");
            return new []
            {
                pngPath, 
                jpgPath,
            };
        }
    }
}
