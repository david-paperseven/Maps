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
        ImageBrush brushStartOfRouteMarker;


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

            brushStartOfRouteMarker = new ImageBrush();
            BitmapImage bi4 = new BitmapImage();
            bi4.UriSource = new Uri("Marker/MARKER_OUTLINE_GREEN-S.png", UriKind.Relative);
            brushStartOfRouteMarker.ImageSource = bi4;
        }

        static double pinwidth = 41*1.2;
        static double pinheight = 50*1.2;
        static double selectedpinwidth = pinwidth * 1.3;
        static double selectedpinheight = pinheight * 1.3;
        public Plaque(PlaqueInfo info, MainPage page, MapLayer textLayer)
        {
            InitBrushes();
            Selected = false;
            Info = info;
            Visible = true;
            Found = false;
            myMainPage = page;
            myTextLayer = textLayer;

            Pin = new Rectangle();
            Pin.Width = pinwidth;
            Pin.Height = pinheight;

            Pin.Fill = brushMarker;
            Pin.Opacity = 0.50;

            MapLayer.SetPosition(Pin, Info.location);
            MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);

            Pin.Tap += new EventHandler<GestureEventArgs>(Pin_Tap);

            text = new TextBlock();

            myMainPage.AddOrRemovePlaque.Opacity = 0.0;
        }

        void Pin_Tap(object sender, GestureEventArgs e)
        {
            if (SaveState.Instance.routeState == MainPage.RouteState.Normal)
            {
                myMainPage.ClearSelectedPins();
                SetSelection();
            }

            if (SaveState.Instance.routeState == MainPage.RouteState.SelectStartPoint)
            {
                if (SaveState.Instance.routeList.GetStartPoint() != null)
                {
                    SaveState.Instance.routeList.GetStartPoint().ClearSelection();
                }

                Pin.Fill = brushStartOfRouteMarker;
                Pin.Opacity = 1.0;
                SaveState.Instance.routeList.SetStartPoint(this);
                ShowQuickInfo();

                myMainPage.Done1.Opacity = 1.0;
                myMainPage.SelectPlaquesButton.IsHitTestVisible = true;

            }

            if (SaveState.Instance.routeState == MainPage.RouteState.SelectRoute)
            {
                ShowQuickInfo();

                myMainPage.AddOrRemovePlaque.Opacity = 1.0;
                myMainPage.AddOrRemovePlaqueYesNo.Opacity = 1.0;
                myMainPage.SelectPlaqueYes.IsHitTestVisible = true;
                myMainPage.SelectPlaqueNo.IsHitTestVisible = true;

                if (SaveState.Instance.routeList.GetList().Contains(this))
                {
                    myMainPage.AddOrRemovePlaque.Text = "Remove from route";
                }
                else
                {
                    myMainPage.AddOrRemovePlaque.Text = "Add to route";
                }


                if (SaveState.Instance.routeList.GetCurrentPoint() != null)
                {
                    SaveState.Instance.routeList.GetCurrentPoint().Pin.Width = pinwidth;
                    SaveState.Instance.routeList.GetCurrentPoint().Pin.Height = pinheight;
                }

                Pin.Width = selectedpinwidth;
                Pin.Height = selectedpinheight;
                SaveState.Instance.routeList.SetCurrentPoint(this);
            }

            if (SaveState.Instance.routeState == MainPage.RouteState.SelectEndPoint)
            {
                myMainPage.Done.Opacity = 1;
                myMainPage.DoneEndPointButton.IsHitTestVisible = true;

                // it is currently the endpoint
                if (this == SaveState.Instance.routeList.GetEndPoint())
                {
                    // if it's in the selection list then just return it to selected
                    if (SaveState.Instance.routeList.GetList().Contains(this))
                    {
                        Pin.Fill = brushSelectedMarker;
                        Pin.Opacity = 1.0;
                    }
                    else
                    {
                        ClearSelection();
                    }
                    SaveState.Instance.routeList.SetEndPoint(null);
                }
                else
                {
                    if (SaveState.Instance.routeList.GetEndPoint() != null) // got to clear the existing endpoint
                    {
                        if (SaveState.Instance.routeList.GetList().Contains(SaveState.Instance.routeList.GetEndPoint()))
                        {
                            SaveState.Instance.routeList.GetEndPoint().Pin.Fill = brushSelectedMarker;
                            SaveState.Instance.routeList.GetEndPoint().Pin.Opacity = 1.0;
                        }
                        else
                        {
                            SaveState.Instance.routeList.GetEndPoint().ClearSelection();
                        }

                    }
                    ShowQuickInfo();
                    Pin.Fill = brushEndOfRouteMarker;
                    Pin.Opacity = 1.0;
                    SaveState.Instance.routeList.SetEndPoint(this);
                }
            }
            /*
            if (myMainPage.routeState == MainPage.RouteState.Travelling)
            {
                myMainPage.FullInfoName.Text = Info.GetName() +"\n";
                myMainPage.FullInfoDateAndCategory.Text = "(" + Info.date + ") " + Info.info1 + "\n\n";
                myMainPage.FullInfoPlaqueInfo.Text = Info.fullinfo + "\n\n";
                myMainPage.FullInfoPlaqueFullInfo.Text = Info.fulltext;

                myMainPage.ShowFullInfoButton.Opacity = 1;
                myMainPage.ShowFullInfoButton.IsHitTestVisible = true;
                //VisualStateManager.GoToState(myMainPage, "FullInfoState", true);
            }
             * */
        }

        public void ShowFullInfo()
        {
            myMainPage.FullInfoName.Text = Info.GetName() + "\n";
            myMainPage.FullInfoDateAndCategory.Text = "(" + Info.date + ") " + Info.info1 + "\n\n";
            myMainPage.FullInfoPlaqueInfo.Text = Info.fullinfo + "\n\n";
            myMainPage.FullInfoPlaqueFullInfo.Text = Info.fulltext;

            myMainPage.ShowFullInfoButton.Opacity = 1;
            myMainPage.ShowFullInfoButton.IsHitTestVisible = true;
            VisualStateManager.GoToState(myMainPage, "FullInfoState", true);
        }

        public void ShowQuickInfo()
        {
            switch (SaveState.Instance.routeState)
            {
                case MainPage.RouteState.SelectStartPoint:
                    {
                        VisualStateManager.GoToState(myMainPage.StartPlaqueNameInfo, "SlideUp", true);
                        myMainPage.StartPlaqueNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                        myMainPage.StartPlaqueNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                        break;
                    }
                case MainPage.RouteState.SelectEndPoint:
                    {
                        VisualStateManager.GoToState(myMainPage.EndPlaqueNameInfo, "SlideUp", true);
                        myMainPage.EndPlaqueNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                        myMainPage.EndPlaqueNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                        break;
                    }
                case MainPage.RouteState.SelectRoute:
                    {
                        VisualStateManager.GoToState(myMainPage.RoutePlaqueNameInfo, "SlideUp", true);
                        myMainPage.RoutePlaqueNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                        myMainPage.RoutePlaqueNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                        break;
                    }
            }
         
        }

        public void UnshowQuickInfo()
        {
        }

        public GeoCoordinate GetLocation() { return Info.location; }
        public void SetColor(Color color) { /*Pin.Background = new SolidColorBrush(color);*/ }
        public Shape Pin { get; set; }
        public bool Selected { get; set; }
        public PlaqueInfo Info { get; set; }
        public bool Visible { get; set; }
        public bool Found { get; set; }
        public void ClearSelection()
        {
            Pin.Fill = brushMarker;
            Pin.Opacity = 0.50;
            Selected = false;
            //UnshowQuickInfo();
        }
        public void SetSelection()
        {
            Pin.Fill = brushSelectedMarker;
            Pin.Opacity = 1.0;
            Selected = true;
            //ShowQuickInfo();
        }

        public void SetGreen()
        {
            Pin.Fill = brushStartOfRouteMarker;
            Pin.Opacity = 1.0;
        }

        public void ResetSize()
        {
            Pin.Width = pinwidth;
            Pin.Height = pinheight;
        }
    }
}
