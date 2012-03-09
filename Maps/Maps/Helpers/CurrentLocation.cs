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
using System.Device.Location;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;

namespace Maps.Helpers
{
    public class CurrentLocation
    {
        public GeoCoordinate Location;

        public CurrentLocation()
        {
            Pin = new Rectangle();
            Pin.Width = 20;
            Pin.Height = 20;
            Pin.Fill = new SolidColorBrush(Colors.Red);
            Pin.Opacity = 0.80;
            Pin.Tap += new EventHandler<GestureEventArgs>(Pin_Tap);

        }

        public void SetLocation(GeoCoordinate location)
        {
            MapLayer.SetPosition(Pin, location);
            MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);
            Location = location;

        }

        public GeoCoordinate GetLocation() { return Location; }

        void Pin_Tap(object sender, GestureEventArgs e)
        {
        }

        public Shape Pin { get; set; }
    }
}
