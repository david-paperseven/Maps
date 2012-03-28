using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;

namespace Maps.Helpers
{
    public class PlaqueInfo
    {
        public enum FilterCategory
        {
            Arts,
            Politics,
            Science,
            Exploration
        }

        public PlaqueInfo(List<string> s)
        {
            number = Convert.ToInt32(s[0]);
            title = s[1];
            forenames = s[2];
            surname = s[3];
            date = s[4];
            address1 = s[5];
            address2 = s[6];
            latitude = Convert.ToDouble(s[7]);
            longitude = Convert.ToDouble(s[8]);
            location = new GeoCoordinate(latitude, longitude);
            cat1 = s[9];
            cat2 = s[10];
            info1 = s[11];
            info2 = s[12];
            fullinfo = s[13];
            moreinfo = s[14];
            fulltext = s[15];

            if (cat1 == "Arts")
                filter = FilterCategory.Arts;
            if (cat1 == "Politics")
                filter = FilterCategory.Politics;
            if (cat1 == "Science & Engineering")
                filter = FilterCategory.Science;
            if (cat1 == "Explorer")
                filter = FilterCategory.Exploration;

        }

        public string GetName()
        {
            if (title!=null)
                return title + " " + forenames + " " + surname;
            else
                return forenames + " " + surname;
        }

        public int number { get; set; }
        public string title { get; set; }
        public string forenames { get; set; }
        public string surname { get; set; }
        public string date { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public GeoCoordinate location { get; set; }
        public string cat1 { get; set; }
        public string cat2 { get; set; }
        public string info1 { get; set; }
        public string info2 { get; set; }
        public string fullinfo { get; set; }
        public string moreinfo { get; set; }
        public string fulltext { get; set; }
        public FilterCategory filter { get; set; }

    }
}
