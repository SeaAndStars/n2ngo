using N2NGO.Utils;
using System.Windows.Controls;

namespace N2NGO.Views.IndexPages;

public partial class Index_ClientingPage : BaseIndexPage
{
    public Index_ClientingPage()
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

            case "QuickJoin":
                {
                    NavigatePage(Globals.CurrentApp.MainWindow.PageQuickJoin);
                    break;
                }
        }
    }
}
