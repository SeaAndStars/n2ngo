using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace N2Nmc.Views.SubPages.Dialogs
{
    /// <summary>
    /// DialogRoomInfo.xaml 的交互逻辑
    /// </summary>
    public partial class DialogRoomInfo : Page
    {
        Card? card = null;

        public DialogRoomInfo(Card card, RoutedEventHandler funcClick, RoutedEventHandler cancelClick)
        {
            InitializeComponent();

            this.card = card;

            RenderTransform = new SkewTransform(10, 3);

            GroupPassword.Visibility = card.IsRoomPasswordNeeded ? Visibility.Visible : Visibility.Collapsed;

            RoomTitle.Content = card.RoomName;
            RoomInfo.Text = "房间号：" + card.RoomCode + '\n' +
                               "隐藏房间：" + (!card.IsRoomVisible ? "是" : "否") + '\n' +
                               "房间密码：" + card.IsRoomPasswordNeededText + '\n';

            FuncButton.Content = card.BtnText;
            FuncButton.Click += funcClick;

            ButtonCancel.Click += cancelClick;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RenderTransform.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            RenderTransform.BeginAnimation(SkewTransform.AngleYProperty, new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.3), BeginTime = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        }
    }
}
