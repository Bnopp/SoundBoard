using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace SoundBoard_UI
{
    /// <summary>
    /// Logique d'interaction pour SelectHotKeyWindow.xaml
    /// </summary>
    public partial class SelectHotKeyWindow : Window
    {
        public List<Key> HotKeys = new List<Key>();
        public List<Char> HotKeysChar = new List<Char>();
        private int counter = 0;

        public SelectHotKeyWindow()
        {
            InitializeComponent();
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key;
            if (!HotKeys.Contains(key) && key != Key.Enter && counter <=3) 
            {
                counter++;
                HotKeys.Add(key);
            }
            if (counter == 3 || key == Key.Enter) this.Close();
            
        }

        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            Char keyChar = (Char)System.Text.Encoding.ASCII.GetBytes(e.Text)[0];
            
            if (!HotKeysChar.Contains(keyChar) && counter <= 3)
            {
                HotKeysChar.Add(keyChar);
            }
            Debug.WriteLine(keyChar);
        }
    }
}
