using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Device.Location;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;
using Microsoft.Phone.Controls.Maps.Platform;

using Maps.Helpers;
using Maps.RouteService;


namespace Maps.Helpers
{
    public class Route
    {
        string credentialsId;
        Map myMap;
        MainPage myMainPage;
        Summary mySummary;
        private readonly Dispatcher uiDispatcher;
        public event Action<RouteCalculationError> Error = delegate { };

        public Route()
        {
        }

        public Route(string Id, Map map, MainPage page, Dispatcher dispatcher)
        {
            credentialsId = Id;
            myMap = map;
            myMainPage = page;
            uiDispatcher = dispatcher;

        }
/*
        // This method makes the initial CalculateRoute asynchronous request using the results of the Geocode Service.
        public void CalculateRoute(System.Collections.ObjectModel.ObservableCollection<RouteService.Waypoint> waypoints, Summary summary)
        {
            // Create the service variable and set the callback method using the CalculateRouteCompleted property.
            RouteService.RouteServiceClient routeService = new RouteService.RouteServiceClient("BasicHttpBinding_IRouteService");
            routeService.CalculateRouteCompleted += new EventHandler<RouteService.CalculateRouteCompletedEventArgs>(client_CalculateRouteCompleted);

            // Set the token.
            RouteService.RouteRequest routeRequest = new RouteService.RouteRequest();
            routeRequest.Credentials = new Credentials();
            routeRequest.Credentials.ApplicationId = credentialsId;

            // Return the route points so the route can be drawn.
            routeRequest.Options = new RouteService.RouteOptions();
            routeRequest.Options.RoutePathType = RouteService.RoutePathType.Points;
            routeRequest.Options.Mode = TravelMode.Walking;

            // Set the waypoints of the route to be calculated using the Geocode Service results stored in the geocodeResults variable.
            routeRequest.Waypoints = waypoints;

            mySummary = summary;

            routeRequest.ExecutionOptions = new ExecutionOptions();
            routeRequest.ExecutionOptions.SuppressFaults = true;
            // Make the CalculateRoute asnychronous request.
            routeService.CalculateRouteAsync(routeRequest);
        }
*/

        public void CalculateRoute(System.Collections.ObjectModel.ObservableCollection<RouteService.Waypoint> waypoints, Summary summary)
        {
            try
            {
                var routeCalculator = new RouteCalculator(
                    credentialsId,
                    uiDispatcher,
                    result =>
                    {
                        SaveState.Instance.routedrawer.result = result.Result;

                        mySummary.Distance = result.Result.Summary.Distance;
                        mySummary.Time = result.Result.Summary.TimeInSeconds;

                        if (SaveState.Instance.routeMode == MainPage.RouteMode.Discovery)
                        {
                            SaveState.Instance.routedrawer.DrawUpToLeg(0);
                        }
                        else
                        {
                            SaveState.Instance.routedrawer.DrawEntireRoute();
                        }


                        myMainPage.SummaryLoaded();
                    },
                    error =>
                    {
                        VisualStateManager.GoToState(myMainPage, "NoInternetState", true);
                    }
                    );

                // Display an error message in case of fault.
                routeCalculator.Error += r => MessageBox.Show(r.Reason);

                mySummary = summary;
                // Start the route calculation asynchronously.
                routeCalculator.CalculateRoute(waypoints);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /*
        private void client_CalculateRouteCompleted(object sender, CalculateRouteCompletedEventArgs e)
        {
            if (e.Result.ResponseSummary.StatusCode == RouteService.ResponseStatusCode.Success)
            {
                // Raise the found event on the UI thread.
                uiDispatcher.BeginInvoke(() => routeService_CalculateRouteCompleted(e));
            }
            else
            {
                // Report route error.
                uiDispatcher.BeginInvoke(() => Error(new RouteCalculationError(e)));
            }
        }
        */

        // This is the callback method for the CalculateRoute request.
        private void routeService_CalculateRouteCompleted(RouteService.CalculateRouteCompletedEventArgs e)
        {
            try
            {
                // If the route calculate was a success and contains a route, then draw the route on the map.
                if ((e.Result.ResponseSummary.StatusCode == RouteService.ResponseStatusCode.Success) & (e.Result.Result.Legs.Count != 0))
                {
                    SaveState.Instance.routedrawer.result = e.Result.Result;

                    mySummary.Distance = e.Result.Result.Summary.Distance;
                    mySummary.Time = e.Result.Result.Summary.TimeInSeconds;

                    if (SaveState.Instance.routeMode == MainPage.RouteMode.Discovery)
                    {
                        SaveState.Instance.routedrawer.DrawUpToLeg(0);
                    }
                    else
                    {
                        SaveState.Instance.routedrawer.DrawEntireRoute();
                    }


                    myMainPage.SummaryLoaded();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
