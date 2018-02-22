using System;
namespace WatchTower.iOS
{
	/// <summary>
	/// Simple class to track various data about location
	/// </summary>
	public class LocationTracker
	{
		public LocationTracker()
		{
		}

		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public bool LocationUpdated { get; set; }
		public DateTime TimeLastUpdated { get; set; }

		public double LatitudeLastPosted { get; set; }
		public double LongitudeLastPosted { get; set; }
		public DateTime TimeLastPosted { get; set; }
		public bool bLocationWasPosted { get; set; }
	}
}
