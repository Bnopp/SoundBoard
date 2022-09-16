using Microsoft.Toolkit.Uwp.Notifications;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI.Xaml.Controls;
using Canvas = System.Windows.Controls.Canvas;

namespace SoundBoard_UI
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        public const string UserSettingsFilename = @"\settings.xml";
        public string _DefaultSettingspath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Soundboard";

        public Settings Settings { get; private set; }

        public List<Sound> lsSounds;
        public List<ArrayList> hotKeysToRemove = new List<ArrayList>();

        private AudioRecorder recorder;
        private string saveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Soundboard\Sounds";

        private int M = 7;
        #endregion

        #region Constructor
        /* The constructor of the MainWindow class. */
        public MainWindow()
        {
            InitializeComponent();

            // if default settings exist
            this.Settings = new Settings();

            lsSounds = new List<Sound>();

            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            string[] Files = System.IO.Directory.GetFiles(saveDir);

            for (int i = 0; i < Files.Length; i++)
            {
                lsSounds.Add(new Sound() { Name = System.IO.Path.GetFileNameWithoutExtension(Files[i]), Shortcut = "none", Path = System.IO.Path.GetFullPath(Files[i]) });
            }

            dgSounds.ItemsSource = lsSounds;
            dgSounds.CanUserAddRows = false;


            if (!File.Exists(_DefaultSettingspath + UserSettingsFilename))
            {
                this.Settings.SoundHotKeys = new List<ArrayList>();
            }
            else
            {
                this.Settings = Settings.Read(_DefaultSettingspath + UserSettingsFilename);
                LoadSavedSettings();
            }

            recorder = new AudioRecorder(10);
            recorder.SavePath = saveDir;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            LoadAudioDevices();

            HotkeyManager.Current.AddOrReplace("Starter", Key.D1, ModifierKeys.Control | ModifierKeys.Alt, StartOrStop);
        }
        #endregion

        public void LoadSavedSettings()
        {
            foreach (ArrayList list in this.Settings.SoundHotKeys)
            {
                int index = lsSounds.FindIndex(a => a.Name == (string)list[0]);
                Debug.WriteLine("index found " + index);
                Debug.WriteLine((string)list[0]);
                if (index != -1) 
                {
                    dgSounds.SelectedIndex = index;
                    CreateHotKey(new List<Key>() { (Key)(int)list[1], (Key)(int)list[2], (Key)(int)list[3] }, index, true);
                    dgSounds.Items.Refresh();
                }
                else
                {
                    hotKeysToRemove.Add(list);
                }
            }
            foreach (ArrayList list in hotKeysToRemove)
            {
                Debug.WriteLine("Remove" + (string)list[0]);
            }
            RemoveOldHotKeys(hotKeysToRemove);
        }

        public void RemoveOldHotKeys(List<ArrayList> hotkeys)
        {
            foreach (ArrayList list in hotkeys)
            {
                Debug.WriteLine(this.Settings.SoundHotKeys.Remove(list));
            }
        }

        /// <summary>
        /// It loads all the audio devices on the computer into two combo boxes
        /// </summary>
        public void LoadAudioDevices()
        {
            var watch = Stopwatch.StartNew();

            foreach (KeyValuePair<string, MMDevice> device in GetInputAudioDevices())
            {
                //Debug.WriteLine("Input Name: {0}, State: {1}", device.Key, device.Value.State);
                cbRecording.Items.Add(device.Key);
            }

            foreach (KeyValuePair<string, MMDevice> device in GetOutputAudioDevices())
            {
                //Debug.WriteLine("Output Name: {0}, State: {1}", device.Key, device.Value.State);
                cbPlayback.Items.Add(device.Key);
            }

            if (cbRecording.Items.Count > 0) cbRecording.SelectedIndex = 0;
            if (cbPlayback.Items.Count > 0) cbPlayback.SelectedIndex = 0;

            watch.Stop();
            Debug.WriteLine("Loaded Audio Devices");
            Debug.WriteLine($"Execution time: {watch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// It returns a dictionary of all the active input audio devices on the system
        /// </summary>
        /// <returns>
        /// A dictionary of strings and MMDevices.
        /// </returns>
        public Dictionary<string, MMDevice> GetInputAudioDevices()
        {
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            //cycle through all audio devices
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                MMDevice temp = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)[i];
                retVal.Add(temp.FriendlyName, temp);
            }
            //clean up
            enumerator.Dispose();
            return retVal;
        }

        /// <summary>
        /// It returns a dictionary of all the output audio devices on the system
        /// </summary>
        /// <returns>
        /// A Dictionary of MMDevice objects.
        /// </returns>
        public Dictionary<string, MMDevice> GetOutputAudioDevices()
        {
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            //cyckle trough all audio devices
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                MMDevice temp = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[i];
                retVal.Add(temp.FriendlyName, temp);
            }

            //clean up
            enumerator.Dispose();
            return retVal;
        }

        private dynamic HotKeyExists(string name)
        {
            foreach (ArrayList list in this.Settings.SoundHotKeys)
            {
                int index = lsSounds.FindIndex(a => a.Name == name);
                if (index != -1) return list;
            }
            return false;
        }

        private void CreateHotKey(List<Key> input, int index, bool loadingSettings)
        {
            int count = 0;
            dgSounds.SelectedIndex = index;
            Sound tmpSound = dgSounds.SelectedItem as Sound;
            var checkExist = HotKeyExists(tmpSound.Name);
            if (loadingSettings && checkExist is ArrayList) hotKeysToRemove.Add(checkExist);
            tmpSound.Shortcut = "";
            List<Key> modifierKeys = new List<Key>();
            Key normalKey = Key.None;
            foreach (Key key in input)
            {
                /* Checking if the key pressed is a modifier key (Ctrl, Alt, Shift) and if it is, it
                adds it to the list of modifier keys. If it is not a modifier key, it sets it to the
                normalKey variable. */
                switch (key)
                {
                    case Key.LeftCtrl:
                        tmpSound.Shortcut += "LCtrl";
                        modifierKeys.Add(key);
                        break;
                    case Key.RightCtrl:
                        tmpSound.Shortcut += "RCtrl";
                        modifierKeys.Add(key);
                        break;
                    case Key.LeftAlt:
                        tmpSound.Shortcut += "LAlt";
                        modifierKeys.Add(key);
                        break;
                    case Key.RightAlt:
                        tmpSound.Shortcut += "RAlt";
                        modifierKeys.Add(key);
                        break;
                    case Key.LeftShift:
                        tmpSound.Shortcut += "LShift";
                        modifierKeys.Add(key);
                        break;
                    case Key.RightShift:
                        tmpSound.Shortcut += "RShift";
                        modifierKeys.Add(key);
                        break;
                    default:
                        tmpSound.Shortcut += key.ToString();
                        normalKey = key;
                        break;
                }
                count++;
                if (count < input.Count) tmpSound.Shortcut += "+";
            }

            HotkeyManager.Current.AddOrReplace(tmpSound.Name, normalKey, (ModifierKeys)modifierKeys[0] | (ModifierKeys)modifierKeys[1], PlaySound);
            if (!loadingSettings) this.Settings.SoundHotKeys.Add(new ArrayList() { tmpSound.Name, modifierKeys[0], modifierKeys[1], normalKey});

            dgSounds.Items.Refresh();
        }

        #region Window Control
        /// <summary>
        /// TitleBar_MouseDown - Drag if single-click, resize if double-click
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) Application.Current.MainWindow.DragMove();
        }

        /// <summary>
        /// CloseButton_Clicked
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            recorder.StopRecording();
            this.Settings.Save(_DefaultSettingspath + UserSettingsFilename);
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Minimized Button_Clicked
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        #endregion

        #region Events

        /// <summary>
        /// Start Recording Button_Clicked
        /// </summary>
        private void btnRecordingStart_Click(object sender, RoutedEventArgs e)
        {
            if (!recorder.IsRecording)
            {
                recorder.StartRecording();
                btnRecordingStart.Content = "Stop";
            }
            else
            {
                recorder.StopRecording();
                btnRecordingStart.Content = "Start";
            }
        }

        /// <summary>
        /// Save Button_Clicked
        /// </summary>
        private void btnRecordingSave_Click(object sender, RoutedEventArgs e)
        {
            recorder.Save();
        }

        /// <summary>
        /// Play Button_Clicked
        /// </summary>
        private void btnSoundPlay_Click(object sender, RoutedEventArgs e)
        {
            if (dgSounds.Items.Count > 0)
            {
                var reader = new WaveFileReader(System.IO.Path.GetFullPath(lsSounds[dgSounds.SelectedIndex].Path));
                var waveOut = new WaveOut();
                waveOut.DeviceNumber = cbPlayback.SelectedIndex;
                waveOut.Init(reader);
                waveOut.Play();
                Debug.WriteLine("Playing " + dgSounds.SelectedIndex);
            }
        }

        /// <summary>
        /// Changes recording time depending on the slider value
        /// </summary>
        private void sTimeToSave_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (recorder != null) recorder.RecordTime = (int)sTimeToSave.Value;
        }

        /// <summary>
        /// Sets the columns settings for the data grid after the columns are auto generated
        /// </summary>
        private void dgSounds_AutoGeneratedColumns(object sender, EventArgs e)
        {
            dgSounds.Columns[0].Width = 208;
            dgSounds.Columns[1].Width = 150;
            dgSounds.Columns.Remove(dgSounds.Columns[2]);
        }

        /// <summary>
        /// DataGrid Cell_DoubleMouseClick
        /// </summary>
        private void CellDouble_Click(object sender, MouseEventArgs e)
        {
            var grid = sender as DataGrid;
            var cellIndex = grid.SelectedIndex;

            SelectHotKeyWindow hotKeyWindow = new SelectHotKeyWindow();
            hotKeyWindow.ShowDialog();

            CreateHotKey(hotKeyWindow.HotKeys, cellIndex, false);
        }

        /// <summary>
        /// Start/Stop recording using a HotKey Shortcut
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="HotkeyEventArgs"></param>
        private void StartOrStop(object Sender, HotkeyEventArgs e)
        {
            string action = "";
            if (!recorder.IsRecording)
            {
                recorder.StartRecording();
                action = "Started";
            }
            else
            {
                recorder.StopRecording();
                action = "Stopped";
            }

            new ToastContentBuilder()
                .AddText("Soundboard Recording " + action)
                .AddButton(new ToastButton()
                .SetContent("Ok")
                .SetBackgroundActivation())
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(2);
                });
        }

        /// <summary>
        /// It takes the audio data from the microphone, converts it to a complex number, performs a
        /// Fast Fourier Transform on it, and then draws a rectangle for each frequency
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments</param>
        /// <returns>
        /// The FFT returns a complex number.
        /// </returns>
        private void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            if (recorder.wBuffer == null) return;

            int len = recorder.wBuffer.FloatBuffer.Length / 8;

            NAudio.Dsp.Complex[] values = new NAudio.Dsp.Complex[len];
            for (int i = 0; i < len; i++)
            {
                values[i].Y = 0;
                values[i].X = recorder.wBuffer.FloatBuffer[i];
            }
            NAudio.Dsp.FastFourierTransform.FFT(true, M, values);

            float size = (float)cVisualiser.ActualWidth / ((float)Math.Pow(2, M) / 2);

            cVisualiser.Children.Clear();

            for (int i = 1; i < Math.Pow(2, M) / 2; i++)
            {
                Rectangle rect = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(253, 133, 74)), Width = size, Height = Math.Abs(values[i].X) * (cVisualiser.ActualHeight / 2) * 5, RadiusY = 5, RadiusX = 5 };
                rect.SetValue(Canvas.LeftProperty, Convert.ToDouble((i - 1) * size));
                rect.SetValue(Canvas.TopProperty, cVisualiser.Height);
                ScaleTransform stInvert = new ScaleTransform(1, -1);
                rect.RenderTransform = stInvert;
                cVisualiser.Children.Add(rect);
            }
        }

        /// <summary>
        /// Plays the sound assigned to the shortcut
        /// </summary>
        private void PlaySound(object Sender, HotkeyEventArgs e)
        {
            if (dgSounds.Items.Count > 0)
            {
                var reader = new WaveFileReader(System.IO.Path.GetFullPath(lsSounds[Convert.ToInt32(e.Name)].Path));
                var waveOut = new WaveOut();
                waveOut.DeviceNumber = cbPlayback.SelectedIndex;
                waveOut.Init(reader);
                waveOut.Play();
                Debug.WriteLine("Playing " + dgSounds.SelectedIndex);
            }
        }

        #endregion
    }
}
