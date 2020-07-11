using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

namespace Baku.VMagicMirror
{
    public class ColorBlobDetector
    {
        /// <summary> ColorBlobの計算をダウンスケールする回数として0, 1, 2のいずれかを取得、設定します。 </summary>
        public int DownScaleCount { get; set; } = 0;

        /// <summary> ピクセル面積ベースで、Contourの要素として認める最小のサイズを取得、設定します。 </summary>
        public double AbsoluteMinContourArea { get; set; } = 2000;
        
        private readonly Scalar mLowerBound = new Scalar(0);
        private readonly Scalar mUpperBound = new Scalar(0);
        
        // 色半径
        private readonly Scalar mColorRadius = new Scalar(25, 50, 50, 0);
        private readonly Scalar mColor = new Scalar(0, 0, 0, 255);
        // 検出した輪郭一覧
        private readonly List<MatOfPoint> mContours = new List<MatOfPoint>();
        
        // その他のキャッシュ
        private readonly Mat mPyrDownMat1 = new Mat();
        private readonly Mat mPyrDownMat2 = new Mat();
        private readonly Mat mHsvMat = new Mat();
        private readonly Mat mMask = new Mat();
        private readonly Mat mDilatedMask = new Mat();
        private readonly Mat mHierarchy = new Mat();
        private readonly Scalar resizeScaleScalar = new Scalar(1, 1);
        private readonly List<MatOfPoint> contourCandidates = new List<MatOfPoint>();
        private readonly Mat dilateKernel = new Mat();

        public void SetColorRadius(double h, double s, double v)
        {
            mColorRadius.val[0] = h;
            mColorRadius.val[1] = s;
            mColorRadius.val[2] = v;
        }
        
        public void SetHsvColor(float h, float s, float v)
        {
            mColor.val[0] = h;
            mColor.val[1] = s;
            mColor.val[2] = v;
        }

        public void RefreshColorBounds()
        {
            mLowerBound.val[0] = 
                (mColor.val[0] >= mColorRadius.val[0]) ? mColor.val[0] - mColorRadius.val[0] : 0;
            mUpperBound.val[0] = 
                (mColor.val[0] + mColorRadius.val[0] <= 255) ? mColor.val[0] + mColorRadius.val[0] : 255;

            mLowerBound.val[1] = mColor.val[1] - mColorRadius.val[1];
            mUpperBound.val[1] = mColor.val[1] + mColorRadius.val[1];

            mLowerBound.val[2] = mColor.val[2] - mColorRadius.val[2];
            mUpperBound.val[2] = mColor.val[2] + mColorRadius.val[2];

            mLowerBound.val[3] = 0;
            mUpperBound.val[3] = 255;
        }
        
        public void Process(Mat rgbaImage)
        {
            //NOTE: ヘタに参照保持したくないので少しもっさり書いてます
            if (DownScaleCount == 0)
            {
                Imgproc.cvtColor(rgbaImage, mHsvMat, Imgproc.COLOR_RGB2HSV_FULL);
            }
            else if (DownScaleCount == 1)
            {
                Imgproc.pyrDown(rgbaImage, mPyrDownMat1);
                Imgproc.cvtColor(mPyrDownMat1, mHsvMat, Imgproc.COLOR_RGB2HSV_FULL);
            }
            else
            {
                Imgproc.pyrDown(rgbaImage, mPyrDownMat1);
                Imgproc.pyrDown(mPyrDownMat1, mPyrDownMat2);
                Imgproc.cvtColor(mPyrDownMat2, mHsvMat, Imgproc.COLOR_RGB2HSV_FULL);
            }

            Core.inRange(mHsvMat, mLowerBound, mUpperBound, mMask);
            Imgproc.dilate(mMask, mDilatedMask, dilateKernel);
            
            contourCandidates.Clear();
            Imgproc.findContours(
                mDilatedMask, contourCandidates, mHierarchy,
                Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE
                );

            // Filter contours by area and resize to fit the original image size
            int resizeScale =
                (DownScaleCount == 0) ? 1 :
                (DownScaleCount == 1) ? 2 : 4;
            resizeScaleScalar.val[0] = resizeScale;
            resizeScaleScalar.val[1] = resizeScale;
                
            mContours.Clear();
            foreach (var contour in contourCandidates)
            {
                if (Imgproc.contourArea(contour) * resizeScale * resizeScale > AbsoluteMinContourArea)
                {
                    if (resizeScale > 1)
                    {
                        Core.multiply(contour, resizeScaleScalar, contour);
                    }
                    mContours.Add(contour);
                }
            }
        }

        public List<MatOfPoint> GetContours() => mContours;

        public void Dispose()
        {
            mPyrDownMat1.Dispose();
            mPyrDownMat2.Dispose();
            mHsvMat.Dispose();
            mMask.Dispose();
            mDilatedMask.Dispose();
            mHierarchy.Dispose();
        }
    }
}