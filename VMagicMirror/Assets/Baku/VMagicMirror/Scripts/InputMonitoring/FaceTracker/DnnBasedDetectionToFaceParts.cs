using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// DNNで検出したデータを姿勢計算にわたすためのクラス
    /// 画像の座標違いの吸収とかをします
    /// </summary>
    public class DnnBasedDetectionToFaceParts
    {
        public DnnBasedDetectionToFaceParts(MyDnnBasedDetectionWebCamTexture source, DnnBasedFaceParts dest)
        {
            _source = source;
            _dest = dest;
        }

        private readonly MyDnnBasedDetectionWebCamTexture _source;
        private readonly DnnBasedFaceParts _dest;
        
        /// <summary>
        /// 検出結果をパーツ側に書き込んでパーツ側の状態を更新します。
        /// </summary>
        public void Update(int imageWidth, int imageHeight, bool disableHorizontalFlip)
        {
            _dest.ImageSize = new Vector2(imageWidth, imageHeight);
            var faceRect = _source.FaceRect;
            //上下だけ逆にしとくと良いはず
            _dest.FaceArea = new Rect(
                faceRect.xMin, imageHeight - faceRect.yMax, faceRect.width, faceRect.height
                );

            //NOTE: ここの順番はExampleコードと揃えてます
            var landMarks = _source.LandMarks;
            _dest.RightEye = new Vector2(landMarks[0].x, imageHeight - landMarks[0].y);
            _dest.LeftEye = new Vector2(landMarks[1].x, imageHeight - landMarks[1].y);
            _dest.NoseTop = new Vector2(landMarks[2].x, imageHeight - landMarks[2].y);
            _dest.MouthRight = new Vector2(landMarks[3].x, imageHeight - landMarks[3].y);
            _dest.MouthLeft = new Vector2(landMarks[4].x, imageHeight - landMarks[4].y);
            _dest.DisableHorizontalFlip = disableHorizontalFlip;

            // for (int i = 0; i < 5; i++)
            // {
            //     Debug.Log($"landmark {i}: {landMarks[i].x:0.00}, {landMarks[i].y:0.00}");
            // }
            
            _dest.Calculate();
        }
    }
}
