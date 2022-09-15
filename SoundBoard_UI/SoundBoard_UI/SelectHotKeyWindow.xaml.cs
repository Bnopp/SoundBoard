using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

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

        /// <summary>
        /// Called whenever a key is pressed and saves it
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key;

            if (!HotKeys.Contains(key) && key != Key.Enter && counter <= 3)
            {
                if (HotKeys.Count == 0) tbMessage.Text = "";
                counter++;
                HotKeys.Add(key);
                tbMessage.Text += key.ToString();
                if (HotKeys.Count < 3) tbMessage.Text += "+";
            }
            if (counter == 3 || key == Key.Enter) this.Close();

        }
    }
}
