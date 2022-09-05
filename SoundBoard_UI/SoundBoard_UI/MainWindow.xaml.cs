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
        public static Random rand = new Random();

        private WaveBuffer buffer;

        private int M = 6;

        public MainWindow()
        {
            InitializeComponent();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            //LoadAudioDevices();

            // start audio capture
            var capture = new WasapiLoopbackCapture();

            capture.DataAvailable += DataAvailable;

            capture.RecordingStopped += (s, a) =>
            {
                capture.Dispose();
            };

            capture.StartRecording();
        }

        public void DataAvailable(object sender, WaveInEventArgs e)
        {
            buffer = new WaveBuffer(e.Buffer); // save the buffer in the class variable
        }

        void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            if (buffer == null)
            {
                Debug.WriteLine("No buffer available");
                return;
            }

            testCanvas.Children.Clear();

            /*double newLeft = rand.Next(0, 640);
            double newTop = rand.Next(0, 480);

            theEllipse.SetValue(Canvas.LeftProperty, newLeft);
            theEllipse.SetValue(Canvas.TopProperty, newTop);*/

            int len = buffer.FloatBuffer.Length / 8;

            // fft
            NAudio.Dsp.Complex[] values = new NAudio.Dsp.Complex[len];
            for (int i = 0; i < len; i++)
            {
                values[i].Y = 0;
                values[i].X = buffer.FloatBuffer[i];
            }
            NAudio.Dsp.FastFourierTransform.FFT(true, M, values);

            float size = (float)this.Width / ((float)Math.Pow(2, M) / 2);

            for (int i = 1; i < Math.Pow(2, M) / 2; i++)
            {
                //Graphics.Print(i.ToString() + ": " + values[i].X.ToString("N2") + " i " + (values[i].Y + 0.50f).ToString("N2"), 0, (i + 1) * 16);
                //Graphics.Rectangle(DrawMode.Fill, (i - 1) * size, WindowHeight / 2, size, -Math.Abs(values[i].X) * (WindowHeight / 2) * 10);

                Debug.WriteLine(Math.Abs(values[i].X) * (this.Height / 2) * 10);
                Rectangle rect = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(253, 133, 74)), Width = size, Height = Math.Abs(values[i].X) * (this.Height / 2) * 10 };
                rect.SetValue(Canvas.LeftProperty, Convert.ToDouble((i - 1) * size));
                rect.SetValue(Canvas.TopProperty, this.Height / 2);
                ScaleTransform stInvert = new ScaleTransform(1, -1);
                rect.RenderTransform = stInvert;
                testCanvas.Children.Add(rect);
            }

            float size = (float)this.Width / ((float)Math.Pow(2, M) / 2);

            for (int i = 1; i < Math.Pow(2, M) / 2; i++)
            {
                //Graphics.Print(i.ToString() + ": " + values[i].X.ToString("N2") + " i " + (values[i].Y + 0.50f).ToString("N2"), 0, (i + 1) * 16);
                //Graphics.Rectangle(DrawMode.Fill, (i - 1) * size, WindowHeight / 2, size, -Math.Abs(values[i].X) * (WindowHeight / 2) * 10);

                Debug.WriteLine(Math.Abs(values[i].X) * (this.Height / 2) * 10);
                Rectangle rect = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(253, 133, 74)), Width = size, Height = Math.Abs(values[i].X) * (this.Height / 2) * 10 };
                rect.SetValue(Canvas.LeftProperty, Convert.ToDouble((i - 1) * size));
                rect.SetValue(Canvas.TopProperty, this.Height / 2);
                ScaleTransform stInvert = new ScaleTransform(1, -1);
                rect.RenderTransform = stInvert;
                testCanvas.Children.Add(rect);
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
    }
}
