#if VMAGICMIRROR_USE_OPENCV
using System;
using System.Collections.Generic;
using System.IO;
using Baku.OpenCvExt;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using UnityEngine;
using Rect = UnityEngine.Rect;
#endif

namespace Baku.VMagicMirror
{
    /// <summary>
    /// OpenCVforUnityのLibFaceDetectionV3WebCamTextureExampleをVMagicMirror用に色々いじったもの
    /// </summary>
    public class DnnFaceAnalyzeRoutine : FaceAnalyzeRoutineBase
    {
#if VMAGICMIRROR_USE_OPENCV
        
        #region 画像処理に必要な色々なパラメータ / 値のキャッシュ
        
        private readonly Scalar _mean = new Scalar(0, 0, 0, 0);
        private const float _scale = 1.0f;
        private const bool _swapRB = false;


        private string model = "YuFaceDetectNet.onnx";
        private const float confThreshold = 0.5f;

        //NOTE: NMS(Non-maximum suppression)は物体検出のよくある後処理でググると詳細が出ます
        private const float nmsThreshold = 0.4f;
        
        //NOTE: これは入力画像のサイズではなく、dnn上で取り扱うサイズ。
        //onnxを変更せずにここの値を変更できるが、変更してもあまり負荷が変わらなさそう
        private const int NetInputWidth = 320;
        private const int NetInputHeight = 240;

        private Net _net;
        private Mat _bgrMat;
        private Mat _rgbaMat;
        private int _rawInputWidth;
        private int _rawInputHeight;

        private List<string> _outBlobNames;

        private PriorBox _priorBox;
        private Mat boxes_m_c1;
        private Mat boxes_m_c4;
        private Mat confidences_m;
        private MatOfRect2d boxes;
        private MatOfFloat confidences;
        private MatOfInt indices;

        //NOTE: この辺は毎回GCAllocしてしまう量をちょっとでも減らすためのキャッシュ
        private readonly Mat _imInfo = new Mat(1, 3, CvType.CV_32FC1);
        private readonly Size _netInputSize = new Size(1f, 1f);
        private readonly List<Mat> _outMats = new List<Mat>();
        private readonly float[] _confidenceArr = new float[1];
        private readonly float[] _bboxArr = new float[4];
        private readonly float[] _landmarksArr = new float[10];

        // NOTE: 下記のfaceRectとかlandmarksは、OpenCV座標の値をそのまま入れる。_resultに入れる時点でお作法のよい値に直す
        private Rect _faceRect;
        // 鼻先、右目、左目、口の右、口の左、が順に入る
        private readonly Vector2[] _landMarks = new Vector2[5];

        #endregion
                
        private readonly DnnFaceParts _result = new DnnFaceParts();
        public override IFaceAnalyzeResult Result => _result;
        
        public override void SetUp()
        {
            base.SetUp();
            
            string modelFilePath = Path.Combine(Application.streamingAssetsPath, "dnn/" + model);
            if (!File.Exists(modelFilePath))
            {
                Debug.LogError("Model file not exist: " + modelFilePath);
            }
            else
            {
                //第2引数はもとのExampleでも使ってなかったので空にしておく
                _net = Dnn.readNet(modelFilePath, "");
                _outBlobNames = GetOutputsNames(_net);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _net?.Dispose();

            _bgrMat?.Dispose();
            _rgbaMat?.Dispose();

            _priorBox?.Dispose();
            _priorBox = null;

            boxes_m_c1?.Dispose();
            boxes_m_c4?.Dispose();
            confidences_m?.Dispose();
            boxes?.Dispose();
            confidences?.Dispose();
            indices?.Dispose();

            boxes_m_c1 = null;
            boxes_m_c4 = null;
            confidences_m = null;
            boxes = null;
            confidences = null;
            indices = null;
        }

        public override void Start()
        {
            base.Start();
            CanRequestNextProcess = true;
        }

        public override void Stop()
        {
            base.Stop();
            HasResultToApply = false;
        }

        protected override void RunFaceDetection() 
        {
            CheckImageSize( _inputWidth, _inputHeight);
            OpenCvExtUtils.ColorsToMat(_inputColors, _rgbaMat, _inputWidth, _inputHeight);
            _rawInputWidth = _inputWidth;
            _rawInputHeight = _inputHeight;
            //画像情報をコピー完了 = 次の画像を入れてもOK
            CanRequestNextProcess = true;
            
            if (_net == null)
            {
                return;
            }

            Imgproc.cvtColor(_rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);
            Mat blob = Dnn.blobFromImage(_bgrMat, _scale, _netInputSize, _mean, _swapRB, false);
            _net.setInput(blob);
            
            if (_net.getLayer(new DictValue(0)).outputNameToIndex("im_info") != -1)
            { 
                Imgproc.resize(_bgrMat, _bgrMat, _netInputSize);
                _imInfo.put(0, 0, new float[] {
                    (float)_netInputSize.height,
                    (float)_netInputSize.width,
                    1.6f
                });
                _net.setInput(_imInfo, "im_info");
            }

            _outMats.Clear();

            //NOTE: この1行がとても重たい。手元環境だと7msくらい持っていく事があるのを確認している。
            _net.forward(_outMats, _outBlobNames);
            PostProcess(_outMats);
            foreach (var mat in _outMats)
            {
                mat.Dispose();
            }
            _outMats.Clear();
            blob.Dispose();

            //今持っている画像処理用のバッファがまだない or サイズが間違ってたら合わせる
            void CheckImageSize(int w, int h)
            {
                if (_bgrMat == null || _bgrMat.width() != w || _bgrMat.height() != h ||
                    _rgbaMat == null || _rgbaMat.width() != w || _rgbaMat.height() != h
                )
                {
                    _bgrMat = new Mat(h, w, CvType.CV_8UC3);
                    _rgbaMat = new Mat(h, w, CvType.CV_8UC4);
                    
                    var inputShape = new Size(NetInputWidth, NetInputHeight);
                    var outputShape = _bgrMat.size();
                    _priorBox = new PriorBox(inputShape, outputShape);

                    _netInputSize.width = NetInputWidth;
                    _netInputSize.height = NetInputHeight;
                }
            }

        }
   
        public override void ApplyResult(CalibrationData calibration, bool shouldCalibrate)
        {
            var imageWidth = (float) _rawInputWidth;
            var imageHeight = (float) _rawInputHeight;
            var scale = 1.0f / imageWidth;
            _result.ImageSize = new Vector2(imageWidth, imageHeight);
            //上下が逆なのと、正規化が前提なのでxを[-0.5, 0.5]の範囲におさめる
            _result.FaceRect = new Rect(
                _faceRect.xMin * scale - 0.5f, 
                (imageHeight * 0.5f - _faceRect.yMax) * scale,
                _faceRect.width * scale,
                _faceRect.height * scale
            );

            //NOTE: ここの順番はExampleコードと揃えてます
            var landMarks = _landMarks;
            _result.RightEye = new Vector2(landMarks[0].x, imageHeight - landMarks[0].y);
            _result.LeftEye = new Vector2(landMarks[1].x, imageHeight - landMarks[1].y);
            _result.NoseTop = new Vector2(landMarks[2].x, imageHeight - landMarks[2].y);
            _result.MouthRight = new Vector2(landMarks[3].x, imageHeight - landMarks[3].y);
            _result.MouthLeft = new Vector2(landMarks[4].x, imageHeight - landMarks[4].y);

            _result.DisableHorizontalFlip = DisableHorizontalFlip;
            _result.Calculate(calibration, shouldCalibrate);
            HasResultToApply = false;
        }

        public override void LerpToDefault(float lerpFactor) => _result.LerpToDefault(lerpFactor);

        private List<string> GetOutputsNames(Net net)
        {
            var result = new List<string>();

            var outLayers = net.getUnconnectedOutLayers();
            for (int i = 0; i < outLayers.total(); ++i)
            {
                result.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_name());
            }
            outLayers.Dispose();

            return result;
        }
        
        //メインの画像処理後に呼ぶことで特徴点を抜き出して保存する
        //OpenCVForUnityのコードの改造です
        private void PostProcess(List<Mat> outs)
        {
            Mat dets = _priorBox.Decode(outs[0], outs[1], outs[2]);
            
            int num = dets.rows();
            CheckMatsValidity(num);
            
            Mat bboxes = dets.colRange(0, 4);
            bboxes.convertTo(boxes_m_c1, CvType.CV_64FC1);

            Mat boxes_m_0_2 = boxes_m_c1.colRange(0, 2);
            Mat boxes_m_2_4 = boxes_m_c1.colRange(2, 4);
            Core.subtract(boxes_m_2_4, boxes_m_0_2, boxes_m_2_4);

            MatUtils.copyToMat(new IntPtr(boxes_m_c1.dataAddr()), boxes_m_c4);

            Mat scores = dets.colRange(14, 15);
            scores.copyTo(confidences_m);

            Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices);
            //削ったうえでconfidenceが一番高い所を拾いに行く
            float maxConf = 0f;
            int maxConfIdx = -1;
            for (int i = 0; i < indices.total(); ++i)
            {
                int index = (int)indices.get(i, 0)[0];
                confidences.get(index, 0, _confidenceArr);
                if (_confidenceArr[0] > maxConf)
                {
                    maxConf = _confidenceArr[0];
                    maxConfIdx = index;
                }
            }

            // トラッキングロスするとここに到達: 
            if (maxConfIdx == -1)
            {
                RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
                {
                    RgbaMat = _rgbaMat,
                    HasValidFaceArea = false,
                    Width = _rgbaMat.width(),
                    Height = _rgbaMat.height(),
                });
                return;
            }

            int idx = maxConfIdx;
            
            bboxes.get(idx, 0, _bboxArr);
            _faceRect = new Rect(
                _bboxArr[0], 
                _bboxArr[1], 
                _bboxArr[2] - _bboxArr[0],
                _bboxArr[3] - _bboxArr[1]
                );
            
            Mat landmarks = dets.colRange(4, 14);
            landmarks.get(idx, 0, _landmarksArr);
            for (int i = 0; i < 5; i++)
            {
                _landMarks[i] = new Vector2(_landmarksArr[i * 2], _landmarksArr[i * 2 + 1]);
            }
            
            RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
            {
                RgbaMat = _rgbaMat,
                HasValidFaceArea = true,
                Width = _rgbaMat.width(),
                Height = _rgbaMat.height(),
                //TODO: 合ってる？合ってればハンドトラッキングが動くし、合ってなければ動かない。
                FaceArea = _faceRect,
            });

            HasResultToApply = true;
        }

        private void CheckMatsValidity(int rows)
        {
            if (boxes_m_c1 == null)
            {
                boxes_m_c1 = new Mat(rows, 4, CvType.CV_64FC1);
            }

            if (boxes_m_c4 == null)
            {
                boxes_m_c4 = new Mat(rows, 1, CvType.CV_64FC4);
            }

            if (confidences_m == null)
            {
                confidences_m = new Mat(rows, 1, CvType.CV_32FC1);
            }

            if (boxes == null)
            {
                boxes = new MatOfRect2d(boxes_m_c4);
            }

            if (confidences == null)
            {
                confidences = new MatOfFloat(confidences_m);
            }

            if (indices == null)
            {
                indices = new MatOfInt();
            }
        }

        private class PriorBox
        {
            private static readonly float[][] MinSizes = {
                new float[]{10.0f,  16.0f,  24.0f},
                new float[]{32.0f,  48.0f},
                new float[]{64.0f,  96.0f},
                new float[]{128.0f, 192.0f, 256.0f}
            };

            private static readonly int[] Steps = new int[] { 8, 16, 32, 64 };
            private static readonly float[] Variance = new float[] { 0.1f, 0.2f };

            private readonly int in_w;
            private readonly int in_h;
            private readonly int out_w;
            private readonly int out_h;

            private readonly List<Size> feature_map_sizes;
            private readonly Mat priors;

            private Mat dets;
            private Mat ones;
            private Mat scale;

            private readonly Mat priors_0_2;
            private readonly Mat priors_2_4;
            private Mat bboxes;
            private Mat bboxes_0_2;
            private Mat bboxes_2_4;
            private  Mat landmarks;
            private Mat landmarks_0_2;
            private Mat landmarks_2_4;
            private Mat landmarks_4_6;
            private Mat landmarks_6_8;
            private Mat landmarks_8_10;
            private Mat scores;
            private Mat ones_0_1;
            private Mat ones_0_2;
            private Mat bbox_scale;
            private Mat landmark_scale;

            public PriorBox(Size inputShape, Size outputShape)
            {
                // initialize
                in_w = (int)inputShape.width;
                in_h = (int)inputShape.height;
                out_w = (int)outputShape.width;
                out_h = (int)outputShape.height;

                Size feature_map_2nd = new Size((int)((int)((in_w + 1) / 2) / 2), (int)((int)((in_h + 1) / 2) / 2));
                Size feature_map_3rd = new Size((int)(feature_map_2nd.width / 2), (int)(feature_map_2nd.height / 2));
                Size feature_map_4th = new Size((int)(feature_map_3rd.width / 2), (int)(feature_map_3rd.height / 2));
                Size feature_map_5th = new Size((int)(feature_map_4th.width / 2), (int)(feature_map_4th.height / 2));
                Size feature_map_6th = new Size((int)(feature_map_5th.width / 2), (int)(feature_map_5th.height / 2));

                feature_map_sizes = new List<Size>
                {
                    feature_map_3rd, feature_map_4th, feature_map_5th, feature_map_6th
                };

                priors = GeneratePrior();
                priors_0_2 = priors.colRange(new Range(0, 2));
                priors_2_4 = priors.colRange(new Range(2, 4));
            }

            private Mat GeneratePrior()
            {
                int priors_size = 0;
                for (int index = 0; index < feature_map_sizes.Count; index++)
                    priors_size += (int)(feature_map_sizes[index].width * feature_map_sizes[index].height * MinSizes[index].Length);

                Mat anchors = new Mat(priors_size, 4, CvType.CV_32FC1);
                int count = 0;
                for (int i = 0; i < feature_map_sizes.Count; i++)
                {
                    Size feature_map_size = feature_map_sizes[i];
                    float[] min_size = MinSizes[i];

                    for (int _h = 0; _h < feature_map_size.height; _h++)
                    {
                        for (int _w = 0; _w < feature_map_size.width; _w++)
                        {
                            for (int j = 0; j < min_size.Length; j++)
                            {
                                float s_kx = min_size[j] / in_w;
                                float s_ky = min_size[j] / in_h;

                                float cx = (float)((_w + 0.5) * Steps[i] / in_w);
                                float cy = (float)((_h + 0.5) * Steps[i] / in_h);

                                anchors.put(count, 0, new float[] { cx, cy, s_kx, s_ky });

                                count++;
                            }
                        }
                    }
                }

                return anchors;
            }

            /// <summary>
            /// Decodes the locations (x1, y1, x2, y2...) and scores (c) from the priors, and the given loc and conf.
            /// </summary>
            /// <param name="loc"></param>
            /// <param name="conf"></param>
            /// <param name="iou"></param>
            /// <returns>dets is concatenated by bboxes, landmarks and scoress. num * [x1, y1, x2, y2, x_re, y_re, x_le, y_le, x_ml, y_ml, x_n, y_n, x_mr, y_ml, score]</returns>
            public Mat Decode(Mat loc, Mat conf, Mat iou)
            {
                Mat loc_m = loc.reshape(1, new int[] { loc.size(1), loc.size(2) }); // [1*num*14] to [num*14]
                Mat conf_m = conf.reshape(1, new int[] { conf.size(1), conf.size(2) }); // [1*num*2] to [num*2]
                Mat iou_m = iou.reshape(1, new int[] { iou.size(1), iou.size(2) }); // [1*num*1] to [num*1]

                int num = loc_m.rows();

                if (dets == null || (dets != null && dets.IsDisposed))
                {
                    dets = new Mat(num, 15, CvType.CV_32FC1);
                    ones = Mat.ones(num, 2, CvType.CV_32FC1);
                    scale = new Mat(num, 1, CvType.CV_32FC4, new Scalar(out_w, out_h, out_w, out_h));
                    scale = scale.reshape(1, num);

                    bboxes = dets.colRange(0, 4);
                    bboxes_0_2 = bboxes.colRange(0, 2);
                    bboxes_2_4 = bboxes.colRange(2, 4);
                    landmarks = dets.colRange(4, 14);
                    landmarks_0_2 = landmarks.colRange(0, 2);
                    landmarks_2_4 = landmarks.colRange(2, 4);
                    landmarks_4_6 = landmarks.colRange(4, 6);
                    landmarks_6_8 = landmarks.colRange(6, 8);
                    landmarks_8_10 = landmarks.colRange(8, 10);
                    scores = dets.colRange(14, 15);
                    ones_0_1 = ones.colRange(0, 1);
                    ones_0_2 = ones.colRange(0, 2);
                    bbox_scale = scale.colRange(0, 4);
                    landmark_scale = scale.colRange(0, 2);
                }


                Mat loc_0_2 = loc_m.colRange(0, 2);
                Mat loc_2_4 = loc_m.colRange(2, 4);
                Mat loc_2_3 = loc_m.colRange(2, 3);
                Mat loc_3_4 = loc_m.colRange(3, 4);

                // # get bboxes
                Core.multiply(loc_0_2, priors_2_4, bboxes_0_2, Variance[0]);
                Core.add(priors_0_2, bboxes_0_2, bboxes_0_2);
                Core.multiply(loc_2_3, ones_0_1, loc_2_3, Variance[0]);
                Core.multiply(loc_3_4, ones_0_1, loc_3_4, Variance[1]);
                Core.exp(loc_2_4, bboxes_2_4);
                Core.multiply(priors_2_4, bboxes_2_4, bboxes_2_4);

                // # (x_c, y_c, w, h) -> (x1, y1, x2, y2)
                Core.divide(bboxes_2_4, ones_0_2, loc_2_4, 0.5);
                Core.subtract(bboxes_0_2, loc_2_4, bboxes_0_2);
                Core.add(bboxes_2_4, bboxes_0_2, bboxes_2_4);

                // # scale recover
                Core.multiply(bboxes, bbox_scale, bboxes);


                Mat loc_4_6 = loc_m.colRange(4, 6);
                Mat loc_6_8 = loc_m.colRange(6, 8);
                Mat loc_8_10 = loc_m.colRange(8, 10);
                Mat loc_10_12 = loc_m.colRange(10, 12);
                Mat loc_12_14 = loc_m.colRange(12, 14);

                // # get landmarks
                Core.multiply(loc_4_6, priors_2_4, landmarks_0_2, Variance[0]);
                Core.add(priors_0_2, landmarks_0_2, landmarks_0_2);
                Core.multiply(loc_6_8, priors_2_4, landmarks_2_4, Variance[0]);
                Core.add(priors_0_2, landmarks_2_4, landmarks_2_4);
                Core.multiply(loc_8_10, priors_2_4, landmarks_4_6, Variance[0]);
                Core.add(priors_0_2, landmarks_4_6, landmarks_4_6);
                Core.multiply(loc_10_12, priors_2_4, landmarks_6_8, Variance[0]);
                Core.add(priors_0_2, landmarks_6_8, landmarks_6_8);
                Core.multiply(loc_12_14, priors_2_4, landmarks_8_10, Variance[0]);
                Core.add(priors_0_2, landmarks_8_10, landmarks_8_10);

                // # scale recover
                Core.multiply(landmarks_0_2, landmark_scale, landmarks_0_2);
                Core.multiply(landmarks_2_4, landmark_scale, landmarks_2_4);
                Core.multiply(landmarks_4_6, landmark_scale, landmarks_4_6);
                Core.multiply(landmarks_6_8, landmark_scale, landmarks_6_8);
                Core.multiply(landmarks_8_10, landmark_scale, landmarks_8_10);


                // # get score
                Mat cls_scores = conf_m.colRange(1, 2);
                Mat iou_scores = iou_m;
                Imgproc.threshold(iou_scores, iou_scores, 0, 0, Imgproc.THRESH_TOZERO);
                Imgproc.threshold(iou_scores, iou_scores, 1.0, 0, Imgproc.THRESH_TRUNC);
                Core.multiply(cls_scores, iou_scores, scores);
                Core.sqrt(scores, scores);

                return dets;
            }

            public void Dispose()
            {
                priors?.Dispose();

                dets?.Dispose();
                ones?.Dispose();
                scale?.Dispose();
            }
        }        
#else
        //NOTE: OpenCVを使ってない場合はカラ実装を当て、データの受信準備もしないし、顔検出の成功判定も出さない。
        //(動作保証として若干怪しいが、まあコンパイル通らないよりマシ)
        public override IFaceAnalyzeResult Result { get; } = new DnnBasedFaceParts();
        public override void LerpToDefault(float lerpFactor)
        {
        }

        protected override void RunFaceDetection()
        {
        }

        public override void ApplyResult(CalibrationData calibration, bool shouldCalibrate)
        {
        }
#endif
    }
}
