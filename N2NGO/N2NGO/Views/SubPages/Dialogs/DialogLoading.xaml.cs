using N2NGO.Utils;
using System.Windows;
using System.Windows.Controls;

namespace N2NGO.Views.SubPages.Dialogs;

public partial class DialogLoading : Page
{
    public DialogLoading(object? contentLabelContent = null, bool showActionButton = false, object? actionButtonContent = null, RoutedEventHandler? actionButtonClickEventHandler = null)
    {
        InitializeComponent();

        Root.InitializeWithCustomUI();

        LabelContent.Content = contentLabelContent;
        ButtonAction.Visibility = showActionButton ? Visibility.Visible : Visibility.Collapsed;
        ButtonAction.Content = actionButtonContent;
        if (actionButtonClickEventHandler is not null)
            ButtonAction.Click += actionButtonClickEventHandler;
    }
}
