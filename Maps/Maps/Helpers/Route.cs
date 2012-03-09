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
        MapLayer routeLayer;

        public Route(string Id, Map map, MainPage page)
        {
            credentialsId = Id;
            myMap = map;
            myMainPage = page;
            routeLayer = new MapLayer();
            myMap.Children.Add(routeLayer);
        }

        // This method makes the initial CalculateRoute asynchronous request using the results of the Geocode Service.
        public void CalculateRoute(System.Collections.ObjectModel.ObservableCollection<RouteService.Waypoint> waypoints, Summary summary)
        {
            // Create the service variable and set the callback method using the CalculateRouteCompleted property.
            RouteService.RouteServiceClient routeService = new RouteService.RouteServiceClient("BasicHttpBinding_IRouteService");
            routeService.CalculateRouteCompleted += new EventHandler<RouteService.CalculateRouteCompletedEventArgs>(routeService_CalculateRouteCompleted);

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

            // Make the CalculateRoute asnychronous request.
            routeService.CalculateRouteAsync(routeRequest);
        }

        // This is the callback method for the CalculateRoute request.
        private void routeService_CalculateRouteCompleted(object sender, RouteService.CalculateRouteCompletedEventArgs e)
        {

            // If the route calculate was a success and contains a route, then draw the route on the map.
            if ((e.Result.ResponseSummary.StatusCode == RouteService.ResponseStatusCode.Success) & (e.Result.Result.Legs.Count != 0))
            {
                // Set properties of the route line you want to draw.
                Color routeColor = Colors.Blue;
                SolidColorBrush routeBrush = new SolidColorBrush(routeColor);
                MapPolyline routeLine = new MapPolyline();
                routeLine.Locations = new LocationCollection();
                routeLine.Stroke = routeBrush;
                routeLine.Opacity = 0.65;
                routeLine.StrokeThickness = 5.0;

                DoubleCollection collection = new DoubleCollection();
                collection.Add(0.1);
                collection.Add(0.4);
                collection.Add(0.1);
                routeLine.StrokeDashArray = collection;

                mySummary.Distance = e.Result.Result.Summary.Distance;
                mySummary.Time = e.Result.Result.Summary.TimeInSeconds;

                // Retrieve the route points that define the shape of the route.
                foreach (Location p in e.Result.Result.RoutePath.Points)
                {
                    Location location = new Location();
                    location.Longitude = p.Longitude;
                    location.Latitude = p.Latitude;
                    routeLine.Locations.Add(location);
                }

                routeLayer.Children.Clear();

                // Add the route line to the new layer.
                routeLayer.Children.Add(routeLine);

                myMainPage.SummaryLoaded();

/*
                // Figure the rectangle which encompasses the route. This is used later to set the map view.
                LocationRect rect = new LocationRect(routeLine.Locations[0], routeLine.Locations[routeLine.Locations.Count - 1]);

                // For each geocode result (which are the waypoints of the route), draw a dot on the map.
                foreach (GeocodeService.GeocodeResult gr in geocodeResults)
                {
                    Ellipse point = new Ellipse();
                    point.Width = 10;
                    point.Height = 10;
                    point.Fill = new SolidColorBrush(Colors.Red);
                    point.Opacity = 0.65;
                    Location location = new Location(gr.Locations[0].Latitude, gr.Locations[0].Longitude);
                    MapLayer.SetPosition(point, location);
                    MapLayer.SetPositionOrigin(point, PositionOrigin.Center);

                    // Add the drawn point to the route layer.                    
                    myRouteLayer.Children.Add(point);
                }

                // Set the map view using the rectangle which bounds the rendered route.
                myMap.SetView(rect);
 * */
            }
        }
    }
}
