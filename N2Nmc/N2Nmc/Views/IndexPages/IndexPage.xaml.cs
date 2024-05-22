using HandyControl.Controls;
using HandyControl.Data;
using N2Nmc.UtilsClass;
using N2Nmc.Views.SubPages.Dialogs;
using N2Nmc_Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static N2Nmc.UtilsClass.SharedData;
using System.Windows.Media;
using System.Windows.Threading;
using static N2Nmc_Protocol.Protocol;
using System.Windows.Media.Animation;
using System.Reflection;
using System.Windows.Markup;

namespace N2Nmc.Views.SubPages
{
    /// <summary>
    /// IndexPage.xaml 的交互逻辑
    /// </summary>
    public partial class IndexPage : BaseIndexPage
    {
        Index_CommunityPage communityPage = new Index_CommunityPage();
        Index_HostingPage hostingPage = new Index_HostingPage();
        Index_ClientingPage clientingPage = new Index_ClientingPage();
        Index_AdvancedPage advancedPage = new Index_AdvancedPage();
        Index_AboutPage aboutPage = new Index_AboutPage();

        public IndexPage()
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

                case "community":
                    {
                        NavigateIndexPage(communityPage);
                        break;
                    }

                case "hosting":
                    {
                        NavigateIndexPage(hostingPage);
                        break;
                    }

                case "clienting":
                    {
                        NavigateIndexPage(clientingPage);
                        break;
                    }

                case "advanced":
                    {
                        NavigateIndexPage(advancedPage);
                        break;
                    }

                case "about":
                    {
                        NavigateIndexPage(aboutPage);
                        break;
                    }
            }
        }
    }

}
