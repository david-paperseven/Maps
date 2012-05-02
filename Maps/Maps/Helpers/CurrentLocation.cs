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
using System.Windows.Threading;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;

namespace Maps.Helpers
{
    public class CurrentLocation
    {
        public GeoCoordinate Location;
        Map myMap;
        Ellipse circle;

        double maxsize = 85;
        double minsize = 70.0;
        double growshrink = 1.0;
        double step = 0.3;

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
            Color strokecolor = new Color();
            strokecolor.A = 255;
            strokecolor.R = 255;
            strokecolor.G = 0;
            strokecolor.B = 179;
            Circle.Stroke = new SolidColorBrush(strokecolor);
            Color fillcolor = new Color();
            fillcolor.A = 128;
            fillcolor.R = 255;
            fillcolor.G = 212;
            fillcolor.B = 242;

            Circle.Fill = new SolidColorBrush(fillcolor);
            Circle.HorizontalAlignment = HorizontalAlignment.Left;
            Circle.VerticalAlignment = VerticalAlignment.Center;
            Circle.Width = 75;
            Circle.Height = 75;
            circle = Circle;
            myMap.Children.Add(Circle);
            CircleDispatcherTimer();

            GeoCoordinate defaultlocation = new GeoCoordinate(51.511397, -0.128263);
            SetLocation(defaultlocation);
        }

        private void CircleDispatcherTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();

            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    if (SaveState.Instance.Paused == false)
                    {
                        double currentSize = circle.Width;
                        currentSize += step * growshrink;
                        if (currentSize > maxsize)
                        {
                            currentSize = maxsize;
                            growshrink *= -1.0;
                        }
                        if (currentSize < minsize)
                        {
                            currentSize = minsize;
                            growshrink *= -1.0;
                        }
                        circle.Width = currentSize;
                        circle.Height = currentSize;
                    }
                };

            timer.Interval = new TimeSpan(0, 0, 0);
            timer.Start();
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
