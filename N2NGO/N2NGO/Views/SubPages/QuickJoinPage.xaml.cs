using N2NGO.UtilsClass;
using N2NGO.Views.SubPages.Dialogs;
using N2NGO.Views.SubPages.Dialogs.MessageDialogs;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// QuickJoinPage.xaml 的交互逻辑
    /// </summary>
    public partial class QuickJoinPage : Page
    {
        public QuickJoinPage()
        {
            InitializeComponent();

            IsPasswdNeeded_Click(null, null);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogJoiningRoom.Opacity = 0;
            DialogJoiningRoom.Visibility = Visibility.Visible;
            TaskCompletionSource<object> animationCompletedTask = new();
            DoubleAnimation _loadingDialogFadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };
            Dispatcher.Invoke(() => DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

            var needPass = IsPasswdNeeded.IsChecked == true;
            var roomCode = RoomConnectText.Text;
            var roomPass = RoomPasswordText.Text;
            await Task.Run(() =>
            {
                animationCompletedTask.Task.Wait();

                SharedData.CurrentApp.JoinRoomAsync(needPass, roomCode, roomPass);
            });

            TaskCompletionSource<object> animationCompletedTask1 = new();
            DoubleAnimation _loadingDialogFadeOut = new() { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            _loadingDialogFadeOut.Completed += (s, _) =>
            {
                animationCompletedTask1.SetResult(0);
            };
            DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeOut);

            await Task.Run(() =>
            {
                animationCompletedTask1.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    DialogJoiningRoom.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void IsPasswdNeeded_Click(object? sender, RoutedEventArgs? e)
        {
            if (IsPasswdNeeded.IsChecked != null)
            {
                bool bNeedPassword = IsPasswdNeeded.IsChecked.Value;
                GroupPasswordInput.IsEnabled = bNeedPassword;
                if (bNeedPassword)
                {
                    SharedData.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogCreateRoomSecurity_Content", "@LOCALE_DialogCreateRoomSecurity_Title");
                }
            }
        }
    }
}
