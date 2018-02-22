using System;


namespace WatchTower.iOS
{
	/// <summary>
	/// This class serves up objects that are needed by multiple parts of app.  Using this class to retrieve an 
	/// object ensures that at most one object of each type is ever in memory at a time.
	/// </summary>
	public class SingletonManager
	{
		// volatile ensures assignment to instance variable completes before instance variable can be accessed
		static volatile LocationManager _locationManager = null;
		static object _lockObjectLocationManager = new object();

		static volatile WatchTowerSettings _watchTowerSettings = null;
		static object _lockObjectWatchTowerSettings = new object();

		static volatile MapIconManager _mapIconManager = null;
		static object _lockObjectMapIconManager = new object();

		static volatile BluetoothSensorManager _bluetoothSensorManager = null;
		static object _lockObjectBluetoothSensorManager = new object();

		static volatile HexoskinManager _hexoskinManager = null;
		static object _lockObjectHexoskinManager = new object();

		/// <summary>
		/// Returns a singleton LocationManager
		/// 
		/// See https://msdn.microsoft.com/en-us/library/ff650316.aspx
		/// </summary>
		/// <value>The location manager.</value>
		public static LocationManager LocationManager
		{
			get
			{
				if (_locationManager == null)
				{
					// wait for lock
					lock (_lockObjectLocationManager)
					{
						// have lock, now make sure object not created by another thread while you were waiting
						if (_locationManager == null)
						{
							_locationManager = new LocationManager();
							Console.WriteLine("created location manager");
						}
					}
				}

				return _locationManager;
			}
		}


		/// <summary>
		/// Returns a singleton WatchTowerSettings.  Any part of app needing settings should use this
		/// to ensure it's using the same content as every other part.
		/// 
		/// See https://msdn.microsoft.com/en-us/library/ff650316.aspx
		/// </summary>
		/// <value>The location manager.</value>
		public static WatchTowerSettings WatchTowerSettings
		{
			get
			{
				if (_watchTowerSettings == null)
				{
					// wait for lock
					lock (_lockObjectWatchTowerSettings)
					{
						// have lock, now make sure object not created by another thread while you were waiting
						if (_watchTowerSettings == null)
							_watchTowerSettings = new WatchTowerSettings();
					}
				}

				return _watchTowerSettings;
			}
		}


		/// <summary>
		/// Returns a singleton HTTPSender
		/// </summary>
		/// <value>The http sender.</value>
		//public static HTTPSender HttpSender
		//{
		//	get
		//	{
		//		if (_httpSender == null)
		//		{
		//			// wait for lock
		//			lock (_lockObjectHttpSender)
		//			{
		//				// have lock, now make sure object not created by another thread while you were waiting
		//				if (_httpSender == null)
		//					_httpSender = new HTTPSender();
		//			}
		//		}

		//		return _httpSender;
		//	}
		//}


		public static MapIconManager MapIconManager
		{
			get
			{
				if (_mapIconManager == null)
				{
					// wait for lock
					lock (_lockObjectMapIconManager)
					{
						// have lock, now make sure object not created by another thread while you were waiting
						if (_mapIconManager == null)
							_mapIconManager = new MapIconManager();
					}
				}

				return _mapIconManager;
			}
		}

		public static BluetoothSensorManager BluetoothSensorManager
		{
			get
			{
				if (_bluetoothSensorManager == null)
				{
					// wait for lock
					lock (_lockObjectBluetoothSensorManager)
					{
						// have lock, now make sure object not created by another thread while you were waiting
						if (_bluetoothSensorManager == null)
							_bluetoothSensorManager = new BluetoothSensorManager();
					}
				}

				return _bluetoothSensorManager;
			}
		}

		public static HexoskinManager HexoskinManager
		{
			get
			{
				if (_hexoskinManager == null)
				{
					// wait for lock
					lock (_lockObjectHexoskinManager)
					{
						// have lock, now make sure object not created by another thread while you were waiting
						if (_hexoskinManager == null)
							_hexoskinManager = new HexoskinManager();
					}
				}

				return _hexoskinManager;
			}
		}
	}
}
