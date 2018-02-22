using System;
using Foundation;

namespace WatchTower.iOS
{
	/// <summary>
	/// WatchTower settings.  Loads and stores various settings for easy access by app.
	/// 
	/// See http://stackoverflow.com/questions/20998894/accessing-xamarin-ios-settings-bundle
	/// </summary>
	public class WatchTowerSettings : NSObject
	{
		string _defaultUserID;
		string _defaultServerUrl;
		string _defaultMapServerUrl;
		int _defaultUpdateInterval;
		string _defaultResourceType;
		string _defaultSenderAgency;
		string _defaultHexoskinID;

		string _editedUserID;
		string _editedServerUrl;
		string _editedtMapServerUrl;
		int _editedUpdateInterval = 0;
		string _editedResourceType;
		string _editedAgency;
		string _editedHexoskinID;

		string _bundleVersion;

		int _updateInterval;
		const int DEFAULT_UPDATE_INTERVAL = 5;

		// keys to use to lookup and track settings
		const string USER_ID_KEY = "user_id_preference";
		const string SERVER_URL_KEY = "server_url_preference";
		const string ENABLED_KEY = "enabled_preference";
		const string UPDATE_INTERVAL_KEY = "update_interval_preference";
		const string MAP_SERVER_URL_KEY = "map_server_url_preference";
		const string RESOURCE_TYPE_KEY = "resource_type_preference";
		const string AGENCY_KEY = "sender_id_preference";
		const string HEXOSKIN_ID_KEY = "hexoskin_id_preference";

		const string REPORT_LOCATION_KEY = "b_report_location";
		const string REPORT_LOCATION_LAST_UPDATED_KEY = "report_location_last_updated";

		// set up change detection tokens
		IntPtr tokenObserveUserID = (IntPtr)1;
		IntPtr tokenObserveServerUrl = (IntPtr)2;
		IntPtr tokenObserveUpdateInterval = (IntPtr)3;
		IntPtr tokenObserveMapServerUrl = (IntPtr)4;
		IntPtr tokenObserveResourceType = (IntPtr)5;
		IntPtr tokenObserveAgencyKey = (IntPtr)6;
		IntPtr tokenObserveHexoskinID = (IntPtr)7;


		public string UserID { get; private set; }
		public string ServerUrl { get; private set; }
		public string MapServerUrl { get; private set; }
		public string ResourceType { get; private set; }
		public string ReportLocationLastUpdatedString { get; private set; }
		public string Agency { get; private set; }
		public string HexoskinID { get; private set; }

		public string BundleVersion
		{
			get
			{
				if (String.IsNullOrEmpty(_bundleVersion))
					_bundleVersion = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString();
				return _bundleVersion;
			}
		}

		public int UpdateIntervalReport { get; private set; }

		public int DesiredAccuracyInMeters { get; set; }

		// this one different in that it isn't stored with regular settings
		public bool bReportLocation { get; private set; }


		/// <summary>
		/// Property to hold update interval.  
		/// 
		/// Note that this property assumes that the value sent to it when setting is in seconds.  The
		/// setter multiplies the incoming value by 1000 before storing (to convert to milliseconds).
		/// </summary>
		/// <value>The update interval.</value>
		public int UpdateInterval
		{
			get
			{
				return _updateInterval;
			}
			private set
			{
				_updateInterval = value * 1000;
			}
		}

		private int UpdateIntervalToSave
		{
			get { return UpdateInterval / 1000; }
		}


		public WatchTowerSettings()
		{
			SetUpByPreferences();
			DesiredAccuracyInMeters = 1; // default for now
		}

		public void SetUpByPreferences()
		{
			LoadDefaultValues();
			LoadEditedValues();

			LoadAppData();
			SetSettingsValues();
			AddSettingsObservers();
		}

		/// <summary>
		/// Saves the report location preference.
		/// </summary>
		/// <param name="bReportLocationVal">If set to <c>true</c> b report location value.</param>
		public void SaveReportLocationPreference(bool bReportLocationVal)
		{
			bReportLocation = bReportLocationVal;
			NSUserDefaults.StandardUserDefaults.SetBool(bReportLocationVal, REPORT_LOCATION_KEY);
		}

		/// <summary>
		/// Saves the location last updated string.
		/// </summary>
		/// <param name="locationLastUpdatedString">Location last updated string.</param>
		public void SaveLocationLastUpdatedString(string locationLastUpdatedString)
		{
			NSUserDefaults.StandardUserDefaults.SetString(locationLastUpdatedString, REPORT_LOCATION_LAST_UPDATED_KEY);
		}

		/// <summary>
		/// Adds the settings observers.
		/// </summary>
		void AddSettingsObservers()
		{
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)USER_ID_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveUserID);
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)SERVER_URL_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveServerUrl);
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)MAP_SERVER_URL_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveMapServerUrl);
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)UPDATE_INTERVAL_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveUpdateInterval);
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)RESOURCE_TYPE_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveResourceType);
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)AGENCY_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveAgencyKey);
			NSUserDefaults.StandardUserDefaults.AddObserver(this, (NSString)HEXOSKIN_ID_KEY, NSKeyValueObservingOptions.OldNew, tokenObserveHexoskinID);
		}

		/// <summary>
		/// Observes the settings values that the user changed.
		/// </summary>
		/// <param name="keyPath">Key path.</param>
		/// <param name="ofObject">Of object.</param>
		/// <param name="change">Change.</param>
		/// <param name="ctx">Context.</param>
		public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr ctx)
		{
			
			string keyNewValue = "new";

			try
			{
				string newValue = change[keyNewValue].ToString();

				if (ctx == tokenObserveUserID)
				{
					this.UserID = newValue;
				}
				else if (ctx == tokenObserveServerUrl)
				{
					this.ServerUrl = newValue;
				}
				else if (ctx == tokenObserveMapServerUrl)
				{
					this.MapServerUrl = newValue;
				}
				else if (ctx == tokenObserveUpdateInterval)
				{
					string sNewUpdateInterval = newValue;
					int newUpdateInterval = 0;
					int defaultUpdateInterval = 5;

					// First make sure we have something we can attempt to convert
					if (!String.IsNullOrEmpty(sNewUpdateInterval))
					{
						try
						{
							newUpdateInterval = Convert.ToInt32(sNewUpdateInterval);

							// If converted value isn't valid, reset to default
							if (newUpdateInterval <= 0)
							{
								Console.WriteLine($"invalid update interval {newUpdateInterval}");
								Console.WriteLine($"resetting to {defaultUpdateInterval}");

								newUpdateInterval = DEFAULT_UPDATE_INTERVAL;
							}
						}
						catch (Exception)
						{
							Console.WriteLine("invalid update interval");
							Console.WriteLine($"resetting to {DEFAULT_UPDATE_INTERVAL}");

							// Something went wrong.  Set to default.
							newUpdateInterval = DEFAULT_UPDATE_INTERVAL;
						}
					}
					else // it's empty.  Set to default
					{
						Console.WriteLine("invalid update interval");
						Console.WriteLine($"resetting to {DEFAULT_UPDATE_INTERVAL}");
						newUpdateInterval = DEFAULT_UPDATE_INTERVAL;
					}

					this.UpdateInterval = newUpdateInterval;
				}
				else if (ctx == tokenObserveResourceType)
				{
					this.ResourceType = newValue;
				}
				else if (ctx == tokenObserveAgencyKey)
				{
					this.Agency = newValue;
				}
				else if (ctx == tokenObserveHexoskinID)
				{
					this.HexoskinID = newValue;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception observing settings change:  {ex.InnerException}");
			}
		}

		/// <summary>
		/// Loads the default values.
		/// </summary>
		void LoadDefaultValues()
		{
			var settingsDictionary = new NSDictionary(NSBundle.MainBundle.PathForResource("Settings.bundle/Root.plist", null));

			LoadSettings(settingsDictionary);
		}

		bool AppDataExistsForKey(string key)
		{
			return NSUserDefaults.StandardUserDefaults[key] != null;
		}

		/// <summary>
		/// Loads the app data.
		/// </summary>
		void LoadAppData()
		{
			// Load report location preference if set
			if (AppDataExistsForKey(REPORT_LOCATION_KEY))
			{
				bReportLocation = NSUserDefaults.StandardUserDefaults.BoolForKey(REPORT_LOCATION_KEY);
			}
			else
			{
        // otherwise default to true
        bReportLocation = false;

				// then save this preference (subsequent user override will update this)
				SaveReportLocationPreference(bReportLocation);
			}


			// load last position string if it's there
			if (AppDataExistsForKey(REPORT_LOCATION_LAST_UPDATED_KEY))
				ReportLocationLastUpdatedString = NSUserDefaults.StandardUserDefaults.StringForKey(REPORT_LOCATION_LAST_UPDATED_KEY);
			else
				ReportLocationLastUpdatedString = "not posted";
		}


		void LoadSettings(NSDictionary settingsDictionary)
		{
			// Load settings from the default dictionary
			if (settingsDictionary != null)
			{
				// See http://stackoverflow.com/questions/20998894/accessing-xamarin-ios-settings-bundle
				var prefSpecifierArray = settingsDictionary[(NSString)"PreferenceSpecifiers"] as NSArray;

				if (prefSpecifierArray != null)
				{
					foreach (var prefItem in NSArray.FromArray<NSDictionary>(prefSpecifierArray))
					{
						var key = prefItem[(NSString)"Key"] as NSString;

						if (key == null)
							continue;

						var value = prefItem[(NSString)"DefaultValue"];


						if (value == null)
							continue;

						switch (key.ToString())
						{
							case USER_ID_KEY:
								_defaultUserID = value.ToString();
								break;
							case SERVER_URL_KEY:
								_defaultServerUrl = value.ToString();
								break;
							case UPDATE_INTERVAL_KEY:
								var intervalValue = (NSNumber)prefItem["DefaultValue"];
								_defaultUpdateInterval = intervalValue.Int32Value;
								break;
							case MAP_SERVER_URL_KEY:
								_defaultMapServerUrl = value.ToString();
								break;
							case RESOURCE_TYPE_KEY:
								_defaultResourceType = value.ToString();
								break;
							case AGENCY_KEY:
								_defaultSenderAgency = value.ToString();
								break;
							case HEXOSKIN_ID_KEY:
								_defaultHexoskinID = "";
								break;
							default:
								break;
						}
					}
				}
			}
		}


		/// <summary>
		/// Loads values that the user may have changed
		/// </summary>
		void LoadEditedValues()
		{
			_editedUserID = NSUserDefaults.StandardUserDefaults.StringForKey(USER_ID_KEY);
			_editedServerUrl = NSUserDefaults.StandardUserDefaults.StringForKey(SERVER_URL_KEY);
			_editedUpdateInterval = (int)NSUserDefaults.StandardUserDefaults.IntForKey(UPDATE_INTERVAL_KEY);
			_editedtMapServerUrl = NSUserDefaults.StandardUserDefaults.StringForKey(MAP_SERVER_URL_KEY);
			_editedResourceType = NSUserDefaults.StandardUserDefaults.StringForKey(RESOURCE_TYPE_KEY);
			_editedAgency = NSUserDefaults.StandardUserDefaults.StringForKey(AGENCY_KEY);
			_editedHexoskinID = NSUserDefaults.StandardUserDefaults.StringForKey(HEXOSKIN_ID_KEY);
		}


		/// <summary>
		/// Sets values based on default or edited changes
		/// </summary>
		void SetSettingsValues()
		{
			UserID = String.IsNullOrEmpty(_editedUserID) ? _defaultUserID : _editedUserID;
			ServerUrl = String.IsNullOrEmpty(_editedServerUrl) ? _defaultServerUrl : _editedServerUrl;
			UpdateInterval = _editedUpdateInterval <= 0 ? _defaultUpdateInterval : _editedUpdateInterval;
			UpdateIntervalReport = UpdateInterval;
			MapServerUrl = String.IsNullOrEmpty(_editedtMapServerUrl) ? _defaultMapServerUrl : _editedtMapServerUrl;
			ResourceType = String.IsNullOrEmpty(_editedResourceType) ? _defaultResourceType : _editedResourceType;
			Agency = String.IsNullOrEmpty(_editedAgency) ? _defaultSenderAgency : _editedAgency;
			HexoskinID = String.IsNullOrEmpty(_editedHexoskinID) ? _defaultHexoskinID : _editedHexoskinID;
		}


		// Save preferences to Settings
		void SavePreferences()
		{
			var keys = new object[] { USER_ID_KEY, SERVER_URL_KEY, UPDATE_INTERVAL_KEY };
			var values = new object[] { UserID, ServerUrl, UpdateIntervalToSave };

			var appDefaults = NSDictionary.FromObjectsAndKeys(values, keys);

			//foreach (string key in keys)
			//{
			//	NSUserDefaults.StandardUserDefaults.RemoveObject(key);
			//}


			//var appDefaults = NSDictionary.FromObjectsAndKeys(new object[]
			//{new NSString(UserID), 
			//new NSString(ServerUrl)}, 
			//       	new object[] { USER_ID_KEY, 
			//	SERVER_URL_KEY });

			//NSUserDefaults.ResetStandardUserDefaults();
			NSUserDefaults.StandardUserDefaults.RegisterDefaults(appDefaults);
			NSUserDefaults.StandardUserDefaults.Synchronize();
		}
	}
}
