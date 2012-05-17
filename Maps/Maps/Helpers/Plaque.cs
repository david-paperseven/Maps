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
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Device.Location;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;

namespace Maps.Helpers
{
    public static class FrameworkElementExtensions
    {
        public static object TryFindResource(this FrameworkElement element, object resourceKey)
        {
            var currentElement = element;

            while (currentElement != null)
            {
                var resource = currentElement.Resources[resourceKey];
                if (resource != null)
                {
                    return resource;
                }

                currentElement = currentElement.Parent as FrameworkElement;
            }

            return Application.Current.Resources[resourceKey];
        }
    }
    
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

            ClearSelection();

            MapLayer.SetPosition(Pin, Info.location);
            MapLayer.SetPositionOrigin(Pin, PositionOrigin.Center);

            Pin.Tap += new EventHandler<GestureEventArgs>(Pin_Tap);

            text = new TextBlock();

            myMainPage.AddPlaque.Visibility = Visibility.Collapsed;
        }

        void Pin_Tap(object sender, GestureEventArgs e)
        {

            
            if (SaveState.Instance.routeState == MainPage.RouteState.Browsing)
            {
                Pin.Width = selectedpinwidth;
                Pin.Height = selectedpinheight;

                if (SaveState.Instance.routeList.GetCurrentPoint() != null)
                {
                    SaveState.Instance.routeList.GetCurrentPoint().Pin.Width = pinwidth;
                    SaveState.Instance.routeList.GetCurrentPoint().Pin.Height = pinheight;
                }
                SaveState.Instance.routeList.SetCurrentPoint(this);

                if (SaveState.Instance.CurrentVisualState == "LocationServicesDisabledState")
                {
                    VisualStateManager.GoToState(myMainPage.BrowsingModeNameInfo1, "SlideUp", true);
                    myMainPage.BrowsingModeNameInfo1.SelectRoutePlaqueName.Text = Info.GetName();
                    myMainPage.BrowsingModeNameInfo1.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                }
                else
                if (SaveState.Instance.CurrentVisualState == "NotInLondonState")
                {
                    VisualStateManager.GoToState(myMainPage.BrowsingModeNameInfo2, "SlideUp", true);
                    myMainPage.BrowsingModeNameInfo2.SelectRoutePlaqueName.Text = Info.GetName();
                    myMainPage.BrowsingModeNameInfo2.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                }
                else
                {
                    VisualStateManager.GoToState(myMainPage.BrowsingModeNameInfo, "SlideUp", true);
                    myMainPage.BrowsingModeNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                    myMainPage.BrowsingModeNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                }
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

                myMainPage.DoneButton.Opacity = 1.0;
                myMainPage.DoneButton.IsHitTestVisible = true;

            }

            if (SaveState.Instance.routeState == MainPage.RouteState.SelectRoute)
            {
                if (SaveState.Instance.routeList.GetStartPoint() == this)
                    return;
                ShowQuickInfo();

                myMainPage.Select_plaques_to_visit.Visibility = Visibility.Collapsed;
                myMainPage.RemovePlaque.Visibility = Visibility.Collapsed;
                myMainPage.RevisitPlaque.Visibility = Visibility.Collapsed;
                myMainPage.AddPlaque.Visibility = Visibility.Collapsed; 

                
                if (SaveState.Instance.routeList.GetList().Contains(this))
                {
                    myMainPage.RemovePlaque.Visibility = Visibility.Visible; 
                }
                else
                {
                    if (PersistentStorage.Instance.Visited(this.Info.number))
                        myMainPage.RevisitPlaque.Visibility = Visibility.Visible; 
                    else
                       myMainPage.AddPlaque.Visibility = Visibility.Visible; 
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

                myMainPage.AppGenRouteEndPointDonButton.Opacity = 1.0;
                myMainPage.AppGenRouteEndPointDonButton.IsHitTestVisible = true;


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
                    myMainPage.Done.Opacity = 0.50;
                    myMainPage.DoneEndPointButton.IsHitTestVisible = false;
                    myMainPage.AppGenRouteEndPointDonButton.Opacity = 0.5;
                    myMainPage.AppGenRouteEndPointDonButton.IsHitTestVisible = false;
                    UnshowQuickInfo();

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
        }


        public void ShowFullInfo()
        {
            myMainPage.CentrePlaqueForFullInfo(GetLocation());

            myMainPage.FullInfoRichTextBox.Blocks.Clear();
            myMainPage.FullInfoScrollViewer.ScrollToVerticalOffset(150);
            myMainPage.FullInfoRichTextBox.Height = 1700.0;
            myMainPage.FullInfo1.Height = 1700.0;

            Brush white = new SolidColorBrush(Colors.White);

            Run unlockedRun = new Run();
            Run nameRun = new Run();
            Run dateRun = new Run();
            Run addressRun = new Run();
            Run infoRun = new Run();
            Run fullInfoRun = new Run();
            Run exitRun = new Run();
            Run spaceAtBottom = new Run();

            FontFamily headline = new FontFamily("/Fonts/NokiaPureHeadlineBd.ttf#Nokia Pure Headline");
            FontFamily light = new FontFamily("/Fonts/NokiaPureHeadlineLt.ttf#Nokia Pure Headline Light");
            unlockedRun.Text = "You've unlocked this plaque!\n";
            unlockedRun.FontFamily = headline;
            unlockedRun.FontSize = 30;
            unlockedRun.Foreground = white;

            nameRun.Text = Info.GetName() + "\n";
            nameRun.FontFamily = headline;
            nameRun.FontSize = 37;
            nameRun.Foreground = white;
            dateRun.Text = "(" + Info.date + ") " + Info.info1 + "\n";
            dateRun.FontFamily = light;
            dateRun.FontSize = 37;
            dateRun.Foreground = white;
            addressRun.Text = Info.address1 + "\n" + Info.address2 + "\n";
            addressRun.FontFamily = light;
            addressRun.FontSize = 37;
            addressRun.Foreground = white;
            infoRun.Text = Info.fullinfo + "\n";
            infoRun.FontFamily = headline;
            infoRun.FontSize = 30;
            infoRun.Foreground = white;
            fullInfoRun.Text = Info.fulltext + "\n\n";
            fullInfoRun.FontFamily = light;
            fullInfoRun.FontSize = 30;
            fullInfoRun.Foreground = white;
            exitRun.Text = "\n\nExit\n";
            exitRun.FontFamily = headline;
            exitRun.FontSize = 37;
            exitRun.Foreground = white;

                    		//<Image x:Name="LINE_BREAK_copy3" Height="3" Canvas.Left="46" Source="QuickInfo_Images/LINE BREAK copy.png" Canvas.Top="65" Width="388" d:IsHidden="True"/>

            Image LineBreak = new Image();
            LineBreak.Source = new BitmapImage(new Uri("QuickInfo_Images/LINE BREAK copy.png",UriKind.RelativeOrAbsolute));
            LineBreak.Height = 8;
            LineBreak.Width = 388;
            InlineUIContainer LBUI = new InlineUIContainer();
            LBUI.Child = LineBreak;

            Image LineBreak2 = new Image();
            LineBreak2.Source = new BitmapImage(new Uri("QuickInfo_Images/LINE BREAK copy.png", UriKind.RelativeOrAbsolute));
            LineBreak2.Height = 8;
            LineBreak2.Width = 388;
            InlineUIContainer LBUI2 = new InlineUIContainer();
            LBUI2.Child = LineBreak2;

            Image LineBreak3 = new Image();
            LineBreak3.Source = new BitmapImage(new Uri("QuickInfo_Images/LINE BREAK copy.png", UriKind.RelativeOrAbsolute));
            LineBreak3.Height = 8;
            LineBreak3.Width = 388;
            InlineUIContainer LBUI3 = new InlineUIContainer();
            LBUI3.Child = LineBreak3;

            Image LineBreak4 = new Image();
            LineBreak4.Source = new BitmapImage(new Uri("QuickInfo_Images/LINE BREAK copy.png", UriKind.RelativeOrAbsolute));
            LineBreak4.Height = 8;
            LineBreak4.Width = 388;
            InlineUIContainer LBUI4 = new InlineUIContainer();
            LBUI4.Child = LineBreak4;

            Hyperlink link = new Hyperlink();
            link.FontFamily = light;
            link.FontSize = 30;
            link.Foreground = white;
            link.Inlines.Add("More Info\n\n");
            link.NavigateUri = new Uri(Info.moreinfo);
            link.TargetName = "_blank";

            //p1.Inlines.Add(exitRun);

            Button button = new Button();
            button.Width = 128;
            button.Height = 128;
            button.Click += exitFullInfoClick;
            button.FontFamily = headline;
            button.FontSize = 37;
            button.Content = "Exit";
            button.BorderBrush = null;
            button.Style = (Style)FrameworkElementExtensions.TryFindResource(myMainPage.element, "ExitButton");

            InlineUIContainer buttonUI = new InlineUIContainer();
            buttonUI.Child = button;

            Paragraph p1 = new Paragraph();
            p1.Inlines.Add(unlockedRun);
            p1.Inlines.Add(LBUI3);
            p1.Inlines.Add(nameRun);
            p1.Inlines.Add(dateRun);
            //p1.Inlines.Add(LBUI);
            p1.Inlines.Add(addressRun);
            p1.Inlines.Add(LBUI4);
            p1.Inlines.Add(infoRun);
            p1.Inlines.Add(LBUI2);
            p1.Inlines.Add(fullInfoRun);
            p1.Inlines.Add(link);
            p1.Inlines.Add(buttonUI);


            myMainPage.FullInfoRichTextBox.Blocks.Add(p1);

            VisualStateManager.GoToState(myMainPage, "FullInfoState", true);
        }


        void exitFullInfoClick(object sender, RoutedEventArgs e)
        {
            myMainPage.ExitFullInfoState();
        }

        public void ShowQuickInfo()
        {
            switch (SaveState.Instance.routeState)
            {
                case MainPage.RouteState.SelectStartPoint:
                    {
                        if (SaveState.Instance.routeMode == MainPage.RouteMode.QuickStart)
                        {
                            myMainPage.QuickStartNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                            myMainPage.QuickStartNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                            
                        }
                        else
                        {
                            VisualStateManager.GoToState(myMainPage.StartPlaqueNameInfo, "SlideUp", true);
                            myMainPage.StartPlaqueNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                            myMainPage.StartPlaqueNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                        }
                        break;
                    }
                case MainPage.RouteState.SelectEndPoint:
                    {
                        if (SaveState.Instance.routeMode == MainPage.RouteMode.UserSelected)
                        {
                            VisualStateManager.GoToState(myMainPage.EndPlaqueNameInfo, "SlideUp", true);
                            myMainPage.EndPlaqueNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                            myMainPage.EndPlaqueNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                        }
                        else
                        {
                            VisualStateManager.GoToState(myMainPage.AppGenEndPlaqueNameInfo, "SlideUp", true);
                            myMainPage.AppGenEndPlaqueNameInfo.SelectRoutePlaqueName.Text = Info.GetName();
                            myMainPage.AppGenEndPlaqueNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + Info.date + ") " + Info.info1;
                        }
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
            VisualStateManager.GoToState(myMainPage.EndPlaqueNameInfo, "Start", true);
            VisualStateManager.GoToState(myMainPage.AppGenEndPlaqueNameInfo, "Start", true);
        }

        public GeoCoordinate GetLocation() { return Info.location; }
        public void SetColor(Color color) { /*Pin.Background = new SolidColorBrush(color);*/ }
        public Shape Pin { get; set; }
        public bool Selected { get; set; }
        public PlaqueInfo Info { get; set; }
        public bool Visible { get; set; }
        public bool Found { get; set; }
        
        public void SetPlaqueFound()
        {
            Found = true;
            ShowFullInfo();
            PersistentStorage.Instance.SetVisited(Info.number);
            ClearSelection();
        }
        
        public void ClearSelection()
        {
            if (PersistentStorage.Instance.Visited(Info.number))
            {
                Pin.Fill = brushMarker;
                Pin.Opacity = 1.0;
            }
            else
            {
                Pin.Fill = brushMarker;
                Pin.Opacity = 0.50;
            }
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

        public void SetRed()
        {
            Pin.Fill = brushEndOfRouteMarker;
            Pin.Opacity = 1.0;
        }

        public void ResetSize()
        {
            Pin.Width = pinwidth;
            Pin.Height = pinheight;
        }
    }
}
