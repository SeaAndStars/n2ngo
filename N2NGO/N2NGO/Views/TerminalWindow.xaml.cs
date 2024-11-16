using N2NGO.Utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace N2NGO.Views
{
    /// <summary>
    /// TerminalWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalWindow : Window
    {
        private bool _canClose = false;

        public TerminalWindowTextWriter? RefTerminalWindowTextWriter { get; set; } = null;

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

        private void WindowTerminal_Closing(object sender, CancelEventArgs e)
        {
            if (!_canClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void WindowTerminal_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Terminal Font Size Scaler
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    this.TerminalView.FontSize++;
                }

                if (e.Delta < 0)
                {
                    this.TerminalView.FontSize--;
                }

                e.Handled = true;
            }
        }

        private void ButtonClearBuffer_Click(object sender, RoutedEventArgs e)
        {
            if (RefTerminalWindowTextWriter is null)
            {
                Globals.CurrentApp.Log.WriteLine("Cannot clear buffer, RefTerminalWindowTextWriter is null", Globals.CurrentApp.Log.Module.TerminalWindow);
                return;
            }
            RefTerminalWindowTextWriter.Clear();
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

        public Action? WriteCallBack { get; set; } = null;

        public TerminalWindowTextWriter(TerminalWindow terminalWindow)
        {
            _data = terminalWindow;
            _data.RefTerminalWindowTextWriter = this;
        }

        public void Clear()
        {
            _data.TerminalBuffer.Clear();
            _cursorX = 0;
            _cursorY = 0;
            _data.TerminalView.Text = "";
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

            _data.TerminalView.Dispatcher.InvokeAsync(() =>
            {
                try { _data.TerminalView.Text = _data.TerminalBufferString; }
                catch { return; }   // Multi-threaded operation conflicts

                if (_data.CheckBoxScrollToEnd.IsChecked.HasValue && _data.CheckBoxScrollToEnd.IsChecked.Value)
                    _data.TerminalView.ScrollToEnd();
            });
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
