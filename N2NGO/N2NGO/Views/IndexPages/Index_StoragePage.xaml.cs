using N2NGO.UtilsClass;
using N2NGO_Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace N2NGO.Views.SubPages
{
    /// <summary>
    /// Index_AdvancedPage.xaml 的交互逻辑
    /// </summary>
    public partial class Index_StoragePage : BaseIndexPage
    {
        public Index_StoragePage()
        {
            InitializeComponent();
        }

        private async void RefreshAll()
        {
            long ProgramFiles;
            long DataFiles;
            long N2NGODataFiles;

            await Task.Run(async () =>
            {
                ProgramFiles = SharedData.GetDirectorySize(Environment.CurrentDirectory) ?? 0;
                DataFiles = SharedData.GetDirectorySize(UserDef.N2NGO_Base_AppData_Path) ?? 0;
                N2NGODataFiles = SharedData.GetDirectorySize(UserDef.N2NGO_N2NGO_AppData_Path) ?? 0;

                long Total = ProgramFiles + N2NGODataFiles;
                long OtherData = DataFiles - N2NGODataFiles;

                string TotalReadable = SharedData.GetFileSizeReadableString(Total);
                string OtherDataFilesReadable = SharedData.GetFileSizeReadableString(OtherData);
                string N2NGOProgramFilesReadable = SharedData.GetFileSizeReadableString(ProgramFiles);
                string N2NGODataFilesReadable = SharedData.GetFileSizeReadableString(N2NGODataFiles);

                await StorageTotalUsage.Dispatcher.InvokeAsync(() => StorageTotalUsage.Text = TotalReadable);
                await StorageProgramFilesUsage.Dispatcher.InvokeAsync(() => StorageProgramFilesUsage.Text = $"{N2NGOProgramFilesReadable}\n{Path.GetFullPath(Environment.CurrentDirectory)}");
                await StorageDataFilesUsage.Dispatcher.InvokeAsync(() => StorageDataFilesUsage.Text = $"{N2NGODataFilesReadable}\n{Path.GetFullPath(UserDef.N2NGO_N2NGO_AppData_Path)}");
                await StorageOtherDataFilesUsage.Dispatcher.InvokeAsync(() => StorageOtherDataFilesUsage.Text = $"{OtherDataFilesReadable}\n{Path.GetFullPath(UserDef.N2NGO_Base_AppData_Path)}");
            });
        }

        protected override void Page_Navigated()
        {
            base.Page_Navigated();
            RefreshAll();
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

                case "RefreshStorageAll":
                    {
                        RefreshAll();
                        break;
                    }
            }
        }
    }
}
