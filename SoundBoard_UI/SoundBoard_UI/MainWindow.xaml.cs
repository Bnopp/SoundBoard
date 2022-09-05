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

        public MainWindow()
        {
            InitializeComponent();

            recorder = new AudioRecorder(5);
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            LoadAudioDevices();
        }

        void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            if (recorder.wBuffer == null) return;

            int len = recorder.wBuffer.FloatBuffer.Length / 8;

            // fft
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
                Rectangle rect = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(253, 133, 74)), Width = size, Height = Math.Abs(values[i].X) * (cVisualiser.ActualHeight / 2) * 10 * 2};
                rect.SetValue(Canvas.LeftProperty, Convert.ToDouble((i - 1) * size));
                rect.SetValue(Canvas.TopProperty, this.Height / 2);
                ScaleTransform stInvert = new ScaleTransform(1, -1);
                rect.RenderTransform = stInvert;
                cVisualiser.Children.Add(rect);
            }
        }

        public void LoadAudioDevices()
        {
            /*for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                var deviceInfo = WaveOut.GetCapabilities(deviceId);
                cbPlayback.Items.Add(deviceInfo.ProductName);
            }

            for (int deviceId = 0; deviceId < WaveIn.DeviceCount; deviceId++)
            {
                var deviceInfo = WaveIn.GetCapabilities(deviceId);
                cbRecording.Items.Add(deviceInfo.ProductName);
            }*/

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
        }

        public Dictionary<string, MMDevice> GetInputAudioDevices()
        {
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
                {
                    if (device.FriendlyName.StartsWith(deviceInfo.ProductName))
                    {
                        retVal.Add(device.FriendlyName, device);
                        break;
                    }
                }
            }

            return retVal;
        }

        public Dictionary<string, MMDevice> GetOutputAudioDevices()
        {
            Debug.WriteLine("method started");
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            int waveOutDevices = WaveOut.DeviceCount;
            for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                WaveOutCapabilities deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
                //Debug.WriteLine(deviceInfo.ProductName);
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All))
                {
                    if (device.FriendlyName.StartsWith(deviceInfo.ProductName))
                    {
                        retVal.Add(device.FriendlyName, device);
                        break;
                    }
                }
            }

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
