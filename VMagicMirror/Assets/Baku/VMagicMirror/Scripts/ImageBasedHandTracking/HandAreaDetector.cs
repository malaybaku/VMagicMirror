#if VMAGICMIRROR_USE_OPENCV

using UnityEngine;
using System.Collections.Generic;
using Baku.OpenCvExt;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using Rect = UnityEngine.Rect;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 手を検出する処理のうちOpenCVにがっつり依存した処理をまとめたやつ
    /// </summary>
    public class HandAreaDetector : IHandAreaDetector
    {
        #region 画像処理で使う設定値
        
        /// <summary> 立っている指を判定するためのしきい値で、低いほど指が立っていると判定しやすくなる </summary>
        public float DefectThreasholdValue { get; set; } = 8500f;

        /// <summary> 肌の色推定値を鳴らすときに使う0~1の値。低いほど推定値がゆっくり推移する </summary>
        public float SkinColorLowPassRate { get; set; }= 0.1f;

        /// <summary> 色検出するときのHueの許容幅 </summary>
        public int HRadius { get; set; }= 25;
        /// <summary> 色検出するときのSaturationの許容幅 </summary>
        public int SRadius { get; set; }= 50;
        /// <summary> 色検出するときのBrightnessの許容幅 </summary>
        public int VRadius { get; set; }= 50;

        /// <summary> ColorBlobが手であると判断する最小面積を指定します。 </summary>
        public float MinContourArea { get; set; } = 2000;

        /// <summary>
        /// 0, 1, 2のいずれかを指定すると、ColorBlob検出時に画像を指定した回数だけダウンスケールして高速処理します。
        /// </summary>
        public int DownScaleCount { get; set; }
        
        #endregion
        
        private Scalar _skinColorRgba;
        private readonly ColorBlobDetector _detector = new ColorBlobDetector();
        private readonly Scalar _colorBlack = new Scalar(0, 0, 0, 255);
        private readonly Point _rectLt = new Point();
        private readonly Point _rectBr = new Point();

        private Mat _mat;

        private readonly Size _gaussianBlurKernelSize = new Size(3, 3);
        //NOTE: 0, 1, 2要素はインデックスで3要素目はキョリが入ることに注意
        private readonly int[] _convexityDefectSetValues = new int[4];
        
        //Cache
        private readonly MatOfInt _hullIndices = new MatOfInt();
        
        #region IHandAreaDetector
        
        public bool HasValidHandArea { get; private set; }
        
        public Vector2 HandAreaCenter { get; private set; }

        public Vector2 HandAreaSize { get; private set; }

        public float HandAreaRotation { get; private set; }
        
        public List<Vector2> ConvexDefectVectors { get; } = new List<Vector2>();
        
        public void UpdateHandDetection(Color32[] colors, int width, int height, Rect faceRect)
        {
            if (_mat == null || _mat.width() != width || _mat.height() != height)
            {
                _mat = new Mat(height, width, CvType.CV_8UC4, _colorBlack);
            }

            OpenCvExtUtils.ColorsToMat(colors, _mat, width, height);

            EstimateSkinColor(_mat, faceRect);
            PaintBlackOnFace(_mat, faceRect);
            EstimateHandPose(_mat);
        }

        /// <summary>
        /// 画像は取得したが顔は検出出来なかった場合に呼び出すことで、手の検出が失敗した状態へと更新します。
        /// </summary>
        public void UpdateHandDetectionWithoutFace()
        {
            HasValidHandArea = false;
        }

        #endregion

        
        /// <summary>
        /// 顔の中心付近の色を平均することにより、肌の色を推定します。
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="faceRect"></param>
        private void EstimateSkinColor(Mat mat, Rect faceRect)
        {
            var center = faceRect.center;

            int cols = mat.cols();
            int rows = mat.rows();
            int x = Mathf.Clamp((int)center.x, 0, cols - 1);
            int y = Mathf.Clamp((int)center.y, 0, rows - 1);

            const int rectSize = 10;
            
            var touchedRect = new OpenCVForUnity.CoreModule.Rect();
            touchedRect.x = (x > rectSize) ? x - rectSize : 0;
            touchedRect.y = (y > rectSize) ? y - rectSize : 0;
            touchedRect.width = (x + rectSize < cols) ? x + rectSize - touchedRect.x : cols - touchedRect.x;
            touchedRect.height = (y + rectSize < rows) ? y + rectSize - touchedRect.y : rows - touchedRect.y;
            
            int rowStart = (y > rectSize) ? y - rectSize : 0;
            int rowEnd = rowStart + ((y + rectSize < rows) ? y + rectSize - touchedRect.y : rows - touchedRect.y);
            int colStart =  (x > rectSize) ? x - rectSize : 0;
            int colEnd = colStart + ((x + rectSize < cols) ? x + rectSize - touchedRect.x : cols - touchedRect.x);
            
            using (var touchedRegionRgba = mat.submat(rowStart, rowEnd, colStart, colEnd))
            {
                var latestBlobColorRgba = Core.sumElems(touchedRegionRgba);
                int pointCount = touchedRect.width * touchedRect.height;
                for (int i = 0; i < latestBlobColorRgba.val.Length; i++)
                {
                    latestBlobColorRgba.val[i] /= pointCount;
                }

                if (_skinColorRgba == null)
                {
                    _skinColorRgba = latestBlobColorRgba;
                }

                //色をならす
                int len = Mathf.Min(latestBlobColorRgba.val.Length, _skinColorRgba.val.Length);
                for (int i = 0; i < len; i++)
                {
                    _skinColorRgba.val[i] = Mathf.Lerp(
                        (float)_skinColorRgba.val[i], (float)latestBlobColorRgba.val[i], SkinColorLowPassRate
                        );
                }
                
                //BlobDetectorはHSVを使いたいのでHSVを渡す: 定数がかかるのはOpenCVとUnityの範囲が違うからです
                // - Unity: RGBもHSBも[0.0, 1.0]
                // - OpenCV: RGBは[0, 255], Hは[0, 179], SVは[0, 255]
                var rgba = _skinColorRgba.val;
                var c = new Color((float)rgba[0] / 255f, (float)rgba[1] / 255f, (float)rgba[2] / 255f);
                Color.RGBToHSV(c, out float h, out float s, out float v);
                _detector.SetHsvColor(h * 179f, s * 255f, v * 255f);
            }            
        }

        /// <summary>
        /// 顔として検出された領域を真っ黒に塗りつぶします。
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="rect"></param>
        /// <remarks>
        /// ColorBlobを得るときに顔を誤って手だと思い込まないための措置です。
        /// </remarks>
        private void PaintBlackOnFace(Mat mat, Rect rect)
        {
            _rectLt.x = rect.xMin;
            _rectLt.y = 0;
            _rectBr.x = rect.xMax;
            _rectBr.y = mat.height();
            Imgproc.rectangle(mat, _rectLt, _rectBr, _colorBlack, -1);  
        }
        
        private void EstimateHandPose(Mat mat)
        {
            var contours = GetSkinColorContours(mat);
            if (contours.Count <= 0)
            {
                //肌色領域が(顔以外に)なかった = 手は写ってないので終了
                HasValidHandArea = false;
                return;
            }
            
            var contour = SelectLargestContour(contours);
            
            var boundRect = Imgproc.boundingRect(contour);
            //画像の下側で手首の凹み部分を検出することがあるのを、指の凹みと誤認識しないためのガードです。
            double defectMinY = boundRect.y + boundRect.height * 0.7;

            var pointMat = new MatOfPoint2f();
            Imgproc.approxPolyDP(new MatOfPoint2f(contour.toArray()), pointMat, 3, true);
            contour = new MatOfPoint(pointMat.toArray());

            var handArea = Imgproc.minAreaRect(pointMat);
            var handAreaCenter = handArea.center;
            var handAreaSize = handArea.size;
            HandAreaCenter = new Vector2((float)handAreaCenter.x, (float)handAreaCenter.y);
            HandAreaSize = new Vector2((float)handAreaSize.width, (float)handAreaSize.height);
            HandAreaRotation = (float)handArea.angle;            
            
            Imgproc.convexHull(contour, _hullIndices);
            var hullIndicesArray = _hullIndices.toArray();
            
            //通常ありえないが、凸包がちゃんと作れてないケース
            if (hullIndicesArray.Length < 3)
            {
                HasValidHandArea = false;
                return;
            }

            UpdateConvexityDefection(contour, _hullIndices, defectMinY);
        }
        
        private List<MatOfPoint> GetSkinColorContours(Mat mat)
        {
            Imgproc.GaussianBlur(mat, mat, _gaussianBlurKernelSize, 1, 1);

            _detector.DownScaleCount = DownScaleCount;
            _detector.AbsoluteMinContourArea = MinContourArea;
            _detector.SetColorRadius(HRadius, SRadius, VRadius);
            _detector.RefreshColorBounds();
            _detector.Process(mat);
            return _detector.GetContours();
        }

        private MatOfPoint SelectLargestContour(List<MatOfPoint> contours)
        {
            var bound = Imgproc.boundingRect(contours[0]);
            double boundSize = bound.width * bound.height;
            int boundPos = 0;

            for (int i = 1; i < contours.Count; i++)
            {
                bound = Imgproc.boundingRect(contours[i]);
                if (bound.width * bound.height > boundSize)
                {
                    boundSize = bound.width * bound.height;
                    boundPos = i;
                }
            }
            
            return contours[boundPos];
        }
        
        private void UpdateConvexityDefection(MatOfPoint contour, MatOfInt hullIndices, double defectMinY)
        {
            var contourArray = contour.toArray();
            var convexDefect = new MatOfInt4();
            Imgproc.convexityDefects(contour, hullIndices, convexDefect);
            
            ConvexDefectVectors.Clear();
            
            int convexDefectCount = convexDefect.rows();
            if (convexDefectCount > 0)
            {
                for (int i = 0; i < convexDefectCount; i++)
                {
                    convexDefect.get(i, 0, _convexityDefectSetValues);
                    Point farPoint = contourArray[_convexityDefectSetValues[2]];
                    int depth = _convexityDefectSetValues[3];
                    if (depth > DefectThreasholdValue && farPoint.y < defectMinY)
                    {
                        var nearPoint1 = contourArray[_convexityDefectSetValues[0]];
                        var nearPoint2 = contourArray[_convexityDefectSetValues[1]];
                        ConvexDefectVectors.Add(new Vector2(
                            (float)(nearPoint1.x * 0.5f + nearPoint2.x * 0.5 - farPoint.x),
                            (float)(nearPoint1.y * 0.5f + nearPoint2.y * 0.5 - farPoint.y)
                        ));
                    }
                }
            }

            //ここまでやりきると全データが有効に更新されている。
            HasValidHandArea = true;
        }
    }
}

#endif //VMAGICMIRROR_USE_OPENCV
