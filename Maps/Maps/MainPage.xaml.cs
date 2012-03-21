using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;


using Maps.Helpers;

using Maps.RouteService;

namespace Maps
{
    public partial class MainPage : PhoneApplicationPage
    {
        public enum RouteState
        {
            Normal,
            SelectStartPoint,
            SelectRoute,
            SelectEndPoint,
            Travelling
        }

        GeoCoordinateWatcher watcher;
        Route route;
        Summary summary;
        PlaqueLoader plaqueLoader;
        List<PlaqueInfo> plaqueInfoList;
        List<Plaque> plaques;
        MapLayer pinLayer;
        MapLayer textLayer;
        CurrentLocation currentLocation;
        Journey journey;
        public RouteList routeList;
        public RouteState routeState;

        public MainPage()
        {
            InitializeComponent();
            InitializeWatcher();
            route = new Route(((ApplicationIdCredentialsProvider)myMap.CredentialsProvider).ApplicationId,myMap,this);
            summary = new Summary();
            currentLocation = new CurrentLocation(myMap);
            routeList = new RouteList();
            plaqueLoader = new PlaqueLoader();
            plaqueInfoList = plaqueLoader.Load();

            InitializePlaques();

            myMap.ZoomLevel = 15;
            routeState = RouteState.Normal;

            journey = new Journey(this);

            // Visual States are always on the first child of the control template  
            FrameworkElement element = VisualTreeHelper.GetChild(MainPageElement, 0) as FrameworkElement;
            VisualStateGroup group = FindVisualState(element, "UserSelectedRoutesMenu");
            group.CurrentStateChanged += new EventHandler<VisualStateChangedEventArgs>(CurrentStateChanged);

            VisualStateManager.GoToState(this, "SplashScreen1", true);
        }

        VisualStateGroup FindVisualState(FrameworkElement element, string name)
        {
            if (element == null)
                return null;

            System.Collections.IList groups = VisualStateManager.GetVisualStateGroups(element);
            foreach (VisualStateGroup group in groups)
                if (group.Name == name)
                    return group;

            return null;
        } 

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            // remove the navigation visibility.  
            // This control is redundant with the multi-touch capabilities of the phone.
            //myMap.NavigationVisibility = System.Windows.Visibility.Collapsed;
            // removes the copyright note
            myMap.CopyrightVisibility = System.Windows.Visibility.Collapsed;
            // removes the Bing logo
            myMap.LogoVisibility = System.Windows.Visibility.Collapsed;
        }

        private void InitializePlaques()
        {
            pinLayer = new MapLayer();
            myMap.Children.Add(pinLayer);

            textLayer = new MapLayer();
            myMap.Children.Add(textLayer);

            plaques = new List<Plaque>();
            for (int i = 0; i < plaqueInfoList.Count; i++)
            {
                Plaque plaque = new Plaque(plaqueInfoList[i], this, textLayer);
                plaques.Add(plaque);
                pinLayer.Children.Add(plaque.Pin);
            }

            GeoCoordinate location = new GeoCoordinate(51.511397, -0.128263);
            currentLocation.SetLocation(location);
            myMap.Children.Add(currentLocation.Pin);

        }

        private void InitializeWatcher()
        {
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy;
            watcher.MovementThreshold = 10.0f; // meters of change before "PositionChanged"
            // wire up event handlers
            watcher.StatusChanged += new
            EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new
            EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            // start up LocServ in bg; watcher_StatusChanged will be called when complete.
            new Thread(startLocServInBackground).Start();
        }

        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The Location Service is disabled or unsupported.
                    // Check to see if the user has disabled the Location Service.
                    if (watcher.Permission == GeoPositionPermission.Denied)
                    {
                        // The user has disabled the Location Service on their device.
                         System.Diagnostics.Debug.WriteLine("You have disabled Location Service.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Location Service is not functioning on this device.");
                    }
                    break;
                case GeoPositionStatus.Initializing:
                    System.Diagnostics.Debug.WriteLine("Location Service is retrieving data...");
                    // The Location Service is initializing.
                    break;
                case GeoPositionStatus.NoData:
                    // The Location Service is working, but it cannot get location data.
                    System.Diagnostics.Debug.WriteLine("Location data is not available.");
                    break;
                case GeoPositionStatus.Ready:
                    // The Location Service is working and is receiving location data.
                    System.Diagnostics.Debug.WriteLine("Location data is available.");
                    break;
            }
            
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            /*
            myMap.Center = e.Position.Location;
            plaques[0].SetLocation(e.Position.Location);
            //myMap.Center = plaques[0].GetLocation();
            myMap.ZoomLevel = 15;
             */
           // e.Position.Location.HorizontalAccuracy
            //currentLocation.SetLocation(e.Position.Location);
            myMap.Center = currentLocation.GetLocation();
        }


        void startLocServInBackground()
        {
            watcher.TryStart(true, TimeSpan.FromMilliseconds(60000));
        }

        public void ClearSelectedPins()
        {
            foreach (Plaque p in plaques)
            {
                if (p.Selected == true)
                {
                    p.ClearSelection();
                }
            }
        }

        void DrawRoute()
        {
            System.Collections.ObjectModel.ObservableCollection<Waypoint> waypoints = routeList.GetFinalList(currentLocation.GetLocation());

            if (waypoints.Count > 1)
            {
                summary.NumPlaques = waypoints.Count - 1; // don't count the current position 
                route.CalculateRoute(waypoints,summary);
                RemoveAllPinsExceptCurrentRoute();
                VisualStateManager.GoToState(this, "DistanceSummary1", true);
            }
        }

        public void SummaryLoaded()
        {
            Distance.Text = summary.Distance.ToString() + " km";
            Approx_time_xx.Text = "Approx time "+summary.GetTime();
            NumPlaques.Text = summary.NumPlaques.ToString() + " plaques";
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
//            routeState = RouteState.Normal;
            UpdatePlaqueVisibilty();

            if (routeState == RouteState.Travelling)
            {
                VisualStateManager.GoToState(this, "RouteSummary1", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "SelectStartPlaque", true);
                routeState = RouteState.SelectStartPoint;
            }
        }

        private void ExitFilterMenu_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnly", true);
        }

        private void ExitUserSelectedRoute_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnly", true);
        }

        private void ExitFullInfo_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnly", true);
        }

        private void DoneEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (routeList.GetEndPoint() != null)
            {
                // DOESN'T LOOK RIGHT??
                VisualStateManager.GoToState(this, "SelectStartPlaque", true);
                DrawRoute();
                routeState = RouteState.Normal;
            }
        }

        private void SelectStartPlaque_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaque", true);
            ClearSelectedPins();
        }

        private void AddRemovePlaqueFromListYes_Click(object sender, RoutedEventArgs e)
        {
            Plaque pl = routeList.GetCurrentPoint();
            if (routeList.GetList().Contains(pl))
            {
                routeList.GetList().Remove(pl);
                pl.ClearSelection();
                pl.ResetSize();
            }
            else
            {
                routeList.GetList().Add(pl);
                pl.SetSelection();
                pl.ResetSize();
            }

        }

        private void AddRemovePlaqueFromListNo_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Filter1", true);
        }

        private void FullInfoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "FullInfo1", true);
        }

        private void DoneSelectingRouteStartPoint(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectRoute1", true);
        }

        private void DoneSelectingRoute(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "DefineEndPoint2", true);
        }

        private void EditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaque", true);
            UpdatePlaqueVisibilty();
            route.FadeOutRouteLine();
            routeState = RouteState.SelectStartPoint;
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "RouteSummary1", true);
        }

        private void RouteSummaryEditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaque", true);
            UpdatePlaqueVisibilty();
            route.FadeOutRouteLine();
            routeState = RouteState.SelectStartPoint;
        }

        private void RouteSummaryGoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnly", true);
            UpdatePlaqueVisibilty();
            routeState = RouteState.Travelling;
        }

        private void SplashScreenButton_Click(object sender, RoutedEventArgs e)
        {
            SplashScreen.Opacity = 0.0;
            SplashScreen.IsHitTestVisible = false;
            SplashScreenButton.IsHitTestVisible = false;
            VisualStateManager.GoToState(this, "MainMenu1", true);
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaque", true);
            routeState = RouteState.SelectStartPoint;
        }

        private void MainMenuFilterButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Filter1", true);
        }

        void CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            switch (e.NewState.Name)
            {
                case "MainMenu1":
                    {
                        routeState = RouteState.Normal;
                        break;
                    }
                case "SelectStartPlaque":
                    {
                        routeState = RouteState.SelectStartPoint;

                        if (routeList.GetStartPoint() == null)
                        {
                            Done1.Opacity = 0.50;
                            SelectPlaquesButton.IsHitTestVisible = false;
                        }
                        break;
                    }
                case "SelectRoute1":
                    {
                        routeState = RouteState.SelectRoute;

                        AddOrRemovePlaque.Opacity = 0.0;
                        AddOrRemovePlaqueYesNo.Opacity = 0.0;
                        SelectPlaqueYes.IsHitTestVisible = false;
                        SelectPlaqueNo.IsHitTestVisible = false;

                        break;
                    }
                case "DefineEndPoint2":
                    {
                        routeState = RouteState.SelectEndPoint;

                        if (routeList.GetEndPoint() == null)
                        {
                            Done.Opacity = 0.50;
                            DoneEndPointButton.IsHitTestVisible = false;
                        }
                        break;
                    }
                case "RouteSummary1":
                    {
                        routeState = RouteState.Travelling;
                        journey.StartJourney(summary);
                        journey.FillInDetails();
                        myMap.Center = currentLocation.GetLocation();
                        myMap.ZoomLevel = 16;
                        break;
                    }
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            switch (routeState)
            {
                case RouteState.SelectStartPoint:
                    {
                        VisualStateManager.GoToState(this, "MainMenu1", true);
                        break;
                    }
                case RouteState.SelectRoute:
                    {
                        VisualStateManager.GoToState(this, "SelectStartPlaque", true);
                        break;
                    }
                case RouteState.SelectEndPoint:
                    {
                        VisualStateManager.GoToState(this, "SelectRoute1", true);
                        break;
                    }
                case RouteState.Travelling:
                    {
                        VisualStateManager.GoToState(this, "MainMenu1", true);
                        break;
                    }
                default:
                    {
                        VisualStateManager.GoToState(this, "MainMenu1", true);
                        break;
                    }
            }
            e.Cancel = true;
            base.OnBackKeyPress(e);

        }

        private void ArtsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Arts.Opacity == 1.0)
                Arts.Opacity = 0.4;
            else
                Arts.Opacity = 1.0;

            UpdateFilter(Arts.Opacity == 1.0, Politics.Opacity == 1.0, Science.Opacity == 1.0, Exploration.Opacity == 1.0);
        }

        private void PoliticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Politics.Opacity == 1.0)
                Politics.Opacity = 0.4;
            else
                Politics.Opacity = 1.0;

            UpdateFilter(Arts.Opacity == 1.0, Politics.Opacity == 1.0, Science.Opacity == 1.0, Exploration.Opacity == 1.0);
        }

        private void ScienceButton_Click(object sender, RoutedEventArgs e)
        {
            if (Science.Opacity == 1.0)
                Science.Opacity = 0.4;
            else
                Science.Opacity = 1.0;

            UpdateFilter(Arts.Opacity == 1.0, Politics.Opacity == 1.0, Science.Opacity == 1.0, Exploration.Opacity == 1.0);
        }

        private void ExplorationButton_Click(object sender, RoutedEventArgs e)
        {
            if (Exploration.Opacity == 1.0)
                Exploration.Opacity = 0.4;
            else
                Exploration.Opacity = 1.0;

            UpdateFilter(Arts.Opacity == 1.0, Politics.Opacity == 1.0, Science.Opacity == 1.0, Exploration.Opacity == 1.0);
        }

        private void UpdateFilter(bool arts, bool politics, bool science, bool exploration)
        {
            for (int i = 0; i < plaques.Count; i++)
            {
                if (plaques[i].Info.filter == PlaqueInfo.FilterCategory.Arts)
                {
                    plaques[i].Visible = arts;
                }
                if (plaques[i].Info.filter == PlaqueInfo.FilterCategory.Politics)
                {
                    plaques[i].Visible = politics;
                }
                if (plaques[i].Info.filter == PlaqueInfo.FilterCategory.Science)
                {
                    plaques[i].Visible = science;
                }
                if (plaques[i].Info.filter == PlaqueInfo.FilterCategory.Exploration)
                {
                    plaques[i].Visible = exploration;
                }
            }
            UpdatePlaqueVisibilty();
        }

        private void RemoveAllPinsExceptCurrentRoute()
        {
            // remove old pins
            pinLayer.Children.Clear();

            List<Plaque> route = routeList.GetList();

            pinLayer.Children.Add(routeList.GetStartPoint().Pin);
            for (int i = 0; i < plaques.Count; i++)
            {
                if (route.Contains(plaques[i]))
                {
                    pinLayer.Children.Add(plaques[i].Pin);
                }
            }
            pinLayer.Children.Add(routeList.GetEndPoint().Pin);
        }

        private void UpdatePlaqueVisibilty()
        {
            // remove old pins
            pinLayer.Children.Clear();

            for (int i = 0; i < plaques.Count; i++)
            {
                if (plaques[i].Visible || plaques[i].Selected)
                    pinLayer.Children.Add(plaques[i].Pin);
            }

        }
    }
}
