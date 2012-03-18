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
            SelectRoute,
            SelectEndPoint
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
            BottomBar.Opacity = 0.0;
            routeState = RouteState.Normal;
            UpdatePlaqueVisibilty();
            VisualStateManager.GoToState(this, "UserSectedRoute", true);
        }

        private void ExitFilterMenu_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 1.0;
            VisualStateManager.GoToState(this, "MapOnly", true);
        }

        private void ExitUserSelectedRoute_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 1.0;
            VisualStateManager.GoToState(this, "MapOnly", true);
        }

        private void ExitFullInfo_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 1.0;
            VisualStateManager.GoToState(this, "MapOnly", true);
        }

        private void DoneEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (routeList.GetEndPoint() != null)
            {
                BottomBar.Opacity = 0.0;
                VisualStateManager.GoToState(this, "SelectRoute3", true);
                DrawRoute();
                routeState = RouteState.Normal;
            }
        }

        private void CalcRoute_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 0.0;
            VisualStateManager.GoToState(this, "SelectRoute3", true);
            ClearSelectedPins();
            routeList.Clear();
            routeState = RouteState.SelectRoute;
            //DrawRoute();
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 0.0;
            VisualStateManager.GoToState(this, "Filter1", true);
        }

        private void FullInfoButton_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 0.0;
            VisualStateManager.GoToState(this, "FullInfo1", true);
        }

        private void DoneSelectingRoute(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 0.0;
            routeState = RouteState.SelectEndPoint;
            VisualStateManager.GoToState(this, "DefineEndPoint2", true);
        }

        private void EditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 0.0;
            VisualStateManager.GoToState(this, "SelectRoute3", true);
            UpdatePlaqueVisibilty();
            routeState = RouteState.SelectRoute;
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Opacity = 1.0;
            VisualStateManager.GoToState(this, "MapOnly", true);
            routeState = RouteState.Normal;
            myMap.Center = currentLocation.GetLocation();
            myMap.ZoomLevel = 17;
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
            for (int i = 0; i < plaques.Count; i++)
            {
                if (route.Contains(plaques[i]))
                {
                    pinLayer.Children.Add(plaques[i].Pin);
                }
            }
            if (!route.Contains(routeList.GetEndPoint()))
            {
                // and add the end point if its not part of the main list
                pinLayer.Children.Add(routeList.GetEndPoint().Pin);
            }
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
