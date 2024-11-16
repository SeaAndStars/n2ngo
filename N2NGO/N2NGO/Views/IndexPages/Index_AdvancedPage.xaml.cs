using N2NGO.Utils;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace N2NGO.Views.IndexPages;

public partial class Index_AdvancedPage : BaseIndexPage
{
    public Index_AdvancedPage()
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

            case "Settings":
                {
                    NavigatePage(Globals.CurrentApp.MainWindow.PageSettings);
                    break;
                }

            case "Storage":
                {
                    NavigateIndexPage(Globals.CurrentApp.MainWindow.PageIndex.StorageIndexPage);
                    break;
                }

            case "CheckUpdate":
                {
                    Task.Run(() => Globals.CurrentApp.CheckN2NGOClientUpdate());
                    break;
                }
        }
    }
}
