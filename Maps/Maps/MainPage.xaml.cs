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
        public CurrentLocation currentLocation;
        Journey journey;
        public RouteList routeList;
        public RouteState routeState;
        bool HaveWeCentred;

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

            HaveWeCentred = false;

            myMap.ZoomLevel = 15;
            routeState = RouteState.Normal;

            journey = new Journey(this);

            // Visual States are always on the first child of the control template  
            FrameworkElement element = VisualTreeHelper.GetChild(MainPageElement, 0) as FrameworkElement;
            VisualStateGroup group = FindVisualState(element, "UserSelectedRoutesMenu");
            group.CurrentStateChanged += new EventHandler<VisualStateChangedEventArgs>(CurrentStateChanged);

            ShowFullInfoButton.Opacity = 0;
            ShowFullInfoButton.IsHitTestVisible = false; 
            RouteSummaryGoButton.IsHitTestVisible = false;
            VisualStateManager.GoToState(this, "SplashScreenState", true);
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
           // currentLocation.SetLocation(e.Position.Location);

            if (HaveWeCentred == false)
            {
                HaveWeCentred = true;
                myMap.Center = currentLocation.GetLocation();
            }
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
                CalculatingPage();
                route.CalculateRoute(waypoints,summary);
                RemoveAllPinsExceptCurrentRoute();
                VisualStateManager.GoToState(this, "DistanceSummaryState", true);
            }
        }

        public void CalculatingPage()
        {
            User_Selected_Route.Text = "Calculating route...";
            NumKms.Text = "";
            ApproxTime.Text = "";
            NumPlaques.Text = "";

            km.Visibility = System.Windows.Visibility.Collapsed;
            Approx_time.Visibility = System.Windows.Visibility.Collapsed;
            plaquestext.Visibility = System.Windows.Visibility.Collapsed;
            
            Edit_Route___Go.Visibility = System.Windows.Visibility.Collapsed;
            EditRouteButton.IsHitTestVisible = false;
            GoButton.IsHitTestVisible = false;
        }

        public void SummaryLoaded()
        {
            User_Selected_Route.Text = "User Selected Route";
            NumKms.Text = summary.Distance.ToString();
            ApproxTime.Text = summary.GetTime();
            NumPlaques.Text = summary.NumPlaques.ToString();

            km.Visibility = System.Windows.Visibility.Visible;
            Approx_time.Visibility = System.Windows.Visibility.Visible;
            plaquestext.Visibility = System.Windows.Visibility.Visible;
            
            Edit_Route___Go.Visibility = System.Windows.Visibility.Visible;
            EditRouteButton.IsHitTestVisible = true;
            GoButton.IsHitTestVisible = true;
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
//            routeState = RouteState.Normal;
            UpdatePlaqueVisibilty();

            if (routeState == RouteState.Travelling)
            {
                VisualStateManager.GoToState(this, "RouteSummaryState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
                routeState = RouteState.SelectStartPoint;
            }
        }

        private void ExitFilterMenu_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnlyState", true);
        }

        private void ExitUserSelectedRoute_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnlyState", true);
        }

        private void ExitFullInfo_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnlyState", true);
        }

        private void DoneEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (routeList.GetEndPoint() != null)
            {
                // DOESN'T LOOK RIGHT??
                VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
                DrawRoute();
                routeState = RouteState.Normal;
            }
        }

        private void SelectStartPlaque_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
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
            VisualStateManager.GoToState(this, "FilterState", true);
        }

        private void FullInfoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "FullInfoState", true);
        }

        private void DoneSelectingRouteStartPoint(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectRouteState", true);
        }

        private void DoneSelectingRoute(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "DefineEndPointState", true);
        }

        private void EditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            UpdatePlaqueVisibilty();
            route.FadeOutRouteLine();
            routeState = RouteState.SelectStartPoint;
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "RouteSummaryState", true);
        }

        private void RouteSummaryEditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            UpdatePlaqueVisibilty();
            route.FadeOutRouteLine();
            RouteSummaryGoButton.IsHitTestVisible = false;
            routeState = RouteState.SelectStartPoint;
        }

        private void RouteSummaryGoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnlyState", true);
            UpdatePlaqueVisibilty();
            routeState = RouteState.Travelling;
            RouteSummaryGoButton.IsHitTestVisible = false;
        }

        private void SplashScreenButton_Click(object sender, RoutedEventArgs e)
        {
            SplashScreen.Opacity = 0.0;
            SplashScreen.IsHitTestVisible = false;
            SplashScreenButton.IsHitTestVisible = false;
            VisualStateManager.GoToState(this, "MainMenuState", true);
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            routeState = RouteState.SelectStartPoint;
        }

        private void MainMenuFilterButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "FilterState", true);
        }

        bool fullinfostate;
        void CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            fullinfostate = false;
            ShowFullInfoButton.Opacity = 0;
            ShowFullInfoButton.IsHitTestVisible = false;
            switch (e.NewState.Name)
            {
                case "FullInfoState":
                    {
                        ShowFullInfoButton.Opacity = 1;
                        ShowFullInfoButton.IsHitTestVisible = true;
                        fullinfostate = true;
                        break;
                    }
                case "MainMenuState":
                    {
                        routeList.Clear();
                        route.ClearRouteLine();
                        ClearSelectedPins();
                        UpdatePlaqueVisibilty();
                        routeState = RouteState.Normal;
                        RouteSummaryGoButton.IsHitTestVisible = false;
                        break;
                    }
                case "SelectStartPlaqueState":
                    {
                        routeState = RouteState.SelectStartPoint;
                        PlaqueExtraInfo.Opacity = 0;
                        if (routeList.GetStartPoint() == null)
                        {
                            Done1.Opacity = 0.50;
                            SelectPlaquesButton.IsHitTestVisible = false;
                        }
                        break;
                    }
                case "SelectRouteState":
                    {
                        routeState = RouteState.SelectRoute;

                        PlaqueExtraInfo1.Opacity = 0;
                        AddOrRemovePlaque.Text = "Select the plaques to visit";
                        AddOrRemovePlaque.Opacity = 1.0;
                        AddOrRemovePlaqueYesNo.Opacity = 0.0;
                        SelectPlaqueYes.IsHitTestVisible = false;
                        SelectPlaqueNo.IsHitTestVisible = false;

                        break;
                    }
                case "DefineEndPointState":
                    {
                        routeState = RouteState.SelectEndPoint;
                        EndPlaqueInfo.Opacity = 0.0;

                        if (routeList.GetEndPoint() == null)
                        {
                            Done.Opacity = 0.50;
                            DoneEndPointButton.IsHitTestVisible = false;
                        }
                        break;
                    }
                case "RouteSummaryState":
                    {
                        RouteSummaryGoButton.IsHitTestVisible = true;
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
                        VisualStateManager.GoToState(this, "MainMenuState", true);
                        break;
                    }
                case RouteState.SelectRoute:
                    {
                        VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
                        break;
                    }
                case RouteState.SelectEndPoint:
                    {
                        VisualStateManager.GoToState(this, "SelectRouteState", true);
                        break;
                    }
                case RouteState.Travelling:
                    {
                        VisualStateManager.GoToState(this, "MainMenuState", true);
                        break;
                    }
                default:
                    {
                        VisualStateManager.GoToState(this, "MainMenuState", true);
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
            if (routeList.GetStartPoint() != routeList.GetEndPoint())
            {
                if (!route.Contains(routeList.GetEndPoint()))
                {
                    pinLayer.Children.Add(routeList.GetEndPoint().Pin);
                }
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

        private void ShowFullInfoButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!fullinfostate)
        	    VisualStateManager.GoToState(this, "FullInfoState", true);
            else
                VisualStateManager.GoToState(this, "MapOnlyState", true);
        }

    }
}
