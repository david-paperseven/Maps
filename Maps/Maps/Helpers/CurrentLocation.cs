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
        GeoCoordinate newLocation;
        Map myMap;
        Ellipse circle;
        GeoCoordinate defaultlocation;
        public bool notInLondon;

        double maxsize = 10;
        double minsize = -10.0;
        double growshrink = 1.0;
        double currentSize = -10;
        double circlesize=200;
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
            Circle.Width = circlesize;
            Circle.Height = circlesize;
            circle = Circle;
            myMap.Children.Add(Circle);
            CircleDispatcherTimer();

            notInLondon = false;
            defaultlocation = new GeoCoordinate(51.511397, -0.128263);
            defaultlocation.HorizontalAccuracy = 30.0;
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

                        double pixelAccuracy = TileSystem.GroundResolution(Location.Latitude, (int)myMap.ZoomLevel);
                        double newsize = (Location.HorizontalAccuracy / pixelAccuracy);
                        if (newsize < 30)
                            newsize = 30;

                        circlesize = circlesize + (newsize - circlesize) * 0.1;
                        if (!(circlesize > 0 && circlesize < 1000))
                        {
                            circlesize = circlesize;
                        }

                        double pixelsize = circlesize + currentSize;
                        if (pixelsize < 0)
                        {
                            pixelsize = 0.0;
                        }
                        circle.Width = pixelsize;
                        circle.Height = pixelsize;

                        // position interp
                        Location.Latitude = Location.Latitude + (newLocation.Latitude - Location.Latitude) * 0.2;
                        Location.Longitude = Location.Longitude + (newLocation.Longitude - Location.Longitude) * 0.2;
                        /*
                    //    MapLayer.SetPosition(Pin, Location);
                    //    MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);
                        
                        //Pin.InvalidateMeasure();
                        MapLayer.SetPosition(Circle, Location);
                        MapLayer.SetPositionOrigin(Circle, PositionOrigin.Center);
                        Circle.Width = Circle.Width;
                        */

                    }
                };

            timer.Interval = new TimeSpan(0, 0, 0);
            timer.Start();
        }

        public bool SetLocation(GeoCoordinate location)
        {
            
            double distance = location.GetDistanceTo(defaultlocation);
            /*
            if (distance > 30000) // 30km
            {
                if (myMap.Children.Contains(Circle))
                {
                    myMap.Children.Remove(Circle);
                    myMap.Children.Remove(Pin);
                }
                notInLondon = true;
                return false;
            }
            */
            notInLondon = false;

            if (!myMap.Children.Contains(Circle))
            {
                myMap.Children.Add(Circle);
                myMap.Children.Add(Pin);
            }

            if (Location != null)
            {
                distance = location.GetDistanceTo(Location);
                newLocation = location;
            }
            else
            {
                Location = location;
            }

            if (!(location.HorizontalAccuracy > 0 && location.HorizontalAccuracy < 1000))
            {
                location.HorizontalAccuracy = location.HorizontalAccuracy;
            }

            MapLayer.SetPosition(Pin, Location);
            MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);

            MapLayer.SetPosition(Circle, Location);
            MapLayer.SetPositionOrigin(Circle, PositionOrigin.Center);

            return true;
        }

        public GeoCoordinate GetLocation() { return Location; }

        void Pin_Tap(object sender, GestureEventArgs e)
        {
        }

        public Shape Pin { get; set; }
        public Ellipse Circle { get; set; }
    }
}
