﻿using BirdTouchWebAPI.Models;
using System;

namespace BirdTouchWebAPI.Extensions
{
    public static class CoordinateExtensions
    {
        public static double DistanceTo(this Coordinate baseCoordinates, Coordinate targetCoordinates)
        {
            return DistanceTo(baseCoordinates, targetCoordinates, UnitOfLength.Kilometers);
        }

        public static double DistanceTo(this Coordinate baseCoordinates, double distanceToLatitude, double distanceToLongitude)
        {
            return DistanceTo(
                baseCoordinates,
                new Coordinate(distanceToLatitude, distanceToLongitude),
                UnitOfLength.Kilometers);
        }

        public static double DistanceTo(this Coordinate baseCoordinates, Coordinate targetCoordinates, UnitOfLength unitOfLength)
        {
            var baseRad = Math.PI * baseCoordinates.Latitude / 180;
            var targetRad = Math.PI * targetCoordinates.Latitude / 180;
            var theta = baseCoordinates.Longitude - targetCoordinates.Longitude;
            var thetaRad = Math.PI * theta / 180;

            double dist =
                Math.Sin(baseRad) * Math.Sin(targetRad) + Math.Cos(baseRad) *
                Math.Cos(targetRad) * Math.Cos(thetaRad);
            dist = Math.Acos(dist);

            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;

            return unitOfLength.ConvertFromMiles(dist);
        }
    }
}

