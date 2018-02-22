using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Timers;
using CoreGraphics;
using Google.Maps;
using UIKit;
using CoreLocation;
using System.Drawing;

namespace WatchTower.iOS
{
	public partial class MapViewController : UIViewController
	{
		private MapView mapView;

		BackgroundWorkerWrapper _bgWorkerWrapper_MapFeatures;

		FeatureCollection features;
		string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

		UIColor _green = UIColor.FromRGB(9, 188, 128);
		UIColor _gray = UIColor.FromRGB(175, 181, 179);
		UIButton _reportLocationButton;


		private WatchTowerSettings _watchTowerSettings;
		private LocationManager _locationManager;
		MapIconManager _mapIconManager;

		/// <summary>
		/// Place holder for when this is toggle-able
		/// </summary>
		/// <returns>The map type.</returns>
		MapViewType GetMapType()
		{
			return MapViewType.Hybrid;
		}

		public MapViewController() : base("MapViewController", null)
		{
		}

		public MapViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			_watchTowerSettings = SingletonManager.WatchTowerSettings;
			_locationManager = SingletonManager.LocationManager;
			_mapIconManager = SingletonManager.MapIconManager;

			// create map sub-view and add to main view
			CameraPosition camera = CameraPosition.FromCamera(latitude: 0,
													   longitude: 0,
													   zoom: 1);
			mapView = MapView.FromCamera(new CGRect(0, 0, View.Frame.Width, View.Frame.Height - 49), camera);
			mapView.MapType = GetMapType();

			mapView.Settings.MyLocationButton = true;
			mapView.MyLocationEnabled = true;
			View.AddSubview(mapView);

			// now initialize report location button and add to main view
			InitializeReportLocationButton();
			View.AddSubview(_reportLocationButton);

			// set up background worker 
			SetUpAndStartBackgroundWorker();

			// Perform any additional setup after loading the view, typically from a nib.
		}

		public override void ViewDidLayoutSubviews()
		{
			base.ViewDidLayoutSubviews();
		}

		public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
		{
			base.ViewWillTransitionToSize(toSize, coordinator);
			if (mapView == null)
			{
				CameraPosition camera = CameraPosition.FromCamera(latitude: 37.797865,
													   longitude: -122.402526,
													   zoom: 6);
				mapView = MapView.FromCamera(new CGRect(0, 0, View.Frame.Width, View.Frame.Height - 49), camera);
				mapView.MapType = GetMapType();
				mapView.Settings.MyLocationButton = true;
				mapView.MyLocationEnabled = true;
			}
			mapView.Frame = new CGRect(0, 0, toSize.Width, toSize.Height - 49);

		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);


			// update appearance of location button based on current WatchTower settings
			SetReportLocationBackgroundColor(_watchTowerSettings.bReportLocation);
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		/// <summary>
		/// Handles the report location toggle.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		void HandleReportLocationToggle(object sender, EventArgs e)
		{
			// New value is opposite of what it was
			bool bNewReportLocationPreference = !_watchTowerSettings.bReportLocation;

			SetReportLocationBackgroundColor(bNewReportLocationPreference);

			// user changes reporting pref - update settings
			_watchTowerSettings.SaveReportLocationPreference(bNewReportLocationPreference);

			if (_watchTowerSettings.bReportLocation)
				_locationManager.ResumePostingLocation();
			else
				_locationManager.StopPostingLocation();

		}

		/// <summary>
		/// Initializes the report location button.
		/// </summary>
		void InitializeReportLocationButton()
		{
			float width = 56;
			float height = width;

			int x = (int)(View.Frame.Width - (width + 10));
			int y = (int)(View.Frame.Height - (height + 130));

			// corner radius needs to be one half of the size of the view
			float cornerRadius = width / 2;
			RectangleF frame = new RectangleF(x, y, width, height);

			// initialize button
			_reportLocationButton = new UIButton(frame);

			// set corner radius
			_reportLocationButton.Layer.CornerRadius = cornerRadius;

			// set image for the button
			_reportLocationButton.SetImage(UIImage.FromBundle("location"), UIControlState.Normal);

			// set background color
			_reportLocationButton.BackgroundColor = _green;
	
			// attach event handler
			_reportLocationButton.TouchUpInside += HandleReportLocationToggle;
		}

		/// <summary>
		/// Sets the color of the report location background.
		/// </summary>
		/// <param name="bReportLocation">If set to <c>true</c> b report location.</param>
		void SetReportLocationBackgroundColor(bool bReportLocation)
		{
			if (bReportLocation)
				_reportLocationButton.BackgroundColor = _green;
			else
				_reportLocationButton.BackgroundColor = _gray;
		}



		/// <summary>
		/// Creates single background worker for use by MapViewController and attaches relevent methods.
		/// </summary>
		private void SetUpAndStartBackgroundWorker()
		{
			_bgWorkerWrapper_MapFeatures = new BackgroundWorkerWrapper(new DelegateDefinitions.DoWorkOrWorkCompletedDelegate(KML_DoWork),
																	   new DelegateDefinitions.DoWorkOrWorkCompletedDelegate(KML_WorkCompleted),
																	   _watchTowerSettings.UpdateInterval);

			_bgWorkerWrapper_MapFeatures.StartWork(_watchTowerSettings.UpdateInterval);
		}


		/// <summary>
		/// Issues GET to map server to get list of features.
		/// </summary>
		private void KML_DoWork()
		{
			try
			{
				string urlToUse = _watchTowerSettings.MapServerUrl;

				HttpWebRequest req;
				req = (HttpWebRequest)WebRequest.Create(urlToUse);
				req.Method = WebRequestMethods.Http.Get;
				req.KeepAlive = true;
				req.Accept = accept;
				req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				HttpWebResponse webResponse = (HttpWebResponse)req.GetResponse();
				Stream responseStream = webResponse.GetResponseStream();
				StreamReader streamReader = new StreamReader(responseStream);
				string s = streamReader.ReadToEnd();
				streamReader.Close();
				responseStream.Close();
				features = FeatureCollection.FromString(s);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());

				// Error reading features.  Clear out collection so we're not showing stale data
				features = null; 
			}

		}

		/// <summary>
		/// Populates current list of features on map and associates icon with each
		/// </summary>
		private void KML_WorkCompleted()
		{
			string icon;
			Marker marker;
			UIImage iconimage;
			this.InvokeOnMainThread(() =>
			{
				mapView.Clear();

				if (features != null) // may be null if invalid map url
				{
					foreach (Feature feature in features.features)
					{
						icon = feature.properties.iconurl;
						icon = icon.Substring(icon.LastIndexOf("/") + 1);
						marker = new Marker();
						marker.Position = new CLLocationCoordinate2D(feature.geometry.coordinates[1], feature.geometry.coordinates[0]);
						marker.Title = feature.properties.title;
						iconimage = _mapIconManager.GetImageForIconName(icon);

						if (iconimage != null)
						{
							marker.Icon = iconimage;
						}
						marker.Map = mapView;
					}
				}

				// reset interval since it may have changed since the last time this was run
				_bgWorkerWrapper_MapFeatures.SetInterval(_watchTowerSettings.UpdateInterval);
			});
		}
	}
}

