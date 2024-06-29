using N2NGO.UtilsClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// Index_HostingPage.xaml 的交互逻辑
    /// </summary>
    public partial class Index_HostingPage : BaseIndexPage
    {
        public Index_HostingPage()
        {
            InitializeComponent();
        }
        
        protected override void TempGrid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
                        NavigatePage(SharedData.CurrentApp.MainView.PageRooming);
                        break;
                    }
            }
        }
    }
}
