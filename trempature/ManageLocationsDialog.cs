using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;

namespace trempature
{
    public partial class ManageLocationsDialog : Form
    {
        private readonly List<Station> _availableStations = new List<Station>();
        private readonly BindingList<Station> _selectedStations = new BindingList<Station>();

        public IEnumerable<Station> SelectedStations
        {
            get { return _selectedStations; }
        }

        private string StationDataPath
        {
            get
            {
                return Path.Combine(Paths.UserAppDataDir, "stations.xml"); 
            }
        }

        private string StationPrefsDataPath
        {
            get
            {
                return Path.Combine(Paths.UserAppDataDir, "station-prefs.xml");
            }
        }

        public ManageLocationsDialog()
        {
            InitializeComponent();

            LoadStations();

            LoadStationPrefs(); 

            availableStationsListBox.DataSource = _availableStations;
            availableStationsListBox.DisplayMember = "DisplayName";
            availableStationsListBox.ValueMember = "XmlUrl";

            selectedStationsListBox.DataSource = _selectedStations;
            selectedStationsListBox.DisplayMember = "DisplayName";
            selectedStationsListBox.ValueMember = "XmlUrl"; 

            var states = new HashSet<string>(_availableStations.Select(s => s.State)); 

            displayStateComboBox.Items.Add("Show all");
            displayStateComboBox.Items.AddRange(states.OrderBy(x => x).ToArray());
            displayStateComboBox.SelectedIndex = 0; 
        }

        private void LoadStationPrefs()
        {
            Paths.EnsureUserAppDataDir();

            if (!File.Exists(StationPrefsDataPath))
            {
                var doc = XDocument.Parse("<station-prefs />");
                doc.Save(StationPrefsDataPath); 
            }

            var prefsDoc = XDocument.Load(StationPrefsDataPath);

            var selectedStations =
                prefsDoc
                .Root
                .Elements("selected-station")
                .Select(e => new Station
                {
                    Name = e.Attribute("name").Value,
                    State = e.Attribute("state").Value,
                    XmlUrl = e.Attribute("url").Value,
                    Id = e.Attribute("id").Value
                });

            _selectedStations.Clear();

            foreach (var selectedStation in selectedStations)
            {
                _selectedStations.Add(selectedStation);
            }          

        }

        private void LoadStations()
        {
            Paths.EnsureUserAppDataDir();

            if (!File.Exists(StationDataPath))
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("trempature.Stations.xml"))
                {
                    using (var reader = XmlReader.Create(stream))
                    {
                        XDocument doc = XDocument.Load(reader);
                        doc.Save(StationDataPath); 
                    }
                }
            }

            var stationsDoc = XDocument.Load(StationDataPath);

            var stations =
                stationsDoc
                .Root
                .Elements("station")
                .Select(e => new Station
                {
                    Name = e.Element("station_name").Value,
                    State = e.Element("state").Value,
                    XmlUrl = e.Element("xml_url").Value,
                    Id = e.Element("station_id").Value
                });

            _availableStations.Clear();
            _availableStations.AddRange(stations);
        }


        private void displayStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var val = displayStateComboBox.SelectedItem;
            if (val.Equals("Show all"))
            {
                availableStationsListBox.DataSource = _availableStations;
            }
            else
            {
                availableStationsListBox.DataSource = _availableStations.Where(s => s.State.Equals(val)).ToList(); 
            }
        }

        private void bAdd_Click(object sender, EventArgs e)
        {
            foreach (var item in availableStationsListBox.SelectedItems)
            {
                _selectedStations.Add(item as Station); 
            }
        }

        private void bRemove_Click(object sender, EventArgs e)
        {
            var itemsToRemove = new List<Station>(); 
            foreach (var item in selectedStationsListBox.SelectedItems)
            {
                itemsToRemove.Add(item as Station); 
            }

            foreach (var item in itemsToRemove)
            {
                _selectedStations.Remove(item);
            }
        }

        private void ManageLocationsDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                var doc = new XDocument(
                    new XElement("station-prefs",
                        _selectedStations.Select(s =>
                            new XElement("selected-station",
                                new XAttribute("name", s.Name),
                                new XAttribute("id", s.Id),
                                new XAttribute("url", s.XmlUrl),
                                new XAttribute("state", s.State)))));

                doc.Save(StationPrefsDataPath);
            }
            else
            {
                LoadStationPrefs(); 
            }
        }
    }
}
