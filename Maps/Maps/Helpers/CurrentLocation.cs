using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
        Map myMap;
        public CurrentLocation(Map map)
        {
            ImageBrush brush = new ImageBrush();
            BitmapImage bi = new BitmapImage();
            bi.UriSource = new Uri("Marker/HOME.png", UriKind.Relative);
            brush.ImageSource = bi;

            Pin = new Rectangle();
            Pin.Width = 30;
            Pin.Height = 30;
            Pin.Fill = brush;
            Pin.Opacity = 0.80;
            Pin.Tap += new EventHandler<GestureEventArgs>(Pin_Tap);

            myMap = map;
            Circle = new Ellipse();
            Circle.Stroke = new SolidColorBrush(Colors.Black);
            Color grey = new Color();
            grey.A = 128;
            grey.R = 128;
            grey.G = 128;
            grey.B = 128;

            Circle.Fill = new SolidColorBrush(grey);
            Circle.HorizontalAlignment = HorizontalAlignment.Left;
            Circle.VerticalAlignment = VerticalAlignment.Center;
            Circle.Width = 75;
            Circle.Height = 75;
            myMap.Children.Add(Circle);

        }

        public void SetLocation(GeoCoordinate location)
        {
            MapLayer.SetPosition(Pin, location);
            MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);

            MapLayer.SetPosition(Circle, location);
            MapLayer.SetPositionOrigin(Circle, PositionOrigin.Center);

            Location = location;

        }

        public GeoCoordinate GetLocation() { return Location; }

        void Pin_Tap(object sender, GestureEventArgs e)
        {
        }

        public Shape Pin { get; set; }
        public Ellipse Circle { get; set; }
    }
}
