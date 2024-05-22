using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// InfoPage.xaml 的交互逻辑
    /// </summary>
    public partial class InfoPage : Page
    {
        Storyboard storyboard = new Storyboard();
        public InfoPage()
        {
            InitializeComponent();
            N2NGO_Warpper.Opacity = 0.0;
            N2NGO_N2N.Opacity = 0.0;
            N2NGO_GO.Opacity = 0.0;
            N2NGO_N2N_Border.Opacity = 0.0;

            storyboard.RepeatBehavior = RepeatBehavior.Forever;

            int delayMs1 = 3000;
            var a1 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(0 + delayMs1), To = 0, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(a1, N2NGO_base); Storyboard.SetTargetProperty(a1, new PropertyPath("(UIElement.Opacity)"));
            var a2 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(250 + delayMs1), To = 0, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(a2, N2NGO_Link); Storyboard.SetTargetProperty(a2, new PropertyPath("(UIElement.Opacity)"));
            var a3 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(450 + delayMs1), To = 0, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(a3, N2NGO_info); Storyboard.SetTargetProperty(a3, new PropertyPath("(UIElement.Opacity)"));

            int delayMs2 = (int)(a3.BeginTime.Value.TotalMilliseconds + 1200);
            var aa1 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(0 + delayMs2), To = 1, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aa1, N2NGO_Warpper); Storyboard.SetTargetProperty(aa1, new PropertyPath("(UIElement.Opacity)"));
            var aa2 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(250 + delayMs2), To = 1, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aa2, N2NGO_N2N_Border); Storyboard.SetTargetProperty(aa2, new PropertyPath("(UIElement.Opacity)"));
            var aa3 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(450 + delayMs2), To = 1, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aa3, N2NGO_GO); Storyboard.SetTargetProperty(aa3, new PropertyPath("(UIElement.Opacity)"));
            var aa4 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(900 + delayMs2), To = 1, Duration = TimeSpan.FromMilliseconds(1200), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aa4, N2NGO_N2N); Storyboard.SetTargetProperty(aa4, new PropertyPath("(UIElement.Opacity)"));

            int delayMs3 = (int)(aa4.BeginTime.Value.TotalMilliseconds + 2500);
            var aaa1 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(0 + delayMs3), To = 0, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaa1, N2NGO_Warpper); Storyboard.SetTargetProperty(aaa1, new PropertyPath("(UIElement.Opacity)"));
            var aaa2 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(250 + delayMs3), To = 0, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaa2, N2NGO_N2N_Border); Storyboard.SetTargetProperty(aaa2, new PropertyPath("(UIElement.Opacity)"));
            var aaa3 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(850 + delayMs3), To = 0, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaa3, N2NGO_N2N); Storyboard.SetTargetProperty(aaa3, new PropertyPath("(UIElement.Opacity)"));
            var aaa4 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(1500 + delayMs3), To = 0, Duration = TimeSpan.FromMilliseconds(1200), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaa4, N2NGO_GO); Storyboard.SetTargetProperty(aaa4, new PropertyPath("(UIElement.Opacity)"));

            int delayMs4 = (int)(aaa4.BeginTime.Value.TotalMilliseconds + 1200);
            var aaaa1 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(0 + delayMs4), To = 1, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaaa1, N2NGO_base); Storyboard.SetTargetProperty(aaaa1, new PropertyPath("(UIElement.Opacity)"));
            var aaaa2 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(250 + delayMs4), To = 1, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaaa2, N2NGO_Link); Storyboard.SetTargetProperty(aaaa2, new PropertyPath("(UIElement.Opacity)"));
            var aaaa3 = new DoubleAnimation { BeginTime = TimeSpan.FromMilliseconds(450 + delayMs4), To = 1, Duration = TimeSpan.FromMilliseconds(800), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; Storyboard.SetTarget(aaaa3, N2NGO_info); Storyboard.SetTargetProperty(aaaa3, new PropertyPath("(UIElement.Opacity)"));

            storyboard.Children.Add(a1);
            storyboard.Children.Add(a2);
            storyboard.Children.Add(a3);
            storyboard.Children.Add(aa1);
            storyboard.Children.Add(aa2);
            storyboard.Children.Add(aa3);
            storyboard.Children.Add(aa4);
            storyboard.Children.Add(aaa1);
            storyboard.Children.Add(aaa2);
            storyboard.Children.Add(aaa3);
            storyboard.Children.Add(aaa4);
            storyboard.Children.Add(aaaa1);
            storyboard.Children.Add(aaaa2);
            storyboard.Children.Add(aaaa3);

            storyboard.Begin();
        }

        public void PlayLogoAnimation()
        {
        }

        string MakeTitle(object sender, string url)
        {
            return string.Format("{0} - [{1}]", ((Run)sender).Text, url);
        }

        private void Run_未来之乡_gitee(object sender, MouseButtonEventArgs e)
        {
            var url = "https://gitee.com/xue-jiangbin";
            SharedData.MakeMessageBoxContent(url, "未来之乡 - Gitee");
        }
        private void Run_LGF_github(object sender, MouseButtonEventArgs e)
        {
            var url = "https://github.com/control0forver";
            SharedData.MakeMessageBoxContent(url, "LGF - GitHub");
        }
        private void Run_LGF_gitee(object sender, MouseButtonEventArgs e)
        {
            var url = "https://gitee.com/lgf-studio";
            SharedData.MakeMessageBoxContent(url, "LGF - Gitee");
        }
        private void Run_n2n_github(object sender, MouseButtonEventArgs e)
        {
            var url = "https://github.com/ntop/n2n";
            SharedData.MakeMessageBoxContent(url, "n2n - GitHub");
        }
        private void Run_N2NGO_gitee(object sender, MouseButtonEventArgs e)
        {
            var url = "https://gitee.com/xue-jiangbin/n2nmc";
            SharedData.MakeMessageBoxContent(url, "N2N GO - Gitee");
        }
    }
}
