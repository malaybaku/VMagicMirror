using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Baku.VMagicMirrorConfig
{
    public partial class HandTrackingResultPanel : UserControl
    {
        public HandTrackingResultPanel()
        {
            InitializeComponent();

            _leftPoints = new[]
            {
                LeftPoint00,
                LeftPoint01,
                LeftPoint02,
                LeftPoint03,
                LeftPoint04,
                LeftPoint05,
                LeftPoint06,
                LeftPoint07,
                LeftPoint08,
                LeftPoint09,
                LeftPoint10,
                LeftPoint11,
                LeftPoint12,
                LeftPoint13,
                LeftPoint14,
                LeftPoint15,
                LeftPoint16,
                LeftPoint17,
                LeftPoint18,
                LeftPoint19,
                LeftPoint20,
            };

            _rightPoints = new[]
            {
                RightPoint00,
                RightPoint01,
                RightPoint02,
                RightPoint03,
                RightPoint04,
                RightPoint05,
                RightPoint06,
                RightPoint07,
                RightPoint08,
                RightPoint09,
                RightPoint10,
                RightPoint11,
                RightPoint12,
                RightPoint13,
                RightPoint14,
                RightPoint15,
                RightPoint16,
                RightPoint17,
                RightPoint18,
                RightPoint19,
                RightPoint20,
            };
        }

        #region Dependency Properties

        public Point[] LeftPoints
        {
            get => (Point[])GetValue(LeftPointsProperty);
            set => SetValue(LeftPointsProperty, value);
        }

        public static readonly DependencyProperty LeftPointsProperty = DependencyProperty.Register(
            nameof(LeftPoints),
            typeof(Point[]),
            typeof(HandTrackingResultPanel), new PropertyMetadata(Array.Empty<Point>(), OnLeftPointsChanged) {}
            );

        public Point[] RightPoints
        {
            get => (Point[])GetValue(RightPointsProperty);
            set => SetValue(RightPointsProperty, value);
        }

        public static readonly DependencyProperty RightPointsProperty = DependencyProperty.Register(
            nameof(RightPoints),
            typeof(Point[]),
            typeof(HandTrackingResultPanel), new PropertyMetadata(Array.Empty<Point>(), OnRightPointsChanged)
            );

        #endregion

        private readonly Ellipse[] _leftPoints;
        private readonly Ellipse[] _rightPoints;

        private static void OnLeftPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {          
            if (d is not HandTrackingResultPanel panel || e.NewValue is not Point[] points)
            {
                return;
            }

            for (int i = 0; i < panel._leftPoints.Length; i++)
            {
                if (i >= points.Length)
                {
                    return;
                }

                Canvas.SetLeft(panel._leftPoints[i], points[i].X);
                Canvas.SetTop(panel._leftPoints[i], points[i].Y);
                if (i == 0)
                {
                    LogOutput.Instance.Write($"L[0]= {points[0].X:0.000}, {points[0].Y:0.000}");
                }
            }
        }

        private static void OnRightPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not HandTrackingResultPanel panel || e.NewValue is not Point[] points)
            {
                return;
            }

            for (int i = 0; i < panel._rightPoints.Length; i++)
            {
                if (i >= points.Length)
                {
                    return;
                }

                Canvas.SetLeft(panel._rightPoints[i], points[i].X);
                Canvas.SetTop(panel._rightPoints[i], points[i].Y);
            }
        }
    }
}
