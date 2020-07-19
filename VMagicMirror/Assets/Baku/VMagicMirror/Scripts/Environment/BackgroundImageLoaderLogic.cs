using System;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> アプリ起動時に背景画像が所定位置にあったら表示し、なければ自滅するクラス </summary>
    public class BackgroundImageLoaderLogic : MonoBehaviour
    {
        [SerializeField] private BackgroundImageCanvas canvasPrefab = null;
        
        private void Start()
        {
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
            
            var canvas = Instantiate(canvasPrefab, transform);
            canvas.SetImage(texture);
            
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
