using N2NGO.Utils;
using System.Windows;
using System.Windows.Controls;

namespace N2NGO.Views.SubPages.Dialogs;

public partial class DialogRoomInfo : Page
{
    public DialogRoomInfo(RoomsPageRoomCardModel card, RoutedEventHandler funcClick, RoutedEventHandler cancelClick)
    {
        InitializeComponent();
        CustomUI.InitButtons(CustomUIHelpers.FindVisualChildren<Button>(grid));
        CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<TextBox>(grid));
        CustomUI.InitCards(CustomUIHelpers.FindVisualChildren<Label>(grid));

        GroupPassword.Visibility = card.IsRoomPasswordNeeded ? Visibility.Visible : Visibility.Collapsed;

        this.DataContext = card;

        FuncButton.Click += funcClick;
        ButtonCancel.Click += cancelClick;
    }
}
