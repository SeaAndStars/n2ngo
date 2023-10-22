using HandyControl.Data;
using N2Nmc.UtilsClass;
using N2Nmc_Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static N2Nmc_Protocol.Protocol;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// DefaultPage.xaml 的交互逻辑
    /// </summary>
    public partial class DefaultPage : Page
    {
        public DefaultPage()
        {
            InitializeComponent();

            LabelVersion.Content += SharedData.versionString;
            NewsOfflineBox.Text = File.ReadAllText("Data/NewsOffline.txt");
        }

        private async void ButtonUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await Task.Run(()=>SharedData.CheckN2NClientUpdate());
        }

        private void LabelVersion_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}
