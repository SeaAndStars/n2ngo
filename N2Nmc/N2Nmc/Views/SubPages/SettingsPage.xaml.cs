using HandyControl.Controls;
using Microsoft.Win32;
using N2Nmc.UtilsClass;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// Page6.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            DisableAnimationTB.IsChecked = SharedData.configFile?.Get("DisableAnimation", "0") != "0";
        }

        private void DisableAnimationTB_Checked(object sender, RoutedEventArgs e)
        {
            MainView.EnableAnimation = false;

            SharedData.configFile?.Set("DisableAnimation", false ? "0" : "1");
        }

        private void DisableAnimationTB_Unchecked(object sender, RoutedEventArgs e)
        {
            MainView.EnableAnimation = true;

            SharedData.configFile?.Set("DisableAnimation", true ? "0" : "1");
        }

        //private void SelectBackgroundImageButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog();
        //    dialog.Filter = ".jpg|*.jpg|.png|*.png|.jpeg|*.jpeg|*|*.*";
        //    if (dialog.ShowDialog(App.Current.MainWindow) == false) return;
        //    BitmapImage tempImage = new BitmapImage();
        //    tempImage.BeginInit();
        //    tempImage.UriSource = new Uri(dialog.FileName, UriKind.RelativeOrAbsolute);
        //    tempImage.EndInit();

        //    ((MainView)App.Current.MainWindow).ImageBackgroundImage.Source = tempImage;
        //}

        private async void InstallTapButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Process.Start( "Data/TapWindowsInstaller/9.21.2.exe","/S /X" ).WaitForExitAsync();
            }
            catch(Exception ex) 
            {
                Growl.Error("在尝试安装Tap驱动时发生异常：" + ex.Message+ "\n请尝试以管理员身份运行N2Nmc或\n手动安装该文件：Data/TapWindowsInstaller/9.21.2.exe");
                return;
            }
            Growl.Success("Tap驱动已完成安装");
        }
    }
}
