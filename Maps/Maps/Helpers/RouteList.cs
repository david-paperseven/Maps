using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;

using Maps.RouteService;

namespace Maps.Helpers
{
    public class RouteList
    {
        List<Plaque> routeList;
        Plaque endPoint;
        public RouteList()
        {
            routeList = new List<Plaque>();
        }

        public void Add(Plaque plaque)
        {
            routeList.Add(plaque);
        }

        public void Remove(Plaque plaque)
        {
            routeList.Remove(plaque);
        }

        public void Clear()
        {
            routeList.Clear();
        }

        public void SetEndPoint(Plaque plaque)
        {
            endPoint = plaque;
        }

        public Plaque GetEndPoint()
        {
            return endPoint;
        }

        public List<Plaque> GetList()
        {
            return routeList;
        }

        public System.Collections.ObjectModel.ObservableCollection<Waypoint> GetFinalList(GeoCoordinate currentLocation)
        {
            System.Collections.ObjectModel.ObservableCollection<Waypoint> waypoints = new System.Collections.ObjectModel.ObservableCollection<Waypoint>();

            Waypoint mywp = new Waypoint();
            mywp.Location = currentLocation;
            waypoints.Add(mywp);

            foreach (Plaque p in routeList)
            {
                if (p != endPoint) // endpoint treated separately
                {
                    Waypoint wp = new Waypoint();
                    wp.Location = p.Info.location;
                    waypoints.Add(wp);
                }
            }

            Waypoint ep = new Waypoint();
            ep.Location = endPoint.Info.location;
            waypoints.Add(ep);
            return waypoints;
        }
    }
}
