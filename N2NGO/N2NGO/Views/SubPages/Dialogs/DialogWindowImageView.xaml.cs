using System.Windows;
using System.Windows.Media;

namespace N2NGO.Views.SubPages.Dialogs;

public partial class DialogImageView : Window
{
    public DialogImageView(ImageSource s, string name = "Unknown")
    {
        InitializeComponent();

        ImageBox.Source = s;
        Title += " - " + name;
    }
}
