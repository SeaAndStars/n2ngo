using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace N2NGO.Views
{
    /// <summary>
    /// TerminalWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalWindow :Window
    {
        private bool _canClose = false;

        public ObservableCollection<string> TerminalBuffer { get; set; } = new();
        public string TerminalBufferString
        {
            get
            {
                StringBuilder stringBuilder = new();
                foreach (string s in TerminalBuffer)
                    stringBuilder.AppendLine(s);
                return stringBuilder.ToString();
            }
        }

        public TerminalWindow()
        {
            InitializeComponent();
        }

        public new void Close()
        {
            _canClose = true;
            base.Close();
        }

        private void WindowTerminal_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_canClose)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }

    public class TerminalWindowTextWriter : TextWriter
    {
        private TerminalWindow _data;
        private int _cursorX = 0;
        private int _cursorY = 0;
        public TerminalWindow Data => _data;
        public int CursorX => _cursorX;
        public int CursorY => _cursorY;

        public TerminalWindowTextWriter(TerminalWindow terminalWindow)
        {
            _data = terminalWindow;
        }

        public override void Write(char value)
        {
            while (_data.TerminalBuffer.Count <= CursorY)
                _data.TerminalBuffer.Add(string.Empty);

            switch (value)
            {
                default:
                    var line = new ObservableCollection<char>(_data.TerminalBuffer[_cursorY].ToCharArray());
                    line.Insert(_cursorX, value);
                    _data.TerminalBuffer[_cursorY] = new string(line.ToArray());
                    _cursorX++;
                    break;

                case '\n':
                    _cursorY++;
                    _cursorX = 0;
                    break;

                case '\r':
                    _cursorX = 0;
                    break;
            }

            _data.TerminalView.Text = _data.TerminalBufferString;
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
