using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public partial class VerticalTabHeaderIconText : UserControl
    {
        public VerticalTabHeaderIconText()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public PackIconKind IconKind
        {
            get => (PackIconKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

        public double IconWidth
        {
            get => (double)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        public double IconHeight
        {
            get => (double)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }

        public Thickness TextMargin
        {
            get => (Thickness)GetValue(TextMarginProperty);
            set => SetValue(TextMarginProperty, value);
        }

        public double TranslateX
        {
            get => (double)GetValue(TranslateXProperty);
            set => SetValue(TranslateXProperty, value);
        }

        public static readonly DependencyProperty TextProperty
            = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(VerticalTabHeaderIconText),
                new PropertyMetadata("", OnTextChanged)
                );

        public static readonly DependencyProperty IconKindProperty
            = DependencyProperty.Register(
                nameof(IconKind),
                typeof(PackIconKind),
                typeof(VerticalTabHeaderIconText),
                new PropertyMetadata(PackIconKind.Abc, OnIconChanged)
                );

        public static readonly DependencyProperty IconWidthProperty
            = DependencyProperty.Register(
                nameof(IconWidth),
                typeof(double),
                typeof(VerticalTabHeaderIconText),
                new PropertyMetadata(22.0, OnIconWidthChanged)
                );

        public static readonly DependencyProperty IconHeightProperty
            = DependencyProperty.Register(
                nameof(IconHeight),
                typeof(double),
                typeof(VerticalTabHeaderIconText),
                new PropertyMetadata(22.0, OnIconHeightChanged)
                );

        public static readonly DependencyProperty TranslateXProperty =
            DependencyProperty.Register(
                nameof(TranslateX),
                typeof(double),
                typeof(VerticalTabHeaderIconText),
                new PropertyMetadata(0.0, OnTranslateXChanged)
                );

        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(
                nameof(TextMargin),
                typeof(Thickness),
                typeof(VerticalTabHeaderIconText),
                new PropertyMetadata(new Thickness(3, 0, 5, 0), OnTextMarginChanged)
                );

        private static void OnIconWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabHeaderIconText control)
            {
                control.packIcon.Width = (double)e.NewValue;
            }
        }

        private static void OnIconHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabHeaderIconText control)
            {
                control.packIcon.Height = (double)e.NewValue;
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabHeaderIconText control)
            {
                control.textBlock.Text = (string)e.NewValue;
            }
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabHeaderIconText control)
            {
                control.packIcon.Kind = (PackIconKind)e.NewValue;
            }
        }

        private static void OnTextMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabHeaderIconText control)
            {
                control.textBlock.Margin = (Thickness)e.NewValue;
            }
        }

        private static void OnTranslateXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabHeaderIconText control)
            {
                control.PositionTransform.X = (double)e.NewValue;
            }
        }
    }
}
