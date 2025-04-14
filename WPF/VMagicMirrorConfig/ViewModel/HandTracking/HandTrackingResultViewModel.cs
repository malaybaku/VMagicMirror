﻿using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class HandTrackingResultViewModel : ViewModelBase
    {
        //NOTE: UI側のサイズ。設計的にはViewModel上でUIの想定サイズを宣言しちゃダメなんだけど、このほうが分かりやすいので…
        private const double PanelWidth = 320f;
        private const double PanelHeight = 180f;

        public RProperty<bool> LeftOrRightDetected { get; } = new RProperty<bool>(false);

        public RProperty<bool> LeftDetected { get; } = new RProperty<bool>(false);
        public RProperty<bool> RightDetected { get; } = new RProperty<bool>(false);

        //NOTE: 21ポイント、というのは滅多に変わらないハズだけど、一応変わってもいいように実装する。
        //配列を2本使うのは、比較的ラクに(かつメモリに優しく)View側に変更通知を行うため

        public Point[] LeftPoints { get; private set; } = new Point[21];
        public Point[] RightPoints { get; private set; } = new Point[21];

        private Point[] _leftPointsBuffer = new Point[21];
        private Point[] _rightPointsBuffer = new Point[21];


        public void SetResult(HandTrackingResult model)
        {
            LeftDetected.Value = model.Left.Detected;
            RightDetected.Value = model.Right.Detected;
            LeftOrRightDetected.Value = model.Left.Detected || model.Right.Detected;

            SwapBuffer(model);

            if (model.Left.Points.Length > 0)
            {
                var src = model.Left.Points;
                for (int i = 0; i < LeftPoints.Length; i++)
                {
                    //NOTE: ミラーした状態で表示したいので反転している
                    LeftPoints[i].X = (1.0 - src[i].X) * PanelWidth;
                    LeftPoints[i].Y = src[i].Y * PanelHeight;
                }
                //NOTE: 配列スワップ + 値の書き込みが終わったのを受けてViewに通知
                RaisePropertyChanged(nameof(LeftPoints));
            }

            if (model.Right.Points.Length > 0)
            {
                var src = model.Right.Points;
                for (int i = 0; i < RightPoints.Length; i++)
                {
                    RightPoints[i].X = (1.0 - src[i].X) * PanelWidth;
                    RightPoints[i].Y = src[i].Y * PanelHeight;
                }
                RaisePropertyChanged(nameof(RightPoints));
            }
        }

        private void SwapBuffer(HandTrackingResult model)
        {
            if (model.Left.Points.Length > 0 && _leftPointsBuffer.Length != model.Left.Points.Length)
            {
                _leftPointsBuffer = new Point[model.Left.Points.Length];
            }

            if (model.Right.Points.Length > 0 && _rightPointsBuffer.Length != model.Right.Points.Length)
            {
                _rightPointsBuffer = new Point[model.Right.Points.Length];
            }

            (_leftPointsBuffer, LeftPoints) = (LeftPoints, _leftPointsBuffer);
            (_rightPointsBuffer, RightPoints) = (RightPoints, _rightPointsBuffer);
        }
    }

    public class HandTrackingResultPointViewModel
    {
        public RProperty<double> X { get; } = new RProperty<double>(0f);
        public RProperty<double> Y { get; } = new RProperty<double>(0f);
    }
}

