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
using System.IO;

namespace trempature
{
    public partial class Form1 : Form
    {
        private bool _allowClose;
        private Font _iconFont; 
        private string _tempText = "NA";
        private string _lastStatus = ""; 
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly ManageLocationsDialog _manageLocationsDialog = new ManageLocationsDialog();
        private string _currentStationId;

        private Station CurrentStation
        {
            get
            {
                if (_currentStationId == null)
                {
                    return null; 
                }
                return _manageLocationsDialog.SelectedStations.FirstOrDefault(s => s.Id.Equals(_currentStationId));
            }
        }

        private string UserPrefsPath
        {
            get
            {
                return Path.Combine(Paths.UserAppDataDir, "prefs.xml");  
            }

        }

        public Form1()
        {
            InitializeComponent();

            _iconFont = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Regular); 
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RetrieveTemperatureAsync();

            // Need to update here as well so that the background
            // will turn red if we haven't been able to update 
            // for a while. 
            UpdateDisplay(); 
        }

        private void RetrieveTemperatureAsync()
        {
            ThreadPool.QueueUserWorkItem(RetrieveTemperature);
        }

        private void RetrieveTemperature(object state)
        {
            try
            {
                var doc = XDocument.Load(CurrentStation.XmlUrl);
                var tempValue = doc.Root.Element("temp_f").Value;
                float temp = float.Parse(tempValue);
                _tempText = ((int)(temp + 0.5F)).ToString();
                _lastUpdate = DateTime.Now;
                Invoke(new Action(UpdateDisplay)); 
            }
            catch (Exception)
            {
            }
        }

        private bool TempIsOutOfDate
        {
            get
            {
                return (DateTime.Now - _lastUpdate) > TimeSpan.FromMinutes(30);
            }
        }

        private string CurrentStatus
        {
            get
            {
                return string.Format("t:{0},ood:{1}", _tempText, TempIsOutOfDate); 
            }
        }

        private void UpdateDisplay()
        {
            if (_lastStatus != CurrentStatus)
            {
                notifyIcon1.Icon = GetIcon();
                _lastStatus = CurrentStatus; 
            }
            if (CurrentStation == null)
            {
                notifyIcon1.Text = "No current station";
            }
            else
            {
                notifyIcon1.Text = string.Format("{2}: {0}F. Last update {1}",
                    _tempText, _lastUpdate, _currentStationId);
            }
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

            if (TempIsOutOfDate)
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
            LoadPrefs();
            UpdateDisplay(); 
            PutToTray();
            AddStations(_manageLocationsDialog.SelectedStations); 
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

        private void manageLocationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentStations = new HashSet<Station>(_manageLocationsDialog.SelectedStations); 

            if (_manageLocationsDialog.ShowDialog() == DialogResult.OK)
            {
                var newStations = new HashSet<Station>(_manageLocationsDialog.SelectedStations);

                var stationsToRemove = currentStations.Except(newStations);
                var stationsToAdd = newStations.Except(currentStations);

                AddStations(stationsToAdd);

                foreach (var stationToRemove in stationsToRemove)
                {
                    locationToolStripMenuItem.DropDownItems.RemoveByKey(stationToRemove.Id); 
                }
            }
        }

        private void AddStations(IEnumerable<Station> stationsToAdd)
        {
            foreach (var stationToAdd in stationsToAdd)
            {
                var item = new ToolStripMenuItem
                {
                    Name = stationToAdd.Id,
                    Text = stationToAdd.Id,
                    Tag = stationToAdd,
                    //ToolTipText = stationToAdd.Name,  // Causes problems by eating the mouse click meant for the menu item itself
                    Checked = stationToAdd.Id.Equals(_currentStationId)
                };
                item.Click += new EventHandler(Station_Clicked);
                locationToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        void Station_Clicked(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var station = menuItem.Tag as Station;

            foreach (var item in locationToolStripMenuItem.DropDownItems)
            {
                var tsmi = item as ToolStripMenuItem; 
                if (tsmi != null)
                {
                    var s = tsmi.Tag as Station;

                    if (s != null)
                    {
                        if (s == station)
                        {
                            tsmi.Checked = true;
                            SetCurrentStation(s);
                        }
                        else
                        {
                            tsmi.Checked = false; 
                        }
                    }
                }
            }
        }

        private void SetCurrentStation(Station s)
        {
            _currentStationId = s.Id;
            _lastUpdate = DateTime.MinValue;
            RetrieveTemperatureAsync();
            SavePrefs(); 
        }

        private void SavePrefs()
        {
            var doc = new XDocument(
                new XElement("prefs",
                    new XAttribute("version", "1"),
                    new XElement("current-station", 
                        new XAttribute("id", CurrentStation.Id), 
                        new XAttribute("url", CurrentStation.XmlUrl)))); 

            doc.Save(UserPrefsPath); 
        }

        private void LoadPrefs()
        {
            if (File.Exists(UserPrefsPath))
            {
                var doc = XDocument.Load(UserPrefsPath);

                var cse = doc.Root.Element("current-station");

                var s = new Station
                {
                    Id = cse.Attribute("id").Value,
                    XmlUrl = cse.Attribute("url").Value
                };

                SetCurrentStation(s); 
            }
        }

    }
}
