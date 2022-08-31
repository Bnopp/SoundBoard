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
        private AudioRecorder _recorder;

        public Form1()
        {
            InitializeComponent();

            _recorder = new AudioRecorder(5);

            _btnStartRecording = new Button { Size = new Size(100, 100), Text = "Start", Location = new Point(0, 0)};
            _btnStopRecording = new Button { Size = _btnStartRecording.Size, Text = "Stop", Location = new Point(_btnStartRecording.Left + _btnStartRecording.Width + 5, 0)};
            _btnReplay = new Button { Size = new Size(100, 100), Text = "Replay", Location = new Point(_btnStopRecording.Left + _btnStopRecording.Width + 5, 0)};
            _btnSave = new Button { Size = new Size(100, 100), Text = "Save", Location = new Point(_btnReplay.Left + _btnReplay.Width + 5, 0)};

            _btnStartRecording.Click += new EventHandler(BtnStartRecording);
            _btnStopRecording.Click += new EventHandler(BtnStopRecording);
            _btnReplay.Click += new EventHandler(BtnReplay);
            _btnSave.Click += new EventHandler(BtnSave);

            this.Controls.AddRange(new Control[] { _btnStartRecording, _btnStopRecording, _btnReplay, _btnSave});

            for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                var capabilities = WaveOut.GetCapabilities(deviceId);
                Console.WriteLine(capabilities.ProductName + " " + deviceId);
            }
        }

        public void BtnStartRecording(object sender, EventArgs e)
        {
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
