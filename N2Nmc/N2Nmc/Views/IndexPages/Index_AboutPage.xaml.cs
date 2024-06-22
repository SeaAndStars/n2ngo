using N2Nmc.UtilsClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Index_AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class Index_AboutPage : BaseIndexPage
    {
        public Index_AboutPage()
        {
            InitializeComponent();

            Indexers.Items.Add(new Indexer { IndexerTitle = "N2N GO(应用程序)", 
                IndexerDescription=string.Format("版本 {0}\n构建日期 {1}\n链接 {2}\nRuntime {3}\n.NET {4}", 
                SharedData.VersionString,
                "4/28/2024",
                "https://gitee.com/xue-jiangbin/n2nmc-private",
                (Environment.Is64BitProcess?"64-bit": "32-bit"),
                RuntimeEnvironment.GetRuntimeDirectory()
                ), nagivKey="N2NGO" });
            Indexers.Items.Add(new Indexer
            { IndexerTitle = "n2n(库)", 
                IndexerDescription=string.Format("版本 {0}\n构建日期 {1}\n链接 {2}",
                    "v.3.1.1-71-g9618512-dirty-r1255 x64_static for Windows", "27/Apr/2024 20:44:00", "https://github.com/ntop/n2n"
                    ) });
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

                case "N2NGO":
                    {
                        NavigatePage(SharedData.GetMainView.pageInfo);
                        break;
                    }
            }
        }
    }
}
