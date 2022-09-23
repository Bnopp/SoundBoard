using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SoundBoard_UI
{
    /// <summary>
    /// Interaction logic for SoundProperties.xaml
    /// </summary>
    public partial class SoundProperties : Window
    {
        private List<int> hotKeys = new List<int>();
        private int counter = 0;
        private bool keyListening = false;
        private bool txtChanged = false;
        private bool scutChanged = false;
        private string newFileName;

        public List<int> HotKeys { get { return hotKeys; } }
        public bool TxtChanged { get { return txtChanged; } }
        public bool ScutChanged { get { return scutChanged; } }
        public string NewFileName { get { return newFileName; } }

        public SoundProperties(string soundName, string shortcut)
        {
            InitializeComponent();
            tbSoundName.Text = soundName;
            lblShortcut.Content = shortcut;
            tbSoundName.SelectAll();
        }

        /// <summary>
        /// CloseButton_Clicked
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// TitleBar_MouseDown - Drag if single-click, resize if double-click
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void tbSoundName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!txtChanged) txtChanged = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (txtChanged) newFileName = tbSoundName.Text;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (keyListening)
            {
                Key key = e.Key;

                if (!HotKeys.Contains((int)key) && key != Key.Escape && counter <= 3)
                {
                    if (HotKeys.Count == 0) lblShortcut.Content = "";
                    HotKeys.Add((int)key);
                    string scut = "";
                    switch (key)
                    {
                        case Key.LeftCtrl:
                            scut = "LCtrl";
                            break;
                        case Key.RightCtrl:
                            scut = "RCtrl";
                            break;
                        case Key.LeftAlt:
                            scut = "LAlt";
                            break;
                        case Key.RightAlt:
                            scut = "RAlt";
                            break;
                        case Key.LeftShift:
                            scut = "LShift";
                            break;
                        case Key.RightShift:
                            scut = "RShift";
                            break;
                        default:
                            scut = key.ToString();
                            break;
                    }
                    lblShortcut.Content += scut;
                    counter++;
                    if (HotKeys.Count < 3) lblShortcut.Content += "+";
                }
                if (counter == 3)
                {
                    keyListening = false;
                    btnShortcut.Content = "Set Shortcut";
                    scutChanged = true;
                }
            }
            else if (e.Key == Key.Escape) this.Close();
        }

        private void tbSoundName_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()));
        }

        private void btnShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (!keyListening)
            {
                keyListening = true;
                (sender as Button).Content = "Awaiting Input";
            }
            else
            {
                keyListening = false;
                (sender as Button).Content = "Set Shortcut";
                if (hotKeys.Count != 3)
                {
                    scutChanged = false;
                    hotKeys.Clear();
                    lblShortcut.Content = "No Shortcut";
                }
            }
        }
    }
}
