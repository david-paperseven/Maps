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
        float OpacitySelected = 0.75f;
        float OpacityUnselected = 0.40f;

        public Plaque(PlaqueInfo info, MainPage page, MapLayer textLayer)
        {
            Selected = false;
            Info = info;
            Visible = true;
            myMainPage = page;
            myTextLayer = textLayer;

            Pin = new Rectangle();
            Pin.Width = 33;
            Pin.Height = 40;
            ImageBrush brush = new ImageBrush();
            BitmapImage bi = new BitmapImage();
            bi.UriSource = new Uri("Marker/MARKER.png", UriKind.Relative);
            brush.ImageSource = bi;
            Pin.Fill = brush;
            Pin.Opacity = OpacityUnselected;
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
                        Pin.Opacity = OpacityUnselected;
                    }
                    else
                    {
                        ClearSelection();
                    }
                    myMainPage.routeList.SetEndPoint(null);
                }
                else
                {
                    Pin.Opacity = OpacitySelected;
                    SetSelection();
                    myMainPage.routeList.SetEndPoint(this);
                }
            }
        }

        public void ShowQuickInfo()
        {
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
        }

        public void UnshowQuickInfo()
        {
            myTextLayer.Children.Remove(text);
        }

        public void FillInQuickInfo()
        {
            if (Info.title != null)
                myMainPage.Name.Text = Info.title + " " + Info.forenames + " " + Info.surname;
            else
                myMainPage.Name.Text = Info.forenames + " " + Info.surname;
            myMainPage.Date.Text = Info.date;
            myMainPage.Address.Text = Info.address1;
        }

        public void FillInFullInfo()
        {
            myMainPage.Name1.Text = Info.title + Info.forenames + Info.surname;
            myMainPage.Birth_Data.Text = "("+Info.date+")";
            myMainPage.Info.Text = Info.fullinfo + "\n\n" + Info.address1 + "\n" + Info.address2 + "\n\n" + Info.moreinfo;
        }

        public GeoCoordinate GetLocation() { return Info.location; }
        public void SetColor(Color color) { /*Pin.Background = new SolidColorBrush(color);*/ }
        public Shape Pin { get; set; }
        public bool Selected { get; set; }
        public PlaqueInfo Info { get; set; }
        public bool Visible { get; set; }
        public void ClearSelection()
        {
            Pin.Opacity = 0.3;
            Pin.Opacity = OpacityUnselected;
            Selected = false;
            UnshowQuickInfo();
        }
        public void SetSelection()
        {
            Pin.Opacity = OpacitySelected;
            Selected = true;
            ShowQuickInfo();
        }
    }
}
