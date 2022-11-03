using Microsoft.Toolkit.Uwp.Notifications;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
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
using System.Deployment.Application;
using System.Xml.Linq;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI.Xaml.Controls;
using Canvas = System.Windows.Controls.Canvas;
using SoundBoard_UI.Properties;
using Windows.Media.Core;
using System.Drawing;
using System.Windows.Media.Imaging;
using Rectangle = System.Windows.Shapes.Rectangle;
using Color = System.Drawing.Color;
using NAudio.Utils;
using System.Threading.Tasks;

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

        private StreamWriter _sw;
        private StreamReader _sr;
        private FileSystemWatcher _watcher;
        private AudioRecorder recorder;
        private WaveOut waveOut;
        private WaveFileReader waveFileReader;
        private WaveFormRenderer waveFormRenderer;
        private  WaveFormRendererSettings waveStandardSettings;
        private string saveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Soundboard\Sounds";

        private int M = 7;
        #endregion

        #region Constructor
        /* The constructor of the MainWindow class. */
        public MainWindow()
        {
            InitializeComponent();

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                this.tbTitle.Text += ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            else
            {
                var version = Assembly.GetEntryAssembly().GetName().Version;
                this.tbTitle.Text += $" DevBuild v{version.Major}.{version.Minor}.{version.Build}.{version.Revision} preAlpha";
            }

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

            recorder = new AudioRecorder(sTimeToSave.Value);
            waveOut = new WaveOut();
            recorder.SavePath = saveDir;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            LoadAudioDevices();
            Debug.WriteLine("Loaded Audio Devices");

            if (!File.Exists(saveDir + @"\preventFileDelete.tmp"))
            {
                _sw = File.CreateText(saveDir + @"\preventFileDelete.tmp");
                _sw.Dispose();
            }
            File.SetAttributes(saveDir + @"\preventFileDelete.tmp", FileAttributes.Hidden);
            _sr = File.OpenText(saveDir + @"\preventFileDelete.tmp");

            if (dgSounds.SelectedIndex != -1)
                dgSounds.SelectedIndex = 0;
        }
        #endregion

        #region Methods

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

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

                    CreateHotKey(new List<Key>() {convertToKey((ModifierKeys)list[1]), convertToKey((ModifierKeys)list[2]), (Key)list[3] }, index, true);
                    Debug.WriteLine($"Created HotKey for {(string)list[0]} - {(ModifierKeys)(int)list[1]}+{(ModifierKeys)(int)list[2]}+{(Key)(int)list[3]}");
                    dgSounds.Items.Refresh();
                }
                else
                {
                    Debug.WriteLine($"Sound {(string)list[0]} not found, adding binded HotKey to remove list: {(ModifierKeys)(int)list[1]}+{(ModifierKeys)(int)list[2]}+{(Key)(int)list[3]}");
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
        public void CreateHotKey(List<Key> keys, int index, bool loadingSettings)
        {
            dgSounds.SelectedIndex = index;
            Sound tmpSound = dgSounds.SelectedItem as Sound;

            var checkExist = HotKeyExists(tmpSound.Path);
            if (!loadingSettings && checkExist is ArrayList) hotKeysToRemove.Add(checkExist);

            tmpSound.Shortcut = $"{convertToModifier(keys[0])}+{convertToModifier(keys[1])}+{keys[2]}";

            HotkeyManager.Current.AddOrReplace(tmpSound.Path, keys[2], convertToModifier(keys[0]) | convertToModifier(keys[1]), PlaySound);
            Debug.WriteLine("Created hotkey " + $"-     {tmpSound.Name} - {convertToModifier(keys[0])}+{convertToModifier(keys[1])}+{keys[2]}");
            
            if (!loadingSettings)
            {
                this.Settings.SoundHotKeys.Add(new ArrayList() { tmpSound.Path, convertToModifier(keys[0]), convertToModifier(keys[1]), keys[2] });
                Debug.WriteLine("Added HotKey to Settings");
            }
            dgSounds.Items.Refresh();
        }

        /// <summary>
        /// It checks if the key is a modifier key.
        /// </summary>
        /// <param name="Key">The key to check</param>
        public bool isModifier(int key)
        {
            Debug.WriteLine((Key)key);
            if ((Key)key == Key.LeftCtrl || (Key)key == Key.RightCtrl ||
                (Key)key == Key.LeftShift || (Key)key == Key.RightShift ||
                (Key)key == Key.LeftAlt || (Key)key == Key.RightAlt) 
            {
                Debug.WriteLine($"{key.ToString()} is modifier");
                return true;
            }

            return false;
        }

        public ModifierKeys convertToModifier(Key nKey)
        {
            if (nKey == Key.LeftCtrl || nKey == Key.RightCtrl)
            {
                return ModifierKeys.Control;
            }
            else if(nKey == Key.LeftShift || nKey == Key.RightShift)
            {
                return ModifierKeys.Shift;
            }
            else if(nKey == Key.LeftAlt || nKey == Key.RightAlt)
            {
                return ModifierKeys.Alt;
            }
            return ModifierKeys.None;
        }

        public Key convertToKey(ModifierKeys nKey)
        {
            if (nKey == ModifierKeys.Control)
            {
                return Key.LeftCtrl;
            }
            else if (nKey == ModifierKeys.Shift)
            {
                return Key.LeftShift;
            }
            else if (nKey == ModifierKeys.Alt)
            {
                return Key.LeftAlt;
            }
            return Key.None;
        }


        private void GetPlayTime(WaveOut wo, WaveFileReader wfr, double skip)
        {
            Debug.WriteLine(wfr.TotalTime);
            Debug.WriteLine(skip);

            var playtime = wo.GetPositionTimeSpan().TotalMilliseconds / wfr.TotalTime.TotalMilliseconds * 100;
            while (wo.PlaybackState == PlaybackState.Playing && Math.Round(playtime,0) <= 100)
            {
                playtime = wo.GetPositionTimeSpan().TotalMilliseconds / wfr.TotalTime.TotalMilliseconds * 100;
                UpdatePlayerTime(playtime + skip);
            }

            UpdatePlayerTime(skip);
                
        }

        private void UpdatePlayerTime(double time)
        {
            Action action = () => sPlayer.Value = time;
            Dispatcher.Invoke(action);
        }

        #endregion

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
            _sr.Close();
            _sr.Dispose();
            File.Delete(saveDir + @"\preventFileDelete.tmp");
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
                // Calculate new position
                var skipTime = sPlayer.Value / 100 * waveFileReader.TotalTime.Seconds;
                var skipPlayer = sPlayer.Value;
                Debug.WriteLine($"skiptime {skipPlayer}");
                long newPos = waveFileReader.Position + (long)(waveFileReader.WaveFormat.AverageBytesPerSecond * skipTime);
                // Force it to align to a block boundary
                if ((newPos % waveFileReader.WaveFormat.BlockAlign) != 0)
                    newPos -= newPos % waveFileReader.WaveFormat.BlockAlign;
                // Force new position into valid range
                newPos = Math.Max(0, Math.Min(waveFileReader.Length, newPos));
                // set position
                waveFileReader.Position = newPos;
                waveOut.Init(waveFileReader);
                waveOut.Play();
                Task.Run(() => GetPlayTime(waveOut, waveFileReader, skipPlayer));
                Debug.WriteLine("Playing " + dgSounds.SelectedIndex);
            }
        }

        /// <summary>
        /// Changes recording time depending on the slider value
        /// </summary>
        private void sTimeToSave_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (recorder != null) recorder.ChangeBufferSize((int)sTimeToSave.Value);
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
                    Debug.WriteLine($"Checking if is modifier: {(Key)wSoundProperties.HotKeys[0]} + {(Key)wSoundProperties.HotKeys[1]} + {(Key)wSoundProperties.HotKeys[2]}");
                    if (isModifier(wSoundProperties.HotKeys[0]) && isModifier(wSoundProperties.HotKeys[1]) && !isModifier(wSoundProperties.HotKeys[2]))
                    {
                        CreateHotKey(new List<Key>() { (Key)wSoundProperties.HotKeys[0], (Key)wSoundProperties.HotKeys[1], (Key)wSoundProperties.HotKeys[2] }, rowIndex, false);
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
                Rectangle rect = new Rectangle { Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(253, 133, 74)), Width = size, Height = Math.Abs(values[i].X) * (cVisualiser.ActualHeight / 2) * 5, RadiusY = 5, RadiusX = 5 };
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

                Task.Run(() => GetPlayTime(waveOut, waveFileReader, 0));

                Debug.WriteLine("Playing " + e.Name);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                if (dgSounds.SelectedIndex != -1)
                {
                    if (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        waveOut.Stop();
                        waveFileReader.Dispose();
                    }
                    File.Delete((dgSounds.SelectedItem as Sound).Path);
                }
            }
        }

        #endregion

        private void dgSounds_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dgSounds.SelectedIndex != -1)
            {
                waveFormRenderer = new WaveFormRenderer();
                var topSpacerColor = Color.FromArgb(64, 83, 22, 3);
                SoundCloudBlockWaveFormSettings sc = new SoundCloudBlockWaveFormSettings(Color.FromArgb(196, 255, 5, 255), topSpacerColor, Color.FromArgb(196, 255, 96, 44),
                    Color.FromArgb(64, 255, 5, 255))
                {
                    Name = "SoundCloud Orange Transparent Blocks",
                    PixelsPerPeak = 5,
                    SpacerPixels = 1,
                    TopSpacerGradientStartColor = topSpacerColor,
                    BackgroundColor = Color.White
                };
                var settings = (WaveFormRendererSettings)sc;
                settings.TopHeight = 50;
                settings.BottomHeight = 15;
                settings.Width = 800;
                settings.DecibelScale = false;
                using (var waveStream = new AudioFileReader(System.IO.Path.GetFullPath(lsSounds[dgSounds.SelectedIndex].Path)))
                {
                    System.Drawing.Image img = waveFormRenderer.Render(waveStream, settings);
                    imgWave.Source = BitmapToImageSource(new Bitmap(img));
                }

                sPlayer.Value = 0;
            }
        }
    }
}
