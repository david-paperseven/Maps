﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;

using Microsoft.Phone.Shell;

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

        public enum RouteMode
        {
            None,
            QuickStart,
            UserSelected,
            AppGenRoute,
            Discovery
        }

        bool onMainMenu = true;

        public MainPage()
        {
            InitializeComponent();
            InitializeWatcher();
            SaveState.Instance.route = new Route(((ApplicationIdCredentialsProvider)myMap.CredentialsProvider).ApplicationId, myMap, this);
            SaveState.Instance.summary = new Summary();
            SaveState.Instance.currentLocation = new CurrentLocation(myMap);
            SaveState.Instance.routeList = new RouteList();
            SaveState.Instance.plaqueLoader = new PlaqueLoader();
            SaveState.Instance.plaqueInfoList = SaveState.Instance.plaqueLoader.Load();
            SaveState.Instance.HaveWeCentred = false;
            myMap.ZoomLevel = 15;
            SaveState.Instance.journey = new Journey(this);

            SaveState.Instance.routeState = MainPage.RouteState.Normal;//DEBUG!
            SaveState.Instance.CurrentVisualState = "SplashScreenState";
            SaveState.Instance.mainpage = this;

            Initializeplaques();
            DebugRouteTimer();
            PlaqueFlashTimer();


            // Visual States are always on the first child of the control template  
            FrameworkElement element = VisualTreeHelper.GetChild(MainPageElement, 0) as FrameworkElement;
            VisualStateGroup group = FindVisualState(element, "UserSelectedRoutesMenu");
            group.CurrentStateChanged += new EventHandler<VisualStateChangedEventArgs>(CurrentStateChanged);

            RouteSummaryGoButton.IsHitTestVisible = false;

            PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            VisualStateManager.GoToState(this, SaveState.Instance.CurrentVisualState, true);
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

        private void Initializeplaques()
        {
            SaveState.Instance.pinlayer = new MapLayer();
            myMap.Children.Add(SaveState.Instance.pinlayer);

            SaveState.Instance.textLayer = new MapLayer();
            myMap.Children.Add(SaveState.Instance.textLayer);

            SaveState.Instance.plaques = new List<Plaque>();
            for (int i = 0; i < SaveState.Instance.plaqueInfoList.Count; i++)
            {
                Plaque plaque = new Plaque(SaveState.Instance.plaqueInfoList[i], this, SaveState.Instance.textLayer);
                SaveState.Instance.plaques.Add(plaque);
                SaveState.Instance.pinlayer.Children.Add(plaque.Pin);
            }

            GeoCoordinate location = new GeoCoordinate(51.511397, -0.128263);
            SaveState.Instance.currentLocation.SetLocation(location);
            myMap.Children.Add(SaveState.Instance.currentLocation.Pin);

        }

        private void InitializeWatcher()
        {
            SaveState.Instance.watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy;
            SaveState.Instance.watcher.MovementThreshold = 5.0f; // meters of change before "PositionChanged"
            // wire up event handlers
            SaveState.Instance.watcher.StatusChanged += new
            EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            SaveState.Instance.watcher.PositionChanged += new
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
                    if (SaveState.Instance.watcher.Permission == GeoPositionPermission.Denied)
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

        int counter = 0;
        private void DebugRouteTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();

            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    if (SaveState.Instance.routeState == RouteState.Travelling)
                    {
                        if (counter < SaveState.Instance.journeysaver.journeypoints.Count)
                        {
                            JourneyPoint point = SaveState.Instance.journeysaver.journeypoints[counter++];
                            ChangedLocation(point.coord);
                        }
                    }

                };


            timer.Interval = new TimeSpan(0, 0, 0,0,900);
            timer.Start();
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            GeoCoordinate location = new GeoCoordinate(51.511397, -0.128263);
            ChangedLocation(location);

            //ChangedLocation(e.Position.Location);
        }

        void ChangedLocation(GeoCoordinate location)
        {
            if (SaveState.Instance.routeState == RouteState.Travelling)
            {
                GeoCoordinate currentLocation = SaveState.Instance.currentLocation.GetLocation();
                double distance = currentLocation.GetDistanceTo(location);
                SaveState.Instance.journey.DistanceTravelled += distance / 1000.0;
            }

            SaveState.Instance.currentLocation.SetLocation(location);
            //SaveState.Instance.journeysaver.Add(location); not recording right now

            if (SaveState.Instance.routeState == RouteState.Travelling)
            {
                SaveState.Instance.journey.PlaqueUnlock();
                SaveState.Instance.journey.FillInDetails();
            }

            if (SaveState.Instance.HaveWeCentred == false)
            {
                SaveState.Instance.HaveWeCentred = true;
                myMap.Center = Bounds.GetCentreWithOffset(SaveState.Instance.currentLocation.GetLocation(),(int)myMap.ZoomLevel);
            }
        }


        void startLocServInBackground()
        {
            SaveState.Instance.watcher.TryStart(true, TimeSpan.FromMilliseconds(60000));
        }

        public void ClearSelectedPins()
        {
            foreach (Plaque p in SaveState.Instance.plaques)
            {
                if (p.Selected == true)
                {
                    p.ClearSelection();
                }
            }
        }

        void DrawRoute()
        {
            System.Collections.ObjectModel.ObservableCollection<Waypoint> waypoints = SaveState.Instance.routeList.GetFinalList(SaveState.Instance.currentLocation.GetLocation());

            if (waypoints.Count > 1)
            {
                SaveState.Instance.summary.NumPlaques = waypoints.Count - 1; // don't count the current position 
                CalculatingPage();
                SaveState.Instance.route.CalculateRoute(waypoints,SaveState.Instance.summary);
                RemoveAllPinsExceptCurrentRoute();
                VisualStateManager.GoToState(this, "DistanceSummaryState", true);
            }
        }

        public void CalculatingPage()
        {
            RouteMode2.Text = "Calculating route...";
            NumKms.Text = "";
            km.Text = "";
            ApproxTime.Text = "";
            NumPlaques.Text = "";
            plaquestext.Text = "";

            DistanceKMS.Visibility = System.Windows.Visibility.Collapsed;
            Approx_time.Visibility = System.Windows.Visibility.Collapsed;
            NumPlaquesText.Visibility = System.Windows.Visibility.Collapsed;
            
            Edit_Route___Go.Visibility = System.Windows.Visibility.Collapsed;
            EditRouteButton.IsHitTestVisible = false;
            GoButton.IsHitTestVisible = false;
        }

        public void SummaryLoaded()
        {
            SetRouteModeText();
            NumKms.Text = SaveState.Instance.summary.Distance.ToString();
            km.Text = "km";
            ApproxTime.Text = SaveState.Instance.summary.GetTime();
            NumPlaques.Text = SaveState.Instance.summary.NumPlaques.ToString();
            plaquestext.Text = "plaques";

            DistanceKMS.Visibility = System.Windows.Visibility.Visible;
            Approx_time.Visibility = System.Windows.Visibility.Visible;
            NumPlaquesText.Visibility = System.Windows.Visibility.Visible;
            
            Edit_Route___Go.Visibility = System.Windows.Visibility.Visible;
            EditRouteButton.IsHitTestVisible = true;
            GoButton.IsHitTestVisible = true;
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            if (SaveState.Instance.routeState == RouteState.Travelling)
            {
                VisualStateManager.GoToState(this, "RouteSummaryState", true);
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
            if (SaveState.Instance.routeList.GetEndPoint() != null)
            {
                // DOESN'T LOOK RIGHT??
                VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
                DrawRoute();
                SaveState.Instance.routeState = RouteState.Normal;
            }
        }

        private void SelectStartPlaque_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            ClearSelectedPins();
        }

        private void AddRemovePlaqueFromListYes_Click(object sender, RoutedEventArgs e)
        {
            Plaque pl = SaveState.Instance.routeList.GetCurrentPoint();
            if (SaveState.Instance.routeList.GetList().Contains(pl))
            {
                SaveState.Instance.routeList.GetList().Remove(pl);
                pl.ClearSelection();
                pl.ResetSize();
            }
            else
            {
                SaveState.Instance.routeList.GetList().Add(pl);
                pl.SetGreen();
                //pl.SetSelection();
                pl.ResetSize();
                SaveState.Instance.flashplaque = pl;
                SaveState.Instance.flashtimer = DateTime.Now;
                AddOrRemovePlaqueYesNo.Opacity = 0.4;

            }

            VisualStateManager.GoToState(RoutePlaqueNameInfo, "Start", true);
        }

        private void AddRemovePlaqueFromListNo_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "FilterState", true);
        }

        private void DoneSelectingRouteStartPoint(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(StartPlaqueNameInfo, "Start", true);
            VisualStateManager.GoToState(this, "SelectRouteState", true);
        }

        private void DoneSelectingRoute(object sender, RoutedEventArgs e)
        {
            Plaque pl = SaveState.Instance.routeList.GetCurrentPoint();
            if (pl != null)
            {
                pl.ResetSize();
            }
            VisualStateManager.GoToState(RoutePlaqueNameInfo, "Start", true);
            VisualStateManager.GoToState(this, "DefineEndPointState", true);
        }
        /*
        private void EditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            UpdatePlaqueVisibilty();
            SaveState.Instance.route.FadeOutRouteLine();
            SaveState.Instance.routeState = RouteState.SelectStartPoint;
        }
        */
        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(EndPlaqueNameInfo, "Start", true);
            VisualStateManager.GoToState(this, "RouteSummaryState", true);

            RouteSummaryGoButton.IsHitTestVisible = true;
            SaveState.Instance.routeState = RouteState.Travelling;
            SaveState.Instance.journey.FillInDetails();
            SaveState.Instance.journey.StartJourney(SaveState.Instance.summary);
            myMap.Center = Bounds.GetCentreWithOffset(SaveState.Instance.currentLocation.GetLocation(), (int)myMap.ZoomLevel);

        }

        private void RouteSummaryEditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            UpdatePlaqueVisibilty();
            SaveState.Instance.route.FadeOutRouteLine();
            RouteSummaryGoButton.IsHitTestVisible = false;
            SaveState.Instance.routeState = RouteState.SelectStartPoint;
             */
        }

        bool clearrouteoption = false;
        public void ClearRouteYes()
        {
            if (clearrouteoption)
            {
                clearrouteoption = false;
                VisualStateManager.GoToState(ClearRouteYesNo, "Start", true);

                ClearRoute();

                switch (SaveState.Instance.routeMode)
                {
                    case RouteMode.QuickStart:
                        {
                            VisualStateManager.GoToState(this, "QuickStartState", true);
                            SaveState.Instance.routeState = MainPage.RouteState.Normal;
                            break;
                        }
                    case RouteMode.UserSelected:
                        {
                            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
                            SaveState.Instance.routeState = MainPage.RouteState.SelectStartPoint;
                            break;
                        }
                    case RouteMode.AppGenRoute:
                        {
                            VisualStateManager.GoToState(this, "AppGenEndPointState", true);
                            SaveState.Instance.routeState = MainPage.RouteState.SelectEndPoint;
                            break;
                        }
                    case RouteMode.Discovery:
                        {
                            VisualStateManager.GoToState(this, "AppGenEndPointState", true);
                            SaveState.Instance.routeState = MainPage.RouteState.SelectEndPoint;
                            break;
                        }
                }
            }
        }

        public void ClearRouteNo()
        {
            clearrouteoption = false;
            VisualStateManager.GoToState(ClearRouteYesNo, "Start", true);
            RouteSummaryGoButton.IsHitTestVisible = true;
        }

        private void DistanceSummaryBackButton_Click(object sender, RoutedEventArgs e)
        {
            clearrouteoption = true;
            ClearRouteYes();
        }

        private void RouteSummaryClearRouteButton_Click(object sender, RoutedEventArgs e)
        {
            clearrouteoption = true;
            RouteSummaryGoButton.IsHitTestVisible = false;
            ClearRouteYesNo.SelectRoutePlaqueName.Text = "Are you sure?";
            ClearRouteYesNo.SelectRoutePlaqueDateAndCategory.Text = "Yes / No";

            VisualStateManager.GoToState(ClearRouteYesNo, "SlideUp", true);
            ClearRouteYesNo.PlaqueExtraInfo.IsHitTestVisible = true;
        }
        

        private void RouteSummaryGoButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MapOnlyState", true);
            //UpdatePlaqueVisibilty();
            RouteSummaryGoButton.IsHitTestVisible = false;
        }

        private void SplashScreenButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "MainMenuState", true);
        }

        private void MainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "SelectStartPlaqueState", true);
            SaveState.Instance.routeState = RouteState.SelectStartPoint;
        }

        private void MainMenuFilterButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "FilterState", true);
        }

        private void UnlockedPlaqueButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "FullInfoState", true);
        }

        private void QuickStartPlotRouteButton_Click(object sender, RoutedEventArgs e)
        {
            SaveState.Instance.summary.NumPlaques = 1; 
            CalculatingPage();
            System.Collections.ObjectModel.ObservableCollection<Waypoint> waypoints = new System.Collections.ObjectModel.ObservableCollection<Waypoint>();

            Waypoint mywp = new Waypoint();
            mywp.Location = SaveState.Instance.currentLocation.Location;
            waypoints.Add(mywp);

            Waypoint sp = new Waypoint();
            sp.Location = SaveState.Instance.routeList.GetStartPoint().Info.location;
            waypoints.Add(sp);

            SaveState.Instance.route.CalculateRoute(waypoints, SaveState.Instance.summary);
            //RemoveAllPinsExceptCurrentRoute();
            VisualStateManager.GoToState(QuickStartNameInfo, "Start", true);
            VisualStateManager.GoToState(this, "DistanceSummaryState", true);
        }

        private void QuickStartButton_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "QuickStartState", true);
        }

        public Plaque FindNearestPlaque(GeoCoordinate currentLocation)
        {
            double closestdistance = 1000000000.0;
            Plaque nearestplaque=null;
            foreach (Plaque p in SaveState.Instance.plaques)
            {
                double distance = currentLocation.GetDistanceTo(p.GetLocation());
                if (distance < closestdistance)
                {
                    closestdistance = distance;
                    nearestplaque = p;
                }
            }
            return nearestplaque;
        }

        private void DiscoveryModeButton_Click(object sender, RoutedEventArgs e)
        {
            SaveState.Instance.routeMode = RouteMode.Discovery;
            RouteMode3.Text = "Discovery Mode";
            VisualStateManager.GoToState(this, "AppGenEndPointState", true);
        }

        private void AppGeneratedRouteButton_Click(object sender, RoutedEventArgs e)
        {
            RouteMode3.Text = "App Generated Route";
            SaveState.Instance.routeMode = RouteMode.AppGenRoute;
            VisualStateManager.GoToState(this, "AppGenEndPointState", true);
        }

        private void AppGenRouteEndPointDone_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(AppGenEndPlaqueNameInfo, "Start", true);
            VisualStateManager.GoToState(this, "AppGenParametersState", true);
        }

        private void ParametersDoneButton_Click(object sender, RoutedEventArgs e)
        {
            SaveState.Instance.plaquedistance.SortPlaques();

            SaveState.Instance.summary.NumPlaques = SaveState.Instance.appgenparameters.NumPlaques;
           
            CalculatingPage();
            
            System.Collections.ObjectModel.ObservableCollection<Waypoint> waypoints = new System.Collections.ObjectModel.ObservableCollection<Waypoint>();

            Waypoint mywp = new Waypoint();
            mywp.Location = SaveState.Instance.currentLocation.Location;
            waypoints.Add(mywp);

            SaveState.Instance.routeList.Clear();
            int count = 0;
            foreach (PD pd in SaveState.Instance.plaquedistance.GetList())
            {
                if (pd.plaque != SaveState.Instance.routeList.GetEndPoint())
                {
                    Waypoint sp = new Waypoint();
                    sp.Location = pd.plaque.GetLocation();
                    waypoints.Add(sp);
                    SaveState.Instance.routeList.Add(pd.plaque); // add them to the routlist so that when we clear the pins it leaves the route left
                    count++;
                    if (count == SaveState.Instance.appgenparameters.NumPlaques)
                    {
                        break;
                    }
                }
            }

            if (SaveState.Instance.routeList.GetEndPoint()!=null)
            {
                Waypoint sp = new Waypoint();
                sp.Location = SaveState.Instance.routeList.GetEndPoint().Info.location;
                waypoints.Add(sp);
            }

            SaveState.Instance.route.CalculateRoute(waypoints, SaveState.Instance.summary);
            RemoveAllPinsExceptCurrentRoute();
            VisualStateManager.GoToState(this, "DistanceSummaryState", true); 
            

        }

        void ClearRoute()
        {
            SaveState.Instance.routeList.Clear();
            SaveState.Instance.route.ClearRouteLine();
            ClearSelectedPins();
            UpdatePlaqueVisibilty();
        }

        void CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            onMainMenu = false;
            SaveState.Instance.CurrentVisualState = e.NewState.Name;

            switch (e.NewState.Name)
            {
                case "FullInfoState":
                    {
                        break;
                    }
                case "UnlockedPlaqueState":
                    {
                        RouteSummaryGoButton.IsHitTestVisible = false;
                        break;
                    }
                case "MainMenuState":
                    {
                        ClearRoute();
                        SaveState.Instance.routeState = RouteState.Normal;
                        RouteSummaryGoButton.IsHitTestVisible = false;
                        onMainMenu = true;
                        break;
                    }
                case "SelectStartPlaqueState":
                    {
                        SaveState.Instance.routeState = RouteState.SelectStartPoint;
                        SaveState.Instance.routeMode = RouteMode.UserSelected;
                        SetRouteModeText();
                        if (SaveState.Instance.routeList.GetStartPoint() == null)
                        {
                            Done1.Opacity = 0.50;
                            SelectPlaquesButton.IsHitTestVisible = false;
                        }
                        break;
                    }
                case "SelectRouteState":
                    {
                        SaveState.Instance.routeState = RouteState.SelectRoute;

                        AddOrRemovePlaque.Text = "Select the plaques to visit";
                        AddOrRemovePlaque.Opacity = 1.0;
                        AddOrRemovePlaqueYesNo.Opacity = 0.0;
                        SelectPlaqueYes.IsHitTestVisible = false;

                        VisualStateManager.GoToState(EndPlaqueNameInfo, "Start", true); 
                        break;
                    }
                case "AppGenEndPointState":
                    {
                        SaveState.Instance.routeState = RouteState.SelectEndPoint;
                        SetRouteModeText();
                        break;
                    }
                case "DefineEndPointState":
                    {
                        SaveState.Instance.routeState = RouteState.SelectEndPoint;

                        if (SaveState.Instance.routeList.GetEndPoint() == null)
                        {
                            Done.Opacity = 0.50;
                            DoneEndPointButton.IsHitTestVisible = false;
                        }
                        break;
                    }
                case "RouteSummaryState":
                    {
                        break;
                    }
                case "QuickStartState":
                    {
                        Plaque nearest = FindNearestPlaque(SaveState.Instance.currentLocation.Location);
                        SaveState.Instance.routeList.SetStartPoint(nearest);
                        nearest.SetGreen();

                        LocationRect rect = Bounds.GetRect(nearest.GetLocation(), SaveState.Instance.currentLocation.Location);
                        // Set the map view using the rectangle which bounds the rendered route.
                        myMap.SetView(rect);

                        QuickStartNameInfo.SelectRoutePlaqueName.Text = nearest.Info.GetName();
                        QuickStartNameInfo.SelectRoutePlaqueDateAndCategory.Text = "(" + nearest.Info.date + ") " + nearest.Info.info1;

                        SaveState.Instance.routeMode = RouteMode.QuickStart;
                        SetRouteModeText();
                        VisualStateManager.GoToState(QuickStartNameInfo, "SlideUp", true);
                        break;
                    }
            }

            if (e.NewState.Name != "SplashScreenState")
            {
                SplashScreen.Opacity = 0.0;
                SplashScreen.IsHitTestVisible = false;
                SplashScreenButton.IsHitTestVisible = false;            
            }
        }
        
        private void SetRouteModeText()
        {
            switch (SaveState.Instance.routeMode)
            {
                case RouteMode.QuickStart:
                    {
                        RouteMode1.Text = "Quick Start";
                        RouteMode2.Text = "Quick Start";
                        break;
                    }
                case RouteMode.UserSelected:
                    {
                        RouteMode1.Text = "User Selected Route";
                        RouteMode2.Text = "User Selected Route";
                        break;
                    }
                case RouteMode.AppGenRoute:
                    {
                        RouteMode1.Text = "App Generated Route";
                        RouteMode2.Text = "App Generated Route";
                        break;
                    }
                case RouteMode.Discovery:
                    {
                        RouteMode1.Text = "Discovery Mode";
                        RouteMode2.Text = "Discovery Mode";
                        break;
                    }
            }
        }

        private void PlaqueFlashTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();

            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    if (SaveState.Instance.flashplaque != null)
                    {
                        TimeSpan elapsedTime = DateTime.Now - SaveState.Instance.flashtimer;
                        if (elapsedTime.TotalMilliseconds > 150)
                        {
                            SaveState.Instance.flashplaque.SetSelection();
                            SaveState.Instance.flashplaque = null;
                            AddOrRemovePlaque.Text = "Select the plaques to visit";
                            AddOrRemovePlaque.Opacity = 1.0;
                            AddOrRemovePlaqueYesNo.Opacity = 0.0;

                            //AddOrRemovePlaqueYesNo.Opacity = 1.0;
                        }

                    }
                };


            timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            timer.Start();
        }


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            //base.OnBackKeyPress(e);

            if (onMainMenu) // time to exit
            {
                base.OnBackKeyPress(e);
                return;
            }

            e.Cancel = true;

            switch (SaveState.Instance.routeMode)
            {
                case RouteMode.QuickStart:
                    {
                        VisualStateManager.GoToState(QuickStartNameInfo, "Start", true); 
                        VisualStateManager.GoToState(this, "MainMenuState", true);
                        break;
                    }
                case RouteMode.UserSelected:
                    {
                        switch (SaveState.Instance.routeState)
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
                                    if (SaveState.Instance.routeList.GetEndPoint() != null)
                                    {
                                        SaveState.Instance.routeList.GetEndPoint().ClearSelection();
                                    }
                                    

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
                        break;
                    }
                default:
                    {
                        VisualStateManager.GoToState(AppGenEndPlaqueNameInfo, "Start", true);

                        VisualStateManager.GoToState(this, "MainMenuState", true);
                        break;
                    }
            }

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
            for (int i = 0; i < SaveState.Instance.plaques.Count; i++)
            {
                if (SaveState.Instance.plaques[i].Info.filter == PlaqueInfo.FilterCategory.Arts)
                {
                    SaveState.Instance.plaques[i].Visible = arts;
                }
                if (SaveState.Instance.plaques[i].Info.filter == PlaqueInfo.FilterCategory.Politics)
                {
                    SaveState.Instance.plaques[i].Visible = politics;
                }
                if (SaveState.Instance.plaques[i].Info.filter == PlaqueInfo.FilterCategory.Science)
                {
                    SaveState.Instance.plaques[i].Visible = science;
                }
                if (SaveState.Instance.plaques[i].Info.filter == PlaqueInfo.FilterCategory.Exploration)
                {
                    SaveState.Instance.plaques[i].Visible = exploration;
                }
            }
            UpdatePlaqueVisibilty();
        }

        private void RemoveAllPinsExceptCurrentRoute()
        {
            // remove old pins
            SaveState.Instance.pinlayer.Children.Clear();

            List<Plaque> route = SaveState.Instance.routeList.GetList();

            if (SaveState.Instance.routeList.GetStartPoint() != null)
            {
                SaveState.Instance.pinlayer.Children.Add(SaveState.Instance.routeList.GetStartPoint().Pin);
            }
            for (int i = 0; i < SaveState.Instance.plaques.Count; i++)
            {
                if (route.Contains(SaveState.Instance.plaques[i]))
                {
                    SaveState.Instance.pinlayer.Children.Add(SaveState.Instance.plaques[i].Pin);
                }
            }
            if (SaveState.Instance.routeList.GetStartPoint() != SaveState.Instance.routeList.GetEndPoint())
            {
                if (!route.Contains(SaveState.Instance.routeList.GetEndPoint()))
                {
                    SaveState.Instance.pinlayer.Children.Add(SaveState.Instance.routeList.GetEndPoint().Pin);
                }
            }
        }

        private void UpdatePlaqueVisibilty()
        {
            // remove old pins
            SaveState.Instance.pinlayer.Children.Clear();

            for (int i = 0; i < SaveState.Instance.plaques.Count; i++)
            {
                if (SaveState.Instance.plaques[i].Visible || SaveState.Instance.plaques[i].Selected)
                    SaveState.Instance.pinlayer.Children.Add(SaveState.Instance.plaques[i].Pin);
            }

        }

    }
}
