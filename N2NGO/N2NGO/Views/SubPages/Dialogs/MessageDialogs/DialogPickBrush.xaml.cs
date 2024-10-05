using System.Windows.Controls;
using System.Windows.Media;

namespace N2NGO.Views.SubPages.Dialogs.MessageDialogs
{
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
                if (!int.TryParse(ColorAlphaInput.Text, out var a))
                    a = defaultA;
                if (!int.TryParse(ColorRedInput.Text, out var r))
                    r = defaultR;
                if (!int.TryParse(ColorGreenInput.Text, out var g))
                    g = defaultG;
                if (!int.TryParse(ColorBlueInput.Text, out var b))
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
