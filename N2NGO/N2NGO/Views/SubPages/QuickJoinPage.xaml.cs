using N2NGO.Utils;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace N2NGO.Views.SubPages
{
    public partial class QuickJoinPage : Page
    {
        public QuickJoinPage()
        {
            InitializeComponent();

            OnIsPasswdNeededCheckboxClick(null, null);
        }

        private async void OnJoinButtonClick(object sender, RoutedEventArgs e)
        {
            DialogJoiningRoom.Opacity = 0;
            DialogJoiningRoom.Visibility = Visibility.Visible;

            var needPass = IsAuthenticationEnabledCheckbox.IsChecked == true;
            var roomCode = RoomConnectText.Text;
            var roomPass = RoomPasswordText.Text;

            TaskCompletionSource joiningCompletion = new();

            DoubleAnimation _loadingDialogFadeIn = new() { To = 1, Duration = TimeSpan.FromSeconds(0.20), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            _loadingDialogFadeIn.Completed += async (_, _) =>
            {
                await Globals.CurrentApp.JoinRoomAsync(needPass, roomCode, roomPass);
                joiningCompletion.SetResult();
            };
            Dispatcher.Invoke(() => DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeIn));

            await joiningCompletion.Task;

            DoubleAnimation _loadingDialogFadeOut = new() { To = 0, Duration = TimeSpan.FromSeconds(0.45), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            _loadingDialogFadeOut.Completed += (_, _) =>
            {
                DialogJoiningRoom.Visibility = Visibility.Collapsed;
            };
            Dispatcher.Invoke(() => DialogJoiningRoom.BeginAnimation(OpacityProperty, _loadingDialogFadeOut));
        }

        private void OnIsPasswdNeededCheckboxClick(object? sender, RoutedEventArgs? e)
        {
            if (IsAuthenticationEnabledCheckbox.IsChecked != null)
            {
                bool bNeedPassword = IsAuthenticationEnabledCheckbox.IsChecked.Value;
                GroupPasswordInput.IsEnabled = bNeedPassword;
                if (bNeedPassword)
                {
                    Globals.CurrentApp.MainWindow.DoMessageDialog("@LOCALE_DialogCreateRoomSecurity_Content", "@LOCALE_DialogCreateRoomSecurity_Title");
                }
            }
        }
    }
}
