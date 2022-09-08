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
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SoundBoard_UI
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AudioRecorder recorder;

        private int M = 7;

        /* The constructor of the MainWindow class. */
        public MainWindow()
        {
            InitializeComponent();

            recorder = new AudioRecorder(5);
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            LoadAudioDevices();
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
        void CompositionTarget_Rendering(object sender, System.EventArgs e)
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
                Rectangle rect = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(253, 133, 74)), Width = size, Height = Math.Abs(values[i].X) * (cVisualiser.ActualHeight / 2) * 5, RadiusY = 5, RadiusX = 5};
                rect.SetValue(Canvas.LeftProperty, Convert.ToDouble((i - 1) * size));
                rect.SetValue(Canvas.TopProperty, cVisualiser.Height);
                ScaleTransform stInvert = new ScaleTransform(1, -1);
                rect.RenderTransform = stInvert;
                cVisualiser.Children.Add(rect);
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
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Minimized Button_Clicked
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnRecordingStart_Click(object sender, RoutedEventArgs e)
        {
            recorder.StartRecording();
        }

        private void btnRecordingStop_Click(object sender, RoutedEventArgs e)
        {
            recorder.StopRecording();
        }

        private void btnRecordingSave_Click(object sender, RoutedEventArgs e)
        {
            recorder.Save("test.wav");
        }
    }
}
