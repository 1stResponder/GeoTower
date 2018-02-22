using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreBluetooth;
using EMS.NIEM.NIEMCommon;
using EMS.NIEM.Sensor;
using ExternalAccessory;
using Foundation;

namespace WatchTower.iOS
{
	/// <summary>
	/// Bluetooth sensor manager.  This is the central class to use to:
	/// 
	/// 1) Connect to Bluetooth devices
	/// 2) Disconnect from Bluetooth devices
	/// 3) Scan for Bluetooth devices
	/// 4) Get strings representing connected devices and associated data
	/// 
	/// This class creates a separate BluetoothSensorMonitor for each connected device, stored in a dictionary.
	/// 
	/// This class provides a sorted list of BluetoothSensorMonitor, based on the dictionary, for coordinating
	/// displaying in UI.  It is sorted on descending order of creation (most recent connected sensor first).
	/// 
	/// This class has two custom events that listeners can subscribe to:
	/// 
	/// 1) SensorConnectionsChanged -> occurs when a sensor is added or removed from the list of connected sensors.
	/// 2) DiscoveredPeripheral -> occurs when a scan is occuring and a new peripheral is discovered.
	/// </summary>
	public class BluetoothSensorManager
	{
		// Dictionary of sensors currently connected to this manager
		Dictionary<NSUuid, BluetoothSensorMonitor> _connectedSensorDictionary;

		// List of sensors currently connected to this manager, sorted in descending order of creation
		List<BluetoothSensorMonitor> _connectedSensorsSorted;

		// Used for bluetooth interfacing
		CBCentralManager _cbCentralManager = new CBCentralManager();

		// Because hexoskins are "special"
		HexoskinManager _hexoskinManager = SingletonManager.HexoskinManager;

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:WatchTower.iOS.BluetoothSensorManager"/> is connected to hexoskin data.
		/// </summary>
		/// <value><c>true</c> if b connected to hexoskin; otherwise, <c>false</c>.</value>
		public bool bConnectedToHexoskin
		{
			get
			{ return _hexoskinManager.bConnectedToData; }
		}

		// To use to track which sensors we've already discovered
		HashSet<NSUuid> _availableSensorIds;

		// Public events that consuming classes can subscribe to

		/// <summary>
		/// Occurs when sensor connections changed.
		/// </summary>
		public event EventHandler<BluetoothConnectionChangedEventArgs> SensorConnectionsChanged;

		/// <summary>
		/// Occurs when peripheral is discovered
		/// </summary>
		public event EventHandler<BluetoothDiscoveredPeripheralEventArgs> DiscoveredPeripheral;


		/// <summary>
		/// Called internally which emits custom event to let subscribers know that SensorConnectionsChanged occurred
		/// </summary>
		/// <param name="e">E.</param>
		protected virtual void OnSensorConnectionsChanged(BluetoothConnectionChangedEventArgs e)
		{
			EventHandler<BluetoothConnectionChangedEventArgs> handler = SensorConnectionsChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}


		/// <summary>
		/// Called internally which emits custom event to let subscribers know that DiscoveredPeripheral occurred
		/// </summary>
		/// <param name="e">E.</param>
		protected virtual void OnDiscoveredPeripheral(BluetoothDiscoveredPeripheralEventArgs e)
		{
			EventHandler<BluetoothDiscoveredPeripheralEventArgs> handler = DiscoveredPeripheral;
			if (handler != null)
			{
				handler(this, e);
			}
		}


		public BluetoothSensorManager()
		{
			_connectedSensorDictionary = new Dictionary<NSUuid, BluetoothSensorMonitor>();
			_connectedSensorsSorted = new List<BluetoothSensorMonitor>();

			_availableSensorIds = new HashSet<NSUuid>();

			InitializeCoreBluetooth();
		}


		/// <summary>
		/// Indicates whether the id is in the list of available sensor ids.
		/// </summary>
		/// <returns><c>true</c>, if not in list of available sensor ids<c>false</c> otherwise.</returns>
		/// <param name="id">Identifier.</param>
		bool PeripheralNotInAvailableList(NSUuid id)
		{
			return !_availableSensorIds.Contains(id);
		}


		/// <summary>
		/// Indicates whether a peripheral with the specified name is already connected.
		/// </summary>
		/// <returns><c>true</c>, if peripheral with name is already connected <c>false</c> otherwise.</returns>
		/// <param name="peripheralName">Peripheral name.</param>
		bool PeripheralNameNotAlreadyConnected(string peripheralName)
		{
			return !_connectedSensorsSorted.Any(p => String.Equals(peripheralName, p.Name, StringComparison.InvariantCultureIgnoreCase));
		}


		/// <summary>
		/// Initializes the core bluetooth and attaches event handlers to events from CB central manager
		/// </summary>
		void InitializeCoreBluetooth()
		{
			_cbCentralManager.UpdatedState += OnCentralManagerUpdatedState;

			_cbCentralManager.DiscoveredPeripheral += (sender, e) =>
			{
				/* When we find a peripheral, make sure it's not already in list
				 * and not already connected before adding it to available list.*/
				if (PeripheralNotInAvailableList(e.Peripheral.Identifier)
				    && PeripheralNameNotAlreadyConnected(e.Peripheral.Name))
				{
					_availableSensorIds.Add(e.Peripheral.Identifier);
					LogPeripheralDetails("Discovered", e);

					// emit custom event to let listeners know that a new peripheral was discovered
					OnDiscoveredPeripheral(new BluetoothDiscoveredPeripheralEventArgs
					{
						PeripheralName = e.Peripheral.Name,
						Peripheral = e.Peripheral,
						bIsHexoskinPeripheral = false});
				}
			};

			_cbCentralManager.FailedToConnectPeripheral += (sender, e) =>
			{
				LogPeripheral($"Failed to connect ({e.Error})", e.Peripheral);
				//DisconnectMonitor();
			};

			_cbCentralManager.ConnectedPeripheral += (sender, e) =>
			{
				LogPeripheral("Connected", e.Peripheral);
				e.Peripheral.DiscoverServices();

				// emit custom event to let listeners know that something changed regarding connected sensors (in this case, one was added)
				OnSensorConnectionsChanged(new BluetoothConnectionChangedEventArgs { Peripheral = e.Peripheral, 
																					UpdatedConnectedDevicesString = GetConnectedDevicesString() });
			};

			_cbCentralManager.DisconnectedPeripheral += (sender, e) =>
			{
				LogPeripheral("Disconnected", e.Peripheral);

				DisconnectFromSensor(e.Peripheral.Identifier);
				_availableSensorIds.Remove(e.Peripheral.Identifier);

				// emit custom event to let listeners know that something changed regarding connected sensors (in this case, one was added)
				OnSensorConnectionsChanged(new BluetoothConnectionChangedEventArgs
				{
					Peripheral = e.Peripheral,
					UpdatedConnectedDevicesString = GetConnectedDevicesString()
				});
			};
		}


		/// <summary>
		/// Used to handle state changes for central manager.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		void OnCentralManagerUpdatedState(object sender, EventArgs e)
		{
			string message = null;

			switch (_cbCentralManager.State)
			{
				case CBCentralManagerState.PoweredOn:
					//connectButton.Enabled = true;
					return;
				case CBCentralManagerState.Unsupported:
					message = "The platform or hardware does not support Bluetooth Low Energy.";
					break;
				case CBCentralManagerState.Unauthorized:
					message = "The application is not authorized to use Bluetooth Low Energy.";
					break;
				case CBCentralManagerState.PoweredOff:
					message = "Bluetooth is currently powered off.";
					break;
				default:
					break;
			}

			if (message != null)
			{
				Console.WriteLine(message);
				//new NSAlert
				//{
				//	MessageText = "Heart Rate Monitor cannot be used at this time.",
				//	InformativeText = message
				//}.RunSheetModal(Window);
				//NSApplication.SharedApplication.Terminate(manager);
			}
		}


		/// <summary>
		/// Logs the peripheral.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="peripheral">Peripheral.</param>
		void LogPeripheral(string message, CBPeripheral peripheral)
			=> Console.WriteLine($"{message}: {peripheral.Identifier} {peripheral}");


		/// <summary>
		/// Logs the peripheral details.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="e">E.</param>
		void LogPeripheralDetails(string message, CBDiscoveredPeripheralEventArgs e)
		{
			Console.WriteLine($"{message}: {e.Peripheral.Identifier} {e.Peripheral}");
			Console.WriteLine($"data: {e.AdvertisementData}");
		}

		/// <summary>
		/// Returns a sorted list based on the values in _connectedSensors
		/// </summary>
		/// <value>The connected sensors.</value>
		public List<BluetoothSensorMonitor> ConnectedSensorsSorted
		{
			get
			{
				return _connectedSensorsSorted;
			}
		}

		/// <summary>
		/// Adds to connected sensors dictionary, and updates associated sorted list.  Use this 
		/// to add sensors to the _connectedSensorDictionary
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="sensorMonitor">Sensor monitor.</param>
		public void AddToConnectedSensors(NSUuid id, BluetoothSensorMonitor sensorMonitor)
		{
			_connectedSensorDictionary.Add(id, sensorMonitor);

			UpdateSortedSensorMonitors();
		}

		/// <summary>
		/// Removes from connected sensors dictionary, and updates associated sorted list.  Use this to
		/// remove sensors from the _connectedSensorDictionary.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void RemoveFromConnectedSensors(NSUuid id)
		{
			_connectedSensorDictionary.Remove(id);

			UpdateSortedSensorMonitors();
		}


		/// <summary>
		/// Updates the sorted sensor monitors list based on changes to _connectedSensorDictionary
		/// </summary>
		void UpdateSortedSensorMonitors()
		{
			var sensorMonitorList = _connectedSensorDictionary.Values.ToList();
			sensorMonitorList.Sort();
			_connectedSensorsSorted = sensorMonitorList;
		}


		/// <summary>
		/// Gets the connected sensor count.
		/// </summary>
		/// <value>The connected sensor count.</value>
		public int ConnectedSensorCount
		{
			get { return _connectedSensorDictionary.Count; }
		}


		/// <summary>
		/// Scans for heart rate monitors.
		/// </summary>
		public void ScanForHeartRateMonitors()
		{
			SearchForHexoskinSensors();

			CBUUID[] cbuuids = { BluetoothIdentifiers.HeartRatePeripheralUUID };

			// clear hash set - everything is fair game at the beginning
			_availableSensorIds.Clear();

			_cbCentralManager.ScanForPeripherals(cbuuids);
		}


		/// <summary>
		/// Searchs for hexoskin sensors.
		/// </summary>
		void SearchForHexoskinSensors()
		{
			string hexoskinID = SingletonManager.WatchTowerSettings.HexoskinID;

			// If user entered a Hexoskin ID into settings and it's not already connected,
			// list it as an available sensor
			if (!String.IsNullOrEmpty(hexoskinID)
			    && PeripheralNameNotAlreadyConnected(hexoskinID))
			{

				//emit custom event to let listeners know that a new peripheral was discovered
				OnDiscoveredPeripheral(new BluetoothDiscoveredPeripheralEventArgs
				{
					PeripheralName = hexoskinID,
					Peripheral = null,
					bIsHexoskinPeripheral = true
				});
			}
		}


		/// <summary>
		/// Call this to stop scanning for heart rate monitors
		/// </summary>
		public void StopScanning()
		{
			_cbCentralManager.StopScan();
		}


		public void ConnectToSensor(CBPeripheral p)
		{
			BluetoothSensorMonitor connectedMonitor = new BluetoothSensorMonitor(_cbCentralManager, p);
			connectedMonitor.Connect();

			AddToConnectedSensors(p.Identifier, connectedMonitor);
		}


		public bool ConnectToHexoskinSensor()
		{
			return TryToGetHexoskinData();
		}

		/// <summary>
		/// Tries to get hexoskin data.
		/// </summary>
		/// <returns><c>true</c>, if to get hexoskin data was tryed, <c>false</c> otherwise.</returns>
		bool TryToGetHexoskinData()
		{
			if (_hexoskinManager.CanGetData()) // we have data
			{
				NSUuid nKey = new NSUuid();

				BluetoothSensorMonitor m = new BluetoothSensorMonitor(_hexoskinManager.HexoskinName, nKey);

				AddToConnectedSensors(nKey, m);

				// trigger event
				OnSensorConnectionsChanged(new BluetoothConnectionChangedEventArgs
				{
					Peripheral = null,
					UpdatedConnectedDevicesString = GetConnectedDevicesString()
				});

				// now start the loop to refresh hexoskin data at regular intervals
				_hexoskinManager.StartGettingData();
			}
			else
				_hexoskinManager.StopGettingData();
			

			return _hexoskinManager.bConnectedToData;
		}


		/// <summary>
		/// Disconnects from specified sensor.
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void DisconnectFromSensor(NSUuid id)
		{
			BluetoothSensorMonitor monitorToDisconnect;

			// remove from UI tracking list (that we use to avoid duplicates)
			_availableSensorIds.Remove(id);

			// If it's in the dictionary, find it and actually disconnect from it
			if (_connectedSensorDictionary.TryGetValue(id, out monitorToDisconnect))
			   monitorToDisconnect.Disconnect();

			DisposeSensorMonitor(id);
		}

		public void MarkSensorDisconnected(CBPeripheral p)
		{
			BluetoothSensorMonitor monitorToDisconnect;

			if (_connectedSensorDictionary.TryGetValue(p.Identifier, out monitorToDisconnect))
				monitorToDisconnect.PresentButDisconnected = true;
		}

		public void DisconnectFromAllSensors()
		{
			List<NSUuid> sensorIdentifiers = new List<NSUuid>(_connectedSensorDictionary.Count);

			foreach (KeyValuePair<NSUuid, BluetoothSensorMonitor> kvp in _connectedSensorDictionary)
			{
				// disconnect from the sensor
				kvp.Value.Disconnect();

				// add key to separate list to remove later (since can't remove from dictionary
				// now while we're iterating through it)
				sensorIdentifiers.Add(kvp.Key);
			}

			// now complete removal
			sensorIdentifiers.ForEach(key => DisposeSensorMonitor(key));
		}


		/// <summary>
		/// Gets all details associated with connected sensors.
		/// </summary>
		/// <returns>The all sensor details.</returns>
		public List<EventDetails> GetAllSensorDetails()
		{
			List<EventDetails> sensorDetailList = new List<EventDetails>(_connectedSensorsSorted.Count);
			Console.WriteLine($"count of connected sensors is {_connectedSensorsSorted.Count}");

			List<BluetoothSensorMonitor> listOfSensorsToDisconnect = new List<BluetoothSensorMonitor>();

			// add details from sensors currently connected
			foreach (BluetoothSensorMonitor sm in _connectedSensorsSorted)
			{
				// for hexoskin, set heart rate to whatever is the most recent retrieved from HTTP request
				if (sm.bIsHexoskinMonitor)
				{
					// Only add to details list if we're actually connected to hexoskin data
					if (_hexoskinManager.bConnectedToData)
					{
						sm.SensorDetails.PhysiologicalDetails.HeartRate = _hexoskinManager.HeartRate;
						sensorDetailList.Add(sm.SensorDetails);
					}
					else // otherwise add to list of devices to disconnect from
					{
						sm.SensorDetails.PhysiologicalDetails.HeartRate = -1;
						_hexoskinManager.StopGettingData();

						listOfSensorsToDisconnect.Add(sm);
					}

				}
				
				else
					sensorDetailList.Add(sm.SensorDetails);
			}

			// We tried to get details, but some sensors not connected.  Remove from connected list.
			if (listOfSensorsToDisconnect.Count > 0)
			{
				listOfSensorsToDisconnect.ForEach(s => DisconnectFromSensor(s.ID));

				// Use custom event to notify listeners that there is a change in connections
				OnSensorConnectionsChanged(new BluetoothConnectionChangedEventArgs
				{
					Peripheral = null,
					UpdatedConnectedDevicesString = GetConnectedDevicesString()
				});
			}

			return sensorDetailList;
		}


		public string GetAllHeartRateDataString()
		{
			StringBuilder sb = new StringBuilder();

			foreach (BluetoothSensorMonitor sm in _connectedSensorsSorted)
			{
				if (sm.SensorDetails.PhysiologicalDetails != null)
					sb.Append($"{sm.Name} - HR = {sm.SensorDetails.PhysiologicalDetails.HeartRate}\n");
			}

			//foreach (KeyValuePair<NSUuid, BluetoothSensorMonitor> kvp in _connectedSensorDictionary)
			//{
			//	if (kvp.Value.SensorDetails.PhysiologicalDetails != null)
			//		sb.Append($"{kvp.Value.Name} - HR = {kvp.Value.SensorDetails.PhysiologicalDetails.HeartRate}\n");
			//}

			return sb.ToString();
		}


		/// <summary>
		/// Returns a string indicated the devices that the phone is currently connected to, sorted descending by 
		/// time of connection (most recent will appear first)
		/// </summary>
		/// <returns>The connected devices string.</returns>
		public string GetConnectedDevicesString()
		{
			StringBuilder sb = new StringBuilder();

			if (_connectedSensorsSorted.Count > 0)
			{
				sb.Append("connected to ");

				foreach (BluetoothSensorMonitor sm in _connectedSensorsSorted)
				{
					if (sm.SensorDetails.PhysiologicalDetails != null)
						sb.Append($"{sm.Name}, ");
				}

				sb.Length -= 2; // to remove last ", "
			}
			else
				sb.Append("not connected");

			return sb.ToString();
		}


		/// <summary>
		/// Clean-up when disconnecting from sensor
		/// </summary>
		/// <param name="sensorIdentifier">Sensor identifier.</param>
		void DisposeSensorMonitor(NSUuid sensorIdentifier)
		{
			BluetoothSensorMonitor connectedMonitor = null;

			if ( _connectedSensorDictionary.TryGetValue(sensorIdentifier, out connectedMonitor))
			{
				if (connectedMonitor != null)
				{

					connectedMonitor.Dispose();
					connectedMonitor = null;

					RemoveFromConnectedSensors(sensorIdentifier);
				}
			}
		}
	}
}
