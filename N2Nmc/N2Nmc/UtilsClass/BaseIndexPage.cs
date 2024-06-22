using N2Nmc.Views.SubPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Windows.Data;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace N2Nmc.UtilsClass
{
    public abstract class BaseIndexPage : Page
    {
        public readonly static DoubleAnimation smallerAnimation = new DoubleAnimation { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        public readonly static DoubleAnimation smallsmallerAnimation = new DoubleAnimation { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        public readonly static DoubleAnimation biggerAnimation = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        List<UIElement> indexers;

        public BaseIndexPage()
        {
            indexers = new();
        }

        protected void PlayIndexerIn()
        {
            int i = 0;
            foreach (var it in indexers)
            {
                if (it == null)
                    continue;
                var transforms = it.RenderTransform as TransformGroup;
                if (transforms == null)
                    continue;
                var ttt = transforms.Children[2] as TranslateTransform;
                if (ttt == null)
                    continue;

                it.BeginAnimation(UIElement.OpacityProperty, null);
                it.Opacity = 0;
                it.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
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

        protected void InitIndexer(object obj)
        {
            var border = obj as Border;
            if (border == null)
                throw new ArgumentNullException(nameof(border));

            var indexer = border.DataContext as Indexer;
            if (indexer == null)
                throw new NullReferenceException(nameof(indexer));

            if (!indexer.TransformInited)
            {
                border.RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, border.Width * .5, border.Height * .5), new SkewTransform(0, 0, 0, 0), new TranslateTransform() }) };
                indexer.TransformInited = true;
            }

            var textbs = border.FindVisualChildren<TextBlock>();
            foreach (var textb in textbs)
            {
                var Runs = new List<Run>();
                foreach (var inline in textb.Inlines)
                    if (inline is Run run && run.Name.Contains("_dynamic"))
                        Runs.Add(run);
                foreach (var run in Runs)
                {
                    if (run.Text.StartsWith("@"))
                        run.SetResourceReference(Run.TextProperty, run.Text.Remove(0,1));
                }
            }
        }
        protected void TempGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            if (border == null)
                throw new NullReferenceException(nameof(border));

            var indexer = border.DataContext as Indexer;
            if (indexer == null)
                throw new NullReferenceException(nameof(indexer));
            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            border.Background = new SolidColorBrush(Color.FromArgb(0x9F, 0xB3, 0xB3, 0xB3));

            TransformGroup TG = (TransformGroup)border.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallerAnimation);
        }
        protected void TempGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            if (border == null)
                throw new NullReferenceException(nameof(border));

            var indexer = border.DataContext as Indexer;
            if (indexer == null)
                throw new NullReferenceException(nameof(indexer));
            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            border.Background = new SolidColorBrush(Color.FromArgb(0x5F, 0xB3, 0xB3, 0xB3));

            TransformGroup TG = (TransformGroup)border.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);
        }
        protected void TempGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null)
                throw new NullReferenceException(nameof(border));

            var indexer = border.DataContext as Indexer;
            if (indexer == null)
                throw new NullReferenceException(nameof(indexer));
            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            TransformGroup TG = (TransformGroup)border.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, smallsmallerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, smallsmallerAnimation);
        }
        protected void TempGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null)
                throw new NullReferenceException(nameof(border));

            var indexer = border.DataContext as Indexer;
            if (indexer == null)
                throw new NullReferenceException(nameof(indexer));
            if (!indexer.TransformInited)
                throw new NullReferenceException("RenderTransform not initialized");

            TransformGroup TG = (TransformGroup)border.RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];

            st.BeginAnimation(ScaleTransform.ScaleXProperty, biggerAnimation);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, biggerAnimation);
        }
        protected void TempGrid_Initialized(object sender, EventArgs e)
        {
            InitIndexer(sender);
            indexers.Add((UIElement)sender);
        }

        protected abstract void TempGrid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e);

        protected void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PlayIndexerIn();
        }

        protected void NavigateIndexPage(BaseIndexPage? indexPage)
        {
            indexPage?.PlayIndexerIn();
            SharedData.GetMainView.NavigatePage(indexPage);
        }

        protected void NavigatePage(Page? page)
        {
            SharedData.GetMainView.NavigatePage(page);
        }
    }

    public class Indexer : DependencyObject
    {
        public bool TransformInited = false;

        public string IndexerTitle { get; set; } = string.Empty;
        public string IndexerDescription { get; set; } = string.Empty;
        public ImageSource? IndexerCBI { get; set; } = null;

        public string nagivKey { get; set; } = string.Empty;
    }

    public class IndexerTextConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueString = value as string;
            if (valueString != null)
            {
                if (valueString.StartsWith("@"))
                    valueString = valueString.Remove(0, 1);

                var resource = Application.Current.FindResource(valueString);
                if (resource is string stringResource)
                {
                    return stringResource;
                }
            }
            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
