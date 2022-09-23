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
using System.Xml.Linq;
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

        private FileSystemWatcher _watcher;
        private AudioRecorder recorder;
        private WaveOut waveOut;
        private WaveFileReader waveFileReader;
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
                Debug.WriteLine($"Created Directory: {saveDir}");
            }

            string[] Files = System.IO.Directory.GetFiles(saveDir);

            for (int i = 0; i < Files.Length; i++)
            {
                if (System.IO.Path.GetExtension(Files[i]) == ".wav")
                {
                    lsSounds.Add(new Sound() { Name = System.IO.Path.GetFileNameWithoutExtension(Files[i]), Shortcut = "No Shortcut", Path = System.IO.Path.GetFullPath(Files[i]) });
                    Debug.WriteLine($"Loaded sound: {System.IO.Path.GetFileNameWithoutExtension(Files[i])}");
                }
            }

            dgSounds.ItemsSource = lsSounds;
            dgSounds.CanUserAddRows = false;


            if (!File.Exists(_DefaultSettingspath + UserSettingsFilename))
            {
                this.Settings.SoundHotKeys = new List<ArrayList>();
                Debug.WriteLine($"Settings file non existant - Created new HotKeys list");
            }
            else
            {
                this.Settings = Settings.Read(_DefaultSettingspath + UserSettingsFilename);
                LoadSavedSettings();
                Debug.WriteLine("Loaded saved settings");
            }

            _watcher = new FileSystemWatcher(saveDir);
            _watcher.Created += OnCreate;
            _watcher.Deleted += OnDelete;
            _watcher.EnableRaisingEvents = true;

            recorder = new AudioRecorder(10);
            waveOut = new WaveOut();
            recorder.SavePath = saveDir;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            LoadAudioDevices();
            Debug.WriteLine("Loaded Audio Devices");

            HotkeyManager.Current.AddOrReplace("Starter", Key.D1, ModifierKeys.Control | ModifierKeys.Alt, StartOrStop);
        }
        #endregion

        /// <summary>
        /// Loads saved settings
        /// </summary>
        public void LoadSavedSettings()
        {
            foreach (ArrayList list in this.Settings.SoundHotKeys)
            {
                int index = lsSounds.FindIndex(a => a.Path == (string)list[0]);
                if (index != -1) 
                {
                    Debug.WriteLine($"Sound found at postiton {index}: {(string)list[0]}");
                    dgSounds.SelectedIndex = index;
                    CreateHotKey(new List<Key>() { (Key)(int)list[1], (Key)(int)list[2], (Key)(int)list[3] }, index, true);
                    Debug.WriteLine($"Created HotKey for {(string)list[0]} - {(Key)(int)list[1]}+{(Key)(int)list[2]}+{(Key)(int)list[3]}");
                    dgSounds.Items.Refresh();
                }
                else
                {
                    Debug.WriteLine($"Sound {(string)list[0]} not found, adding binded HotKey to remove list: {(Key)(int)list[1]}+{(Key)(int)list[2]}+{(Key)(int)list[3]}");
                    hotKeysToRemove.Add(list);
                }
            }
            
            if (hotKeysToRemove.Count > 0)
            {
                Debug.WriteLine("HotKeys to remove:");
                foreach (ArrayList list in hotKeysToRemove)
                {
                    Debug.WriteLine($"-     {(string)list[0]} - {(Key)(int)list[1]}+{(Key)(int)list[2]}+{(Key)(int)list[3]}");
                }
                RemoveOldHotKeys();
            }
            else
            {
                Debug.WriteLine("No keys to remove");
            }
        }

        /// <summary>
        /// It removes old hotkeys.
        /// </summary>
        public void RemoveOldHotKeys()
        {
            Debug.WriteLine("Attempting to remove HotKeys");
            if (hotKeysToRemove.Count > 0)
            {
                Debug.WriteLine("Removing HotKeys: ");
                foreach (ArrayList list in hotKeysToRemove)
                {
                    this.Settings.SoundHotKeys.Remove(list);
                    Debug.WriteLine($"-     {(string)list[0]} - {(Key)(int)list[1]}+{(Key)(int)list[2]}+{(Key)(int)list[3]}");
                }
                hotKeysToRemove.Clear();
            }
            else
            {
                Debug.WriteLine("No keys to remove");
            }
        }

        /// <summary>
        /// It loads all the audio devices on the computer into two combo boxes
        /// </summary>
        public void LoadAudioDevices()
        {
            

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

            Debug.WriteLine("Loaded Audio Devices");
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

        /// <summary>
        /// It checks if a hotkey exists.
        /// </summary>
        /// <param name="name">The name of the hotkey you want to check for.</param>
        public dynamic HotKeyExists(string name)
        {
            Debug.WriteLine($"Cheking if the HotKey for {name} already exists");

            foreach (ArrayList list in this.Settings.SoundHotKeys)
            {
                int index = lsSounds.FindIndex(a => a.Path == name);
                if (index != -1 && name == (string)list[0]) 
                {
                    Debug.WriteLine($"HotKey for {name} exists");
                    Debug.WriteLine($"HotKey for {index} exists");
                    Debug.WriteLine($"{(Key)list[3]}");
                    return list; 
                }
            }

            Debug.WriteLine($"HotKey for {name} doesn't exist");
            return false;
        }

        /// <summary>
        /// It creates a hotkey
        /// </summary>
        /// <param name="input">The list of keys that make up the hotkey.</param>
        /// <param name="index">The index of the hotkey in the list of hotkeys.</param>
        /// <param name="loadingSettings">This is a boolean value that tells the program whether or not
        /// it's loading settings from the settings file. If it is, then it will not add the hotkey to
        /// the list of hotkeys.</param>
        public void CreateHotKey(List<Key> input, int index, bool loadingSettings)
        {
            int count = 0;
            dgSounds.SelectedIndex = index;
            Sound tmpSound = dgSounds.SelectedItem as Sound;

            var checkExist = HotKeyExists(tmpSound.Path);
            if (!loadingSettings && checkExist is ArrayList) hotKeysToRemove.Add(checkExist);

            tmpSound.Shortcut = "";
            List<ModifierKeys> modifierKeys = new List<ModifierKeys>();
            Key normalKey = Key.None;
            foreach (Key key in input)
            {
                /* Checking if the key pressed is a modifier key (Ctrl, Alt, Shift) and if it is, it
                adds it to the list of modifier keys. If it is not a modifier key, it sets it to the
                normalKey variable. */
                switch (key)
                {
                    case Key.LeftCtrl :
                        tmpSound.Shortcut += "LCtrl";
                        modifierKeys.Add(ModifierKeys.Control);
                        break;
                    case Key.RightCtrl:
                        tmpSound.Shortcut += "RCtrl";
                        modifierKeys.Add(ModifierKeys.Control);
                        break;
                    case Key.LeftAlt:
                        tmpSound.Shortcut += "LAlt";
                        modifierKeys.Add(ModifierKeys.Alt);
                        break;
                    case Key.RightAlt:
                        tmpSound.Shortcut += "RAlt";
                        modifierKeys.Add(ModifierKeys.Alt);
                        break;
                    case Key.LeftShift:
                        tmpSound.Shortcut += "LShift";
                        modifierKeys.Add(ModifierKeys.Shift);
                        break;
                    case Key.RightShift:
                        tmpSound.Shortcut += "RShift";
                        modifierKeys.Add(ModifierKeys.Shift);
                        break;
                    default:
                        tmpSound.Shortcut += key.ToString();
                        normalKey = key;
                        break;
                }
                count++;
                if (count < input.Count) tmpSound.Shortcut += "+";
            }
            HotkeyManager.Current.AddOrReplace(tmpSound.Path, normalKey, modifierKeys[0] | modifierKeys[1], PlaySound);
            Debug.WriteLine("Created hotkey " + $"-     {tmpSound.Name} - {modifierKeys[0].ToString()}+{modifierKeys[1].ToString()}+{normalKey.ToString()}");
            if (!loadingSettings)
            {
                this.Settings.SoundHotKeys.Add(new ArrayList() { tmpSound.Path, modifierKeys[0], modifierKeys[1], normalKey });
                Debug.WriteLine("Added HotKey to Settings");
            }
            dgSounds.Items.Refresh();
        }

        /// <summary>
        /// It checks if the key is a modifier key.
        /// </summary>
        /// <param name="Key">The key to check</param>
        public bool isModifier(Key key)
        {
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt) 
            {
                Debug.WriteLine($"{key.ToString()} is modifier");
                return true;
            }

            return false;
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

        private void OnCreate(object sender, FileSystemEventArgs e)
        {
            string value = $"Created: {e.FullPath}";
            Debug.WriteLine(value);
            if (System.IO.Path.GetExtension(e.FullPath) == ".wav")
            {
                lsSounds.Add(new Sound() { Name = System.IO.Path.GetFileNameWithoutExtension(e.FullPath), Shortcut = "No Shortcut", Path = e.FullPath });
                dgSounds.Dispatcher.Invoke(() => { dgSounds.Items.Refresh(); });

                Debug.WriteLine($"Loaded sound: {System.IO.Path.GetFileNameWithoutExtension(e.FullPath)}");
            }
        }

        private void OnDelete(object sender, FileSystemEventArgs e)
        {
            lsSounds.Clear();

            string value = $"Deleted: {e.FullPath}";
            Debug.WriteLine(value);

            string[] Files = System.IO.Directory.GetFiles(saveDir);

            for (int i = 0; i < Files.Length; i++)
            {
                if (System.IO.Path.GetExtension(Files[i]) == ".wav")
                {
                    lsSounds.Add(new Sound() { Name = System.IO.Path.GetFileNameWithoutExtension(Files[i]), Shortcut = "No Shortcut", Path = System.IO.Path.GetFullPath(Files[i]) });
                    Debug.WriteLine($"Loaded sound: {System.IO.Path.GetFileNameWithoutExtension(Files[i])}");
                }
            }

            dgSounds.Dispatcher.Invoke(() => { dgSounds.Items.Refresh(); });

            Debug.WriteLine("Reloaded sounds");
        }

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
            if(waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
                waveFileReader.Dispose();
            }
            if (dgSounds.Items.Count > 0)
            {
                waveFileReader = new WaveFileReader(System.IO.Path.GetFullPath(lsSounds[dgSounds.SelectedIndex].Path));
                waveOut.DeviceNumber = cbPlayback.SelectedIndex;
                waveOut.Init(waveFileReader);
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
            lblSaveTime.Content = $"{(int)sTimeToSave.Value} sec";
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
        private void GridDouble_Click(object sender, MouseEventArgs e)
        {
            var grid = sender as DataGrid;

            if (grid.SelectedIndex != -1)
            {
                var rowIndex = grid.SelectedIndex;
                var tmpSound = (grid.SelectedItem as Sound);
                SoundProperties wSoundProperties = new SoundProperties(tmpSound.Name, tmpSound.Shortcut);
                wSoundProperties.ShowDialog();
                if (wSoundProperties.TxtChanged)
                {
                    string ext = System.IO.Path.GetExtension(tmpSound.Path);
                    string directory = System.IO.Path.GetDirectoryName(tmpSound.Path);
                    string newPath = directory + $@"\{wSoundProperties.NewFileName}" + ext;
                    if (waveOut.PlaybackState == PlaybackState.Playing) waveOut.Stop();
                    if (waveFileReader != null) waveFileReader.Dispose();
                    File.Move(tmpSound.Path, newPath);
                    foreach (ArrayList list in this.Settings.SoundHotKeys)
                    {
                        int index = lsSounds.FindIndex(a => a.Path == tmpSound.Path);
                        if (index != -1 && tmpSound.Path == (string)list[0])
                        {
                            list[0] = newPath;
                        }
                    }
                    tmpSound.Name = System.IO.Path.GetFileNameWithoutExtension(newPath);
                    tmpSound.Path = newPath;
                    grid.Items.Refresh();
                    Debug.WriteLine($"File renamed from {tmpSound.Name} to {System.IO.Path.GetFileNameWithoutExtension(newPath)}");
                }

                if (wSoundProperties.ScutChanged)
                {
                    if (isModifier(wSoundProperties.HotKeys[0]) && isModifier(wSoundProperties.HotKeys[1]) && !isModifier(wSoundProperties.HotKeys[2]))
                    {
                        CreateHotKey(wSoundProperties.HotKeys, rowIndex, false);
                        RemoveOldHotKeys();
                    }
                }
            }
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
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
                waveFileReader.Dispose();
            }
            if (dgSounds.Items.Count > 0)
            {
                waveFileReader = new WaveFileReader(System.IO.Path.GetFullPath(e.Name));
                waveOut.DeviceNumber = cbPlayback.SelectedIndex;
                waveOut.Init(waveFileReader);
                waveOut.Play();
                Debug.WriteLine("Playing " + e.Name);
            }
        }

        #endregion
    }
}
