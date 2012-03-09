using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Maps.Helpers
{
    public class Summary
    {
        public Summary() 
        { 
        }

        public string GetTime()
        {
            long numMins = Time / 60;
            long hours = numMins / 60;
            long mins = numMins % 60;

            string output = hours.ToString() + ":" + mins.ToString("D2");
            return output;
        }

        public double Distance { get; set; }
        public long Time { get; set; }
        public int NumPlaques { get; set; }
    }
}
