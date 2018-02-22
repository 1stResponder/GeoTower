using System;
using CoreLocation;
using UIKit;

namespace WatchTower.iOS
{
	public class LocationManager
	{
		protected CLLocationManager locMgr;
		public event EventHandler<LocationUpdatedEventArgs> LocationUpdated = delegate { };


		LocationTracker _lastLocation;
		WatchTowerSettings _watchTowerSettings;
		BackgroundWorkerWrapper _bgWorkerWrapper;

		public LocationManager()
		{
			this.locMgr = new CLLocationManager();
			this.locMgr.PausesLocationUpdatesAutomatically = false;

			// iOS 8 has additional permissions requirements
			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				locMgr.RequestAlwaysAuthorization(); // works in background
													 //locMgr.RequestWhenInUseAuthorization (); // only in foreground
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
			{
				locMgr.AllowsBackgroundLocationUpdates = true;
			}

			_lastLocation = new LocationTracker();
			_watchTowerSettings = SingletonManager.WatchTowerSettings;

			_bgWorkerWrapper = new BackgroundWorkerWrapper(new DelegateDefinitions.DoWorkOrWorkCompletedDelegate(PostLocation),
														   new DelegateDefinitions.DoWorkOrWorkCompletedDelegate(PostLocationCompleted),
														   _watchTowerSettings.UpdateInterval);

			StartLocationUpdates();
		}

		public CLLocationManager LocMgr
		{
			get { return this.locMgr; }
		}

		public double LastLatitude
		{
			get { return _lastLocation.Latitude; }
		}

		public double LastLongitude
		{
			get { return _lastLocation.Longitude; }
		}

		public void StartLocationUpdates()
		{
			if (CLLocationManager.LocationServicesEnabled)
			{
				//set the desired accuracy, in meters
				LocMgr.DesiredAccuracy = _watchTowerSettings.DesiredAccuracyInMeters;
				LocMgr.LocationsUpdated += (object sender, CLLocationsUpdatedEventArgs e) =>
				{
					// fire our custom Location Updated event
					LocationUpdated(this, new LocationUpdatedEventArgs(e.Locations[e.Locations.Length - 1]));
				};

				// keep track of lat/long as location updates
				this.LocationUpdated += UpdateLastLocation;
				LocMgr.StartUpdatingLocation();

				// start posting location
				if (_watchTowerSettings.bReportLocation)
					_bgWorkerWrapper.StartWork(_watchTowerSettings.UpdateInterval);
			}
		}

		public void StopPostingLocation()
		{
			_bgWorkerWrapper.StopWork();
		}

		public void ResumePostingLocation()
		{
			_bgWorkerWrapper.StartWork(_watchTowerSettings.UpdateInterval);
		}


		void PostLocation()
		{
			//don't post location unless it's been set
			if (_lastLocation.LocationUpdated)
			{
				//Console.WriteLine($"posting location for user {_watchTowerSettings.UserID}.  Latitude: {_lastLocation.Latitude}; Longitude: {_lastLocation.Longitude}");
				HTTPSender.SendLocation(_lastLocation.Latitude, _lastLocation.Longitude, 
				                                         _watchTowerSettings.ServerUrl, _watchTowerSettings.UserID, 
				                        _watchTowerSettings.Agency, _watchTowerSettings.ResourceType, SingletonManager.BluetoothSensorManager.GetAllSensorDetails());

				_lastLocation.TimeLastPosted = DateTime.Now;
				_lastLocation.bLocationWasPosted = true;
				_lastLocation.LatitudeLastPosted = _lastLocation.Latitude;
				_lastLocation.LongitudeLastPosted = _lastLocation.Longitude;

				//SingletonManager.BluetoothSensorManager.SendAllDataToServer();
			}
		}

		void PostLocationCompleted()
		{
			_bgWorkerWrapper.SetInterval(_watchTowerSettings.UpdateInterval);
		}

		/// <summary>
		/// Gets the string to be shown for "Last sent" section
		/// </summary>
		/// <returns>The last location string.</returns>
		public string GetLastLocationString()
		{
			if (_lastLocation.bLocationWasPosted)
			{
				string shortDateString = String.Format("{0:MM/dd/yy hh:mm:ss}", _lastLocation.TimeLastPosted);

				string latLonString = String.Format("\nLat: {0:0.0000}, \nLon: {1:0.0000}", _lastLocation.LatitudeLastPosted, _lastLocation.LongitudeLastPosted);

				return shortDateString + " " + latLonString;
			}
			else // location never posted.  Just get the string to be saved from settings
				return _watchTowerSettings.ReportLocationLastUpdatedString;
		}

		/// <summary>
		/// Saves the last string that was displayed on Location tab for last location posted
		/// </summary>
		public void SaveLastLocationPostedString()
		{
			_watchTowerSettings.SaveLocationLastUpdatedString(GetLastLocationString());
		}


		/// <summary>
		/// Updates the variable tracking last location.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		public void UpdateLastLocation(object sender, LocationUpdatedEventArgs e)
		{
			// update last location
			_lastLocation.Latitude = e.Location.Coordinate.Latitude;
			_lastLocation.Longitude = e.Location.Coordinate.Longitude;
			_lastLocation.TimeLastUpdated = DateTime.Now;
			_lastLocation.LocationUpdated = true;
		}
	}
}
