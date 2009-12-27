using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Xml.Linq;

namespace trempature
{
    public partial class Form1 : Form
    {
        private bool _allowClose;
        private Font _iconFont; 
        private string _tempText = "XX";
        private DateTime _lastUpdate = DateTime.MinValue; 

        public Form1()
        {
            InitializeComponent();

            _iconFont = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Regular); 
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RetrieveTemperatureAsync();
        }

        private void RetrieveTemperatureAsync()
        {
            ThreadPool.QueueUserWorkItem(RetrieveTemperature);
        }

        private void RetrieveTemperature(object state)
        {
            // TODO: Fix this so we don't hardcode, but rather let the user select
            // locations from http://www.weather.gov/xml/current_obs/index.xml
            try
            {
                var doc = XDocument.Load("http://www.weather.gov/xml/current_obs/KMSP.xml");
                var tempValue = doc.Root.Element("temp_f").Value;
                float temp = float.Parse(tempValue);
                _tempText = ((int)(temp + 0.5F)).ToString();
                _lastUpdate = DateTime.Now;
                Invoke(new Action(UpdateIcon)); 
            }
            catch (Exception)
            {
            }
        }

        private void UpdateIcon()
        {
            notifyIcon1.Icon = GetIcon();
        }

        private void DrawFilledRegion(Graphics g, Pen pen, Brush brush, int x, int y, int width, int height)
        {
            g.FillRectangle(brush, x, y, width, height);
            g.DrawRectangle(pen, x, y, width, height);
        }
        private Icon GetIcon()
        {
            Bitmap bitmap = new Bitmap(32, 32);
            Graphics g = Graphics.FromImage(bitmap);

            g.Clear(Color.Transparent);
            g.DrawRectangle(Pens.Black, new Rectangle(1, 1, 30, 30));

            Brush background;

            if ((DateTime.Now - _lastUpdate) > TimeSpan.FromMinutes(30))
            {
                background = Brushes.DarkRed;
            }
            else
            {
                background = Brushes.Black; 
            }

            g.FillRectangle(background, new Rectangle(2, 2, 28, 28));

            var centerPoint = GetCenterPoint(g, _tempText, _iconFont, 32, 32); 

            g.DrawString(_tempText, _iconFont, Brushes.Yellow, centerPoint); 

            //DrawFilledRegion(g, Pens.Black, Brushes.Yellow, 13, 13, 8, 8);

            return Icon.FromHandle(bitmap.GetHicon());
        }

        private PointF GetCenterPoint(Graphics g, string s, Font font, int width, int height)
        {
            var size = g.MeasureString(s, font);

            return new PointF((width / 2) - (size.Width / 2) + 0.5F, (height / 2) - (size.Height / 2) + 0.5F); 
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_allowClose)
            {
                PutToTray();
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
        private void PutToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }
        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateIcon(); 
            PutToTray();
            RetrieveTemperatureAsync(); 
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RestoreFromTray(); 
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            Close(); 
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            RestoreFromTray(); 
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                PutToTray();
            }
        }

    }
}
