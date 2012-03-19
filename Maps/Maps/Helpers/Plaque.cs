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
    public class Plaque
    {
        MainPage myMainPage;
        MapLayer myTextLayer;
        TextBlock text;

        ImageBrush brushMarker;
        ImageBrush brushSelectedMarker;
        ImageBrush brushEndOfRouteMarker;


        public void InitBrushes()
        {
            brushMarker = new ImageBrush();
            BitmapImage bi = new BitmapImage();
            bi.UriSource = new Uri("Marker/MARKER_BLUE.png", UriKind.Relative);
            brushMarker.ImageSource = bi;

            brushSelectedMarker = new ImageBrush();
            BitmapImage bi2 = new BitmapImage();
            bi2.UriSource = new Uri("Marker/MARKER_OUTLINE_BLUE-S.png", UriKind.Relative);
            brushSelectedMarker.ImageSource = bi2;

            brushEndOfRouteMarker = new ImageBrush();
            BitmapImage bi3 = new BitmapImage();
            bi3.UriSource = new Uri("Marker/MARKER_OUTLINE_RED-S.png", UriKind.Relative);
            brushEndOfRouteMarker.ImageSource = bi3;
        }

        public Plaque(PlaqueInfo info, MainPage page, MapLayer textLayer)
        {
            InitBrushes();
            Selected = false;
            Info = info;
            Visible = true;
            myMainPage = page;
            myTextLayer = textLayer;

            Pin = new Rectangle();
            Pin.Width = 41;
            Pin.Height = 50;

            Pin.Fill = brushMarker;
            Pin.Opacity = 0.50;

            MapLayer.SetPosition(Pin, Info.location);
            MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);

            Pin.Tap += new EventHandler<GestureEventArgs>(Pin_Tap);

            text = new TextBlock();
        }

        void Pin_Tap(object sender, GestureEventArgs e)
        {
            if (myMainPage.routeState == MainPage.RouteState.Normal)
            {
                myMainPage.ClearSelectedPins();
                SetSelection();
            }

            if (myMainPage.routeState == MainPage.RouteState.SelectRoute)
            {
                if (Selected == false)
                {
                    SetSelection();
                    myMainPage.routeList.Add(this);
                }
                else
                {
                    myMainPage.routeList.Remove(this);
                    ClearSelection();
                }
            }

            if (myMainPage.routeState == MainPage.RouteState.SelectEndPoint)
            {
                // it is currently the endpoint
                if (this == myMainPage.routeList.GetEndPoint())
                {
                    // if it's in the selection list then just return it to selected
                    if (myMainPage.routeList.GetList().Contains(this))
                    {
                        Pin.Fill = brushSelectedMarker;
                        Pin.Opacity = 1.0;
                    }
                    else
                    {
                        ClearSelection();
                    }
                    myMainPage.routeList.SetEndPoint(null);
                }
                else
                {
                    if (myMainPage.routeList.GetEndPoint() != null) // got to clear the existing endpoint
                    {
                        if (myMainPage.routeList.GetList().Contains(myMainPage.routeList.GetEndPoint()))
                        {
                            myMainPage.routeList.GetEndPoint().Pin.Fill = brushSelectedMarker;
                            myMainPage.routeList.GetEndPoint().Pin.Opacity = 1.0;
                        }
                        else
                        {
                            myMainPage.routeList.GetEndPoint().ClearSelection();
                        }

                    }
                    ShowQuickInfo();
                    Pin.Fill = brushEndOfRouteMarker;
                    Pin.Opacity = 1.0;
                    myMainPage.routeList.SetEndPoint(this);
                }
            }
        }

        public void ShowQuickInfo()
        {
            if (myMainPage.routeState == MainPage.RouteState.SelectEndPoint)
            {
                myMainPage.EndPointPlaqueName.Text = Info.title + " " + Info.forenames + " " + Info.surname;
            }
            else
            {
                myMainPage.SelectRoutePlaqueName.Text = Info.title + " " + Info.forenames + " " + Info.surname;
            }

/*
            if (!myTextLayer.Children.Contains(text))
            {
                text.Text = Info.title + " " + Info.forenames + " " + Info.surname;
                text.FontSize = 20;
                text.FontWeight = FontWeights.Bold;
                text.Foreground = new SolidColorBrush(Colors.Blue);
                MapLayer.SetPosition(text, Info.location);
                MapLayer.SetPositionOrigin(text, PositionOrigin.Center);
                MapLayer.SetPositionOffset(text, new Point(0, -30));
                myTextLayer.Children.Add(text);
            }
 * */
        }

        public void UnshowQuickInfo()
        {
            if (myMainPage.routeState == MainPage.RouteState.SelectEndPoint)
            {
                myMainPage.EndPointPlaqueName.Text = "";
            }
            else
            {
                myMainPage.SelectRoutePlaqueName.Text = "";
            }
            //myTextLayer.Children.Remove(text);
        }

        public GeoCoordinate GetLocation() { return Info.location; }
        public void SetColor(Color color) { /*Pin.Background = new SolidColorBrush(color);*/ }
        public Shape Pin { get; set; }
        public bool Selected { get; set; }
        public PlaqueInfo Info { get; set; }
        public bool Visible { get; set; }
        public void ClearSelection()
        {
            Pin.Fill = brushMarker;
            Pin.Opacity = 0.50;
            Selected = false;
            UnshowQuickInfo();
        }
        public void SetSelection()
        {
            Pin.Fill = brushSelectedMarker;
            Pin.Opacity = 1.0;
            Selected = true;
            ShowQuickInfo();
        }
    }
}
