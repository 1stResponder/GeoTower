using System;
using CoreLocation;

namespace WatchTower.iOS
{
	public class LocationUpdatedEventArgs
	{
		CLLocation location;

		public LocationUpdatedEventArgs(CLLocation location)
		{
			this.location = location;
		}

		public CLLocation Location
		{
			get { return location; }
		}
	}
}
