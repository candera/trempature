using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace trempature
{
    public class Station
    {
        public string XmlUrl { get; set; }
        public string State { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }

        public string DisplayName
        {
            get
            {
                return string.Format("{0}: {1}", Id, Name); 
            }
        }
    }
}
