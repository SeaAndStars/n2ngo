using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

namespace N2NGO.Views.SubPages.Dialogs.MessageDialogs
{
    /// <summary>
    /// DialogPickBrush.xaml 的交互逻辑
    /// </summary>
    public partial class DialogPickBrush : Page
    {
        public DialogPickBrush()
        {
            InitializeComponent();
        }

        public SolidColorBrush GetColorBrush(byte defaultA = 0xFF, byte defaultR = 0xFF, byte defaultG = 0xFF, byte defaultB = 0xFF)
        {
            try
            {
                int a, r, g, b;
                if (!int.TryParse(ColorAlphaInput.Text, out a))
                    a = defaultA;
                if (!int.TryParse(ColorRedInput.Text, out r))
                    r = defaultR;
                if (!int.TryParse(ColorGreenInput.Text, out g))
                    g = defaultG;
                if (!int.TryParse(ColorBlueInput.Text, out b))
                    b = defaultB;
                return new(Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
            }
            catch
            {
                return new(Color.FromArgb((byte)defaultA, (byte)defaultR, (byte)defaultG, (byte)defaultB));
            }
        }
        private void ColorInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            PreviewColor.DataContext = GetColorBrush();
        }
    }
}
