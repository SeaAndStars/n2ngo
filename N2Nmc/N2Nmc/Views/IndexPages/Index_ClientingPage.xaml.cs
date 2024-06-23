using N2Nmc.UtilsClass;
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

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// Index_ClientingPage.xaml 的交互逻辑
    /// </summary>
    public partial class Index_ClientingPage : BaseIndexPage
    {
        public Index_ClientingPage()
        {
            InitializeComponent();
        }
        
        protected override void TempGrid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Click
            var indexer = (Indexer)((Border)sender).DataContext;

            switch (indexer.nagivKey)
            {
                default: break;

                case "goback":
                    {
                        NavigateIndexPage(null);
                        break;
                    }

                case "QuickJoin":
                    {
                        NavigatePage(SharedData.GetMainView.pageQuickJoin);
                        break;
                    }
            }
        }
    }
}
