using System;
using CoreGraphics;
using CoreLocation;
using CoreTelephony;
using Foundation;
using UIKit;

namespace WatchTower.iOS
{
	public partial class LocationViewController : UIViewController
	{
		public LocationManager Manager { get; set; }

		WatchTowerSettings _watchTowerSettings;

		public LocationViewController() : base("LocationViewController", null)
		{
			Manager = SingletonManager.LocationManager;
			_watchTowerSettings = SingletonManager.WatchTowerSettings;
		}

		public LocationViewController(IntPtr handle) : base(handle)
		{
			Manager = SingletonManager.LocationManager;
			_watchTowerSettings = SingletonManager.WatchTowerSettings;
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			// add handler to Manager.LocationUpdated event
			Manager.LocationUpdated += OnLocationChanged;

			// Set appearance for location switch and last updated label
			LocationSwitch.On = _watchTowerSettings.bReportLocation;
			LastSentLabel.Text = _watchTowerSettings.ReportLocationLastUpdatedString;

			VersionLabel.Text = _watchTowerSettings.BundleVersion;
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);

			// Check WatchTower settings and update accordingly (user could have toggled preference on 
			// map view).
			LocationSwitch.On = _watchTowerSettings.bReportLocation;
			SetReportLocationTextAppearance();



			//string meterString = _watchTowerSettings.DesiredAccuracyInMeters > 1 ? "meters" : "meter";
		}


		public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
		{
			base.ViewWillTransitionToSize(toSize, coordinator);

			//CGRect newBounds = new CGRect(View.Bounds.X, View.Bounds.Y, toSize.Width, toSize.Height);
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		//void SetUpWindowSize(CGSize toSize, CGRect newBounds)
		//{
		//	//CGSize newSizeForScroll;

		//	//if (toSize.Width > toSize.Height)// landscape
		//	//{
		//	//	newSizeForScroll = new CGSize(toSize.Width, toSize.Width);
		//	//	ScrollView.ContentSize = newSizeForScroll;
		//	//}
		//	//else // portrait
		//	//{
		//	//	newSizeForScroll = new CGSize(toSize.Width, toSize.Height);
		//	//}

		//	//MainImageView.Frame = new CGRect(0, 0, newSizeForScroll.Width, newSizeForScroll.Height);
		//	////MainImageView.Bounds = new CGRect(0, 0, toSize.Width, toSize.Height);;
		//	//ScrollView.Frame = newBounds;
		//	//ScrollView.ContentSize = newSizeForScroll;
		//}

		partial void ReportingEnabledChanged(UISwitch sender)
		{
			// user changes reporting pref - update settings
			_watchTowerSettings.SaveReportLocationPreference(sender.On);

			if (sender.On)
			{
				Manager.ResumePostingLocation();
			}
			else
			{
				Manager.StopPostingLocation();
			}

			SetReportLocationTextAppearance();
		}

		/// <summary>
		/// Sets the report location text appearance based on WatchTower settings.
		/// </summary>
		void SetReportLocationTextAppearance()
		{
			if (_watchTowerSettings.bReportLocation)
			{
				ReportLabel.Text = "Reporting Enabled";
				ReportLabel.TextColor = UIColor.Black;
				ReportLabel.Font = UIFont.BoldSystemFontOfSize(20);
			}
			else
			{
				ReportLabel.Text = "Reporting Disabled";
				ReportLabel.TextColor = UIColor.Gray;
				ReportLabel.Font = UIFont.SystemFontOfSize(20);
			}
		}



		public void OnLocationChanged(object sender, LocationUpdatedEventArgs e)
		{
			// Handle foreground updates
			CLLocation location = e.Location;

			//LblAltitude.Text = location.Altitude + " meters";
			LatLabel.Text = location.Coordinate.Latitude.ToString();
			LonLabel.Text = location.Coordinate.Longitude.ToString();

			LastSentLabel.Text = Manager.GetLastLocationString();
			//LblCourse.Text = location.Course.ToString();
			//LblSpeed.Text = location.Speed.ToString();

			//Console.WriteLine("foreground updated");
		}

		/// <summary>
		/// Gets the name of the phone service carrier.
		/// </summary>
		/// <returns>The phone service carrier name.</returns>
		string GetPhoneServiceCarrierName()
		{
			string carrierName = "N/A";

			using (var info = new CTTelephonyNetworkInfo())
			{


				if (info.SubscriberCellularProvider != null)
					carrierName = info.SubscriberCellularProvider.CarrierName;
			}

			return carrierName;
		}
	}
}

