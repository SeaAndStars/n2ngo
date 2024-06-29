using N2NGO.UtilsClass;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace N2NGO.Views.SubPages.Dialogs
{
    /// <summary>
    /// DialogRoomInfo.xaml 的交互逻辑
    /// </summary>
    public partial class DialogRoomInfo : Page
    {
        public DialogRoomInfo(RoomsPageRoomCardModel card, RoutedEventHandler funcClick, RoutedEventHandler cancelClick)
        {
            InitializeComponent();
            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>(grid));
            SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<TextBox>(grid));
            SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>(grid));

            GroupPassword.Visibility = card.IsRoomPasswordNeeded ? Visibility.Visible : Visibility.Collapsed;

            this.DataContext = card;

            FuncButton.Click += funcClick;
            ButtonCancel.Click += cancelClick;
        }
    }
}
