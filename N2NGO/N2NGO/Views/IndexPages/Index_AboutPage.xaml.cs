using N2NGO.UtilsClass;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// Index_AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class Index_AboutPage : BaseIndexPage
    {
        public Index_AboutPage()
        {
            InitializeComponent();

            AddReferences();
        }

        private void AddReferences()
        {
            Indexers.Items.Add(new Indexer
            {
                IndexerTitle = "N2N GO",
                IndexerDescription = string.Format("版本 {0}\n构建日期 {1}\n链接 {2}\nRuntime {3}\n.NET {4}",
                SharedData.VersionString,
                "6/28/2024",
                "https://gitee.com/xue-jiangbin/n2nmc-private",
                (Environment.Is64BitProcess ? "64-bit" : "32-bit"),
                RuntimeEnvironment.GetRuntimeDirectory()
                ),
                NagivKey = "N2NGO",
                IndexerCBI = new BitmapImage(new("/Data/Images/N2N GO Preview.png", UriKind.Relative))
            });

            Indexers.Items.Add(new Indexer
            {
                IndexerTitle = "N2N GO Core",
                IndexerDescription = string.Format("版本 {0}\n构建日期 {1}\n链接 {2}\nRuntime {3}\n.NET {4}",
                SharedData.Version,
                "6/28/2024",
                "https://gitee.com/xue-jiangbin/n2nmc-private",
                (Environment.Is64BitProcess ? "64-bit" : "32-bit"),
                RuntimeEnvironment.GetRuntimeDirectory()
                )
            });

            Indexers.Items.Add(new Indexer
            {
                IndexerTitle = "n2n",
                IndexerDescription = string.Format("版本 {0}\n构建日期 {1}\n链接 {2}",
                    "v.3.1.1-71-g9618512-dirty-r1255 x64_static for Windows", "27/Apr/2024 20:44:00", "https://github.com/ntop/n2n"
                    )
            });

            Indexers.Items.Add(new Indexer
            {
                IndexerTitle = "Nuget",
                IndexerDescription =
                "HandyControl (3.5.1)\n" +
                "MagicEffects (1.0.0)\n" +
                "SharpVectors (1.8.4)\n" +
                "Walterlv.Themes.FluentDesign (7.9.0)"
            });
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

                case "N2NGO":
                    {
                        NavigatePage(SharedData.CurrentApp.MainView.PageInfo);
                        break;
                    }
            }
        }
    }
}
