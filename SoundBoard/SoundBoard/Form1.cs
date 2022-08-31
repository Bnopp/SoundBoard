using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoundBoard
{
    public partial class Form1 : Form
    {
        private Button _btnStartRecording;
        private Button _btnStopRecording;
        private Button _btnReplay;
        private Button _btnSave;
        private Label _lblInputDevice;
        private Label _lblOutputDevice;
        public ComboBox _cmbInputDevice;
        public ComboBox _cmbOutputDevice;
        private AudioRecorder _recorder;

        public Form1()
        {
            InitializeComponent();

            _recorder = new AudioRecorder(5);

            _lblInputDevice = new Label { Text = "Input Device", Location = new Point(50, 25) };
            _lblOutputDevice = new Label { Text = "Output Device", Location = new Point(50, _lblInputDevice.Top + _lblInputDevice.Height + 10) };
            _cmbInputDevice = new ComboBox { Width = 250, Location = new Point(_lblInputDevice.Left + _lblInputDevice.Width + 10, _lblInputDevice.Height) };
            _cmbOutputDevice = new ComboBox { Width = 250, Location = new Point(_cmbInputDevice.Left, _lblOutputDevice.Top) };
            _btnStartRecording = new Button { Size = new Size(50, 50), Text = "Start", Location = new Point(50, 100)};
            _btnStopRecording = new Button { Size = _btnStartRecording.Size, Text = "Stop", Location = new Point(_btnStartRecording.Left + _btnStartRecording.Width + 5, _btnStartRecording.Top)};
            _btnReplay = new Button { Size = _btnStartRecording.Size, Text = "Replay", Location = new Point(_btnStopRecording.Left + _btnStopRecording.Width + 5, _btnStartRecording.Top) };
            _btnSave = new Button { Size = _btnStartRecording.Size, Text = "Save", Location = new Point(_btnReplay.Left + _btnReplay.Width + 5, _btnStartRecording.Top) };

            _btnStartRecording.Click += new EventHandler(BtnStartRecording);
            _btnStopRecording.Click += new EventHandler(BtnStopRecording);
            _btnReplay.Click += new EventHandler(BtnReplay);
            _btnSave.Click += new EventHandler(BtnSave);

            this.Controls.AddRange(new Control[] { _lblInputDevice, _lblOutputDevice, _cmbInputDevice, _cmbOutputDevice, _btnStartRecording, _btnStopRecording, _btnReplay, _btnSave});

            LoadAudioDevices();
        }

        private void LoadAudioDevices()
        {
            for (int deviceId = 0; deviceId < WaveIn.DeviceCount; deviceId++)
            {
                var deviceInfo = WaveIn.GetCapabilities(deviceId);
                _cmbInputDevice.Items.Add(deviceInfo.ProductName);
            }

            for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                var deviceInfo = WaveOut.GetCapabilities(deviceId);
                _cmbOutputDevice.Items.Add(deviceInfo.ProductName);
            }
        }

        public void BtnStartRecording(object sender, EventArgs e)
        {
            _recorder.InputDeviceNb = _cmbInputDevice.SelectedIndex;
            _recorder.OutputDeviceNb = _cmbOutputDevice.SelectedIndex;
            _recorder.StartRecording();
        }

        public void BtnStopRecording(object sender, EventArgs e)
        {
            _recorder.StopRecording();
        }

        public void BtnReplay(object sender, EventArgs e)
        {
            _recorder.PlayRecorded();
        }

        public void BtnSave(object sender, EventArgs e)
        {
            _recorder.Save("test.wav");
        }
    }
}
