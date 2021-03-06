using System.IO;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> アプリ起動時に背景画像が所定位置にあったら表示し、なければ自滅するクラス </summary>
    public class BackgroundImageLoaderLogic : MonoBehaviour
    {
        [SerializeField] private BackgroundImageCanvas canvasPrefab = null;

        //NOTE: 1回も使ってない場合はインスタンスを作らないでおく。邪魔だからね。
        private bool _hasCanvas = false;
        private BackgroundImageCanvas _canvas = null;

        //これinjectが正道なんだけどめんどくさいんでFindObjectします…。￥
        private ShadowBoardMotion _shadowBoardMotion = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetBackgroundImagePath,
                command =>
                {
                    if (File.Exists(command.Content))
                    {
                        LoadBackgroundImage(command.Content);     
                    }
                    else
                    {
                        ClearBackgroundImage();
                    }
                });
        }

        private void LoadBackgroundImage(string imageFilePath)
        {
            if (!_hasCanvas)
            {
                _canvas = Instantiate(canvasPrefab, transform);
                _hasCanvas = true;
            }
            _canvas.SetImage(LoadTexture(imageFilePath));
            
            //HACK: 背景を読み込むと影機能はもはや使えない(使うと背景が映らなくなってしまう)ので、
            //UIからなんと言われても影を切る
            _shadowBoardMotion.ForceKillShadowRenderer = true;
        }

        private void ClearBackgroundImage()
        {
            //そもそも1回も背景画像を入れてないよね、というケースをガードしてます
            if (!_hasCanvas || !_canvas.gameObject.activeInHierarchy)
            {
                return;
            }
            
            _canvas.gameObject.SetActive(false);
            _shadowBoardMotion.ForceKillShadowRenderer = false;
        }

        private void Start()
        {
            //TODO: ここマナー悪い
            _shadowBoardMotion = FindObjectOfType<ShadowBoardMotion>();
        }

        private static Texture2D LoadTexture(string filePath)
        {
            byte[] bin = File.ReadAllBytes(filePath);
            var result = new Texture2D(16, 16);
            if (!result.LoadImage(bin))
            {
                Destroy(result);
                return null;
            }
            return result;
        }
    }
}
