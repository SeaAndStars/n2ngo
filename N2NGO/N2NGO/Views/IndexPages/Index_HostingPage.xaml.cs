using N2NGO.Utils;
using System.Windows.Controls;

namespace N2NGO.Views.IndexPages;

public partial class Index_HostingPage : BaseIndexPage
{
    public Index_HostingPage()
    {
        InitializeComponent();
    }
    
    protected override void Indexer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Click
        var indexer = (Indexer)((Border)sender).DataContext;

        switch (indexer.NagivKey)
        {
            default: break;

            case "goback":
                {
                    NavigateIndexPage(null);
                    break;
                }

            case "Rooming":
                {
                    NavigatePage(Globals.CurrentApp.MainWindow.PageRooming);
                    break;
                }
        }
    }
}
