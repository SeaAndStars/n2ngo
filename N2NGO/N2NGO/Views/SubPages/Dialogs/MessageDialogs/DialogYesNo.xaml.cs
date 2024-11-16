using System.Windows.Controls;

namespace N2NGO.Views.SubPages.Dialogs.MessageDialogs;

public partial class DialogYesNo : Page
{
    public enum YesNoE
    {
        No,Yes
    }

    public YesNoE YesNo = YesNoE.No;

    public DialogYesNo()
    {
        InitializeComponent();
    }
}
