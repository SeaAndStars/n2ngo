using N2NGO.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace N2NGO.Views.IndexPages
{
    public abstract class BaseIndexPage : Page
    {
        public readonly static DoubleAnimation SmallerAnimation = new() { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        public readonly static DoubleAnimation SmallsmallerAnimation = new() { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        public readonly static DoubleAnimation BiggerAnimation = new() { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        private readonly List<UIElement> _indexers = new();

        public BaseIndexPage()
        {

        }

        protected void PlayIndexerIn()
        {
            int i = 0;
            foreach (var it in _indexers)
            {
                if (it == null)
                    continue;
                if (it.RenderTransform is not TransformGroup transforms)
                    continue;

                foreach (var _ in transforms.Children)
                {
                    // TranslateTransform
                    if (_ is TranslateTransform ttt)
                    {
                        it.BeginAnimation(OpacityProperty, null);
                        it.Opacity = 0;
                        it.BeginAnimation(OpacityProperty, new DoubleAnimation
                        {
                            BeginTime = TimeSpan.FromSeconds(i * 0.12),
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.12)
                        });


                        ttt.BeginAnimation(TranslateTransform.XProperty, null);
                        ttt.BeginAnimation(TranslateTransform.YProperty, null);
                        ttt.X = 125;
                        ttt.Y = -80;
                        ttt.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
                        {
                            BeginTime = TimeSpan.FromSeconds(i * 0.13),
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.23),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        });
                        ttt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
                        {
                            BeginTime = TimeSpan.FromSeconds(i * 0.14),
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        });

                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Update indexer
        /// </summary>
        /// <param name="obj">UI Element based on <see cref="Decorator"></see></param>

        protected static void NavigateIndexPage(BaseIndexPage? indexPage)
        {
            if (indexPage != null)
            {
                indexPage.PlayIndexerIn();
                if (indexPage is BaseIndexPage indexPage_BaseIndexPage)
                    indexPage_BaseIndexPage.Dispatcher.InvokeAsync(() => indexPage_BaseIndexPage.Page_Navigated());
            }

            Globals.CurrentApp.MainWindow.NavigatePage(indexPage);
        }
        protected static void NavigatePage(Page? page)
        {
            Globals.CurrentApp.MainWindow.NavigatePage(page);
        }
        protected static void InitIndexer(object obj)
        {
            if (obj is not Border element)
                throw new ArgumentException("obj is not Border", nameof(obj));

            if (element.DataContext is not Indexer indexer)
            {
                // throw new Exception("element.DataContext is not Indexer");

                indexer = new Indexer();
                element.DataContext = indexer;
            }

            if (!indexer.TransformInited)
            {
                element.RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, element.Width * .5, element.Height * .5), new SkewTransform(0, 0, 0, 0), new TranslateTransform() }) };
                indexer.TransformInited = true;
            }

            var textbs = element.FindVisualChildren<TextBlock>();
            foreach (var textb in textbs)
            {
                var Runs = new List<Run>();
                foreach (var inline in textb.Inlines)
                    if (inline is Run run && run.Name.Contains("_dynamic"))
                        Runs.Add(run);
                foreach (var run in Runs)
                {
                    if (run.Text.StartsWith("@"))
                        run.SetResourceReference(Run.TextProperty, run.Text.Remove(0, 1));
                }
            }
        }

        protected void Indexer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not Border element)
                throw new ArgumentException("sender is not Border", nameof(sender));

            if (element.DataContext is not Indexer indexer)
                throw new Exception("element.DataContext is not Indexer");

            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            if (element.RenderTransform is not TransformGroup transforms)
                throw new Exception("element.RenderTransform is not TransformGroup");

            foreach (var _ in transforms.Children)
            {
                // TranslateTransform
                if (_ is ScaleTransform scaleTransform)
                {
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SmallerAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, SmallerAnimation);
                }
            }

            element.Background = new SolidColorBrush(Color.FromArgb(0x4F, 0xB3, 0xB3, 0xB3));
        }
        protected void Indexer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not Border element)
                throw new ArgumentException("sender is not Border", nameof(sender));

            if (element.DataContext is not Indexer indexer)
                throw new Exception("element.DataContext is not Indexer");

            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            if (element.RenderTransform is not TransformGroup transforms)
                throw new Exception("element.RenderTransform is not TransformGroup");

            foreach (var _ in transforms.Children)
            {
                // TranslateTransform
                if (_ is ScaleTransform scaleTransform)
                {
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, BiggerAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, BiggerAnimation);
                }
            }

            element.Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xB3, 0xB3, 0xB3));
        }
        protected void Indexer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not Border element)
                throw new ArgumentException("sender is not Border", nameof(sender));

            if (element.DataContext is not Indexer indexer)
                throw new Exception("element.DataContext is not Indexer");

            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            if (element.RenderTransform is not TransformGroup transforms)
                throw new Exception("element.RenderTransform is not TransformGroup");

            foreach (var _ in transforms.Children)
            {
                // TranslateTransform
                if (_ is ScaleTransform scaleTransform)
                {
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SmallsmallerAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, SmallsmallerAnimation);
                }
            }
        }
        protected void Indexer_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not Border element)
                throw new ArgumentException("sender is not Border", nameof(sender));

            if (element.DataContext is not Indexer indexer)
                throw new Exception("element.DataContext is not Indexer");

            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            if (element.RenderTransform is not TransformGroup transforms)
                throw new Exception("element.RenderTransform is not TransformGroup");

            foreach (var _ in transforms.Children)
            {
                // TranslateTransform
                if (_ is ScaleTransform scaleTransform)
                {
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, BiggerAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, BiggerAnimation);
                }
            }
        }
        protected void Indexer_Initialized(object sender, EventArgs e)
        {
            InitIndexer(sender);
            _indexers.Add((UIElement)sender);
        }

        protected abstract void Indexer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e);

        protected virtual void Page_Navigated() { }

        protected virtual void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PlayIndexerIn();
            CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>((Page)sender));
        }
    }

    public class Indexer
    {
        public bool TransformInited = false;

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public string IndexerTitle { get; set; } = string.Empty;
        public string IndexerDescription { get; set; } = string.Empty;
        public ImageSource? IndexerCBI { get; set; } = null;

        public string NagivKey { get; set; } = string.Empty;
    }

    public class IndexerTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string valueString)
                return value;

            if (valueString.StartsWith("@"))
                valueString = valueString.Remove(0, 1);

            var resource = Application.Current.FindResource(valueString);
            if (resource is string stringResource)
            {
                return stringResource;
            }
            return resource;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
