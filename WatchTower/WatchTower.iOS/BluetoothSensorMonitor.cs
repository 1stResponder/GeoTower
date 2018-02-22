using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreBluetooth;
using EMS.NIEM.Sensor;
using Foundation;

namespace WatchTower.iOS
{
	/// <summary>
	/// Main class to use as go-between between phone and a sensor.  The connection is actually established
	/// using a CBCentralManager, but objects of this class are used to:
	/// 
	/// 1) represent, and
	/// 2) communicate with 
	/// 
	/// the sensor once the connection is established.
	/// 
	/// Note that most of the communication with the sensor is accomplished using a CBPeripheralDelegate which listens for 
	/// events raised by the BlueTooth sensor.
	/// </summary>
	public class BluetoothSensorMonitor : IComparable<BluetoothSensorMonitor>
	{
		bool disposed;

		// to use for tracking global sorting order
		static int _creationOrder = 0;

		// The manager used to actually connect or disconnect from sensor 
		public CBCentralManager Manager { get; }

		// The peripheral (sensor) associated with this object
		public CBPeripheral Peripheral { get; }

		// The workaround manager for hexoskin since we can't connect directly
		HexoskinManager _hexoskinManager = SingletonManager.HexoskinManager;

		string _name;

		public string Name
		{
			get
			{
				return _name;
			}
		}

		// The ID associated with the peripheral that this monitor is associated with
		public NSUuid ID { get; private set; }

		// The object used to handle parsing data from the sensor
		public SensorHandler SensorHandler { get; private set; }

		WatchTowerSettings _watchTowerSettings = SingletonManager.WatchTowerSettings;

		public bool PresentButDisconnected { get; set; }

		// Used to give this an order relative to another monitor
		public int CreationOrder { get; private set; }

		public bool bIsHexoskinMonitor { get; private set; }

		/// <summary>
		/// Gets the sensor details.
		/// </summary>
		/// <value>The sensor details.</value>
		public SensorDetail SensorDetails
		{
			get { return SensorHandler.Detail; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WatchTower.iOS.BluetoothSensorMonitor"/> class.
		/// </summary>
		/// <param name="hexoskinName">Hexoskin name.</param>
		/// <param name="id">Identifier.</param>
		public BluetoothSensorMonitor(string hexoskinName, NSUuid id)
		{
			bIsHexoskinMonitor = true;
			_name = hexoskinName;
			ID = id;

			SensorHandler = new SensorHandler(GetDeviceDetailsMap(hexoskinName), _watchTowerSettings.UserID);
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="T:WatchTower.iOS.BluetoothSensorMonitor"/> class.
		/// </summary>
		/// <param name="manager">Manager.</param>
		/// <param name="peripheral">Peripheral.</param>
		public BluetoothSensorMonitor(CBCentralManager manager, CBPeripheral peripheral)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));

			if (peripheral == null)
				throw new ArgumentNullException(nameof(peripheral));

			// set creation order to allow for sorting
			this.CreationOrder = ++BluetoothSensorMonitor._creationOrder;

			Manager = manager;

			Peripheral = peripheral;
			Peripheral.Delegate = new SensorPeripheralDelegate(this);

			// Find the services advertised by the peripheral
			Peripheral.DiscoverServices();
			SensorHandler = new SensorHandler(GetDeviceDetailsMap(peripheral.Name), _watchTowerSettings.UserID);

			PresentButDisconnected = false;

			bIsHexoskinMonitor = false;
			_name = peripheral.Name;
		}


		/// <summary>
		/// Create a map for device details
		/// </summary>
		/// <returns>The device details map.</returns>
		/// <param name="peripheralName">Peripheral name.</param>
		Dictionary<string, string> GetDeviceDetailsMap(string peripheralName)
		{
			Dictionary<string, string> deviceDetailsMap = new Dictionary<string, string>();

			deviceDetailsMap.Add(BluetoothConstants.FW_REV, "1.0");
			deviceDetailsMap.Add(BluetoothConstants.HW_REV, "1.0");
			deviceDetailsMap.Add(BluetoothConstants.SW_REV, "1.0");
			deviceDetailsMap.Add(BluetoothConstants.SERIAL_NUMBER, "1234ABCD");
			deviceDetailsMap.Add(BluetoothConstants.MODEL_NUMBER, "1.0");


			deviceDetailsMap.Add(BluetoothConstants.DEVICE_NAME, peripheralName);

			return deviceDetailsMap;
		}


		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			disposed = true;

			if (!disposing)
				return;

			if (Peripheral != null
			    && Peripheral.Delegate != null)
			{
				Peripheral.Delegate.Dispose();
				Peripheral.Delegate = null;
			}
		}


		/// <summary>
		/// Connects to the associated peripheral and sets the ID for this object
		/// </summary>
		public void Connect()
		{
			if (disposed)
				return;

			Manager.ConnectPeripheral(Peripheral, new PeripheralConnectionOptions
			{
				NotifyOnDisconnection = true
			});

			ID = Peripheral.Identifier;
			//OnNameUpdated();
		}


		/// <summary>
		/// Disconnects from the associated peripheral
		/// </summary>
		public void Disconnect()
		{
			if (disposed)
				return;

			if (Peripheral != null)
				Manager.CancelPeripheralConnection(Peripheral);
		}


		/// <summary>
		/// Updates the data associated with the sensor
		/// </summary>
		/// <param name="hr">Hr.</param>
		void UpdateData(NSData hr)
		{
			var now = DateTime.Now;

			// A byte array to store data from sensor
			byte[] data = new byte[hr.Length];

			// Copy data from sensor data to byte array
			System.Runtime.InteropServices.Marshal.Copy(hr.Bytes, data, 0, Convert.ToInt32(data.Length));

			// Then send to sensor handler for parsing and processing
			SensorHandler.updateData(data);
		}

		/// <summary>
		/// To allow for sorting
		/// </summary>
		/// <returns></returns>
		/// <param name="other">Other.</param>
		public int CompareTo(BluetoothSensorMonitor other)
		{
			return other.CreationOrder.CompareTo(this.CreationOrder);
		}


		/// <summary>
		/// Returns a simple string representing the data we want to show in UI associated with the sensor
		/// </summary>
		/// <returns>The connected sensor UIS tring.</returns>
		public string GetConnectedSensorUIString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(this.Name);

			if (this.bIsHexoskinMonitor)
				this.SensorDetails.PhysiologicalDetails.HeartRate = _hexoskinManager.HeartRate;

			if (this.SensorDetails.PhysiologicalDetails != null)
				sb.Append($"HR = {this.SensorDetails.PhysiologicalDetails.HeartRate}");

			return sb.ToString();
		}


		/// <summary>
		/// Class used to listen for events raised by peripheral once connected
		/// </summary>
		sealed class SensorPeripheralDelegate : CBPeripheralDelegate
		{
			readonly BluetoothSensorMonitor monitor;

			// Temporary - probably want to put some of this processing on another thread and only read/update 
			// during certain time intervals
			DateTime _dtCurrentUpdateEnd = DateTime.MinValue;
			DateTime _dtNextTimeToUpdate = DateTime.MinValue;
			int _secondsBetweenUpdateProcessing = 10; // seconds
			int _secondsToGatherUpdates = 5; // seconds
			bool _bUpdateOccurring = false;

			public SensorPeripheralDelegate(BluetoothSensorMonitor monitor)
			{
				this.monitor = monitor;
			}

			/// <summary>
			/// Occurs when a new service is discovered for the peripheral
			/// </summary>
			/// <param name="peripheral">Peripheral.</param>
			/// <param name="error">Error.</param>
			public override void DiscoveredService(CBPeripheral peripheral, NSError error)
			{
				if (monitor.disposed)
					return;

				foreach (var service in peripheral.Services)
				{
					// Right now limited to POC and Zephyr services
					if (service.UUID == BluetoothIdentifiers.POCServiceUUID
						|| service.UUID == BluetoothIdentifiers.ZephyrServiceUUID)//GetCBUUID(S_PERIPHERAL_UUID))
					{
						peripheral.DiscoverCharacteristics(service);
					}

					Console.WriteLine($"discovered service: {service.UUID}");
				}
			}


			/// <summary>
			/// Occurs when a new characteristic is discovered for  aservice
			/// </summary>
			/// <param name="peripheral">Peripheral.</param>
			/// <param name="service">Service.</param>
			/// <param name="error">Error.</param>
			public override void DiscoveredCharacteristic(CBPeripheral peripheral,
				CBService service, NSError error)
			{
				if (monitor.disposed)
					return;

				foreach (var characteristic in service.Characteristics)
				{
					Console.WriteLine($"discovered characteristic: {characteristic.UUID}");

					// Right now limited to POC and Zephyr hear rate characteristics
					if (characteristic.UUID == BluetoothIdentifiers.POCHeartRateMeasurementCharacteristicUUID
						|| characteristic.UUID == BluetoothIdentifiers.ZephyrHeartRateCharacteristicUUID)
					{
						service.Peripheral.SetNotifyValue(true, characteristic);
					}
				}
			}


			/// <summary>
			/// Occurs when a Descripter for a characteristic is discovered
			/// </summary>
			/// <param name="peripheral">Peripheral.</param>
			/// <param name="characteristic">Characteristic.</param>
			/// <param name="error">Error.</param>
			public override void DiscoveredDescriptor(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
			{
				if (monitor.disposed)
					return;

				foreach (var descriptor in characteristic.Descriptors)
				{
					Console.WriteLine($"discovered descriptor: {descriptor.UUID}");

				}
			}

			public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error)
			{
				base.UpdatedValue(peripheral, descriptor, error);
			}


			/// <summary>
			/// Occurs when the value for a characteristic is updated
			/// </summary>
			/// <param name="peripheral">Peripheral.</param>
			/// <param name="characteristic">Characteristic.</param>
			/// <param name="error">Error.</param>
			public override void UpdatedCharacterteristicValue(
				CBPeripheral peripheral,
				CBCharacteristic characteristic, NSError error)
			{
				DateTime dtNow = DateTime.Now;

				/* If we haven't yet reach cutoff time for current update,
				 * OR we've passed the time after which we want to start updates...*/
				if ((dtNow < _dtCurrentUpdateEnd)
				   		||
					((dtNow > _dtNextTimeToUpdate)))
				{
					//Console.WriteLine($"updating value for characteristic {characteristic.UUID}");
					if (monitor.disposed || error != null || characteristic.Value == null)
						return;

					if (characteristic.UUID == BluetoothIdentifiers.POCHeartRateMeasurementCharacteristicUUID
						|| characteristic.UUID == BluetoothIdentifiers.ZephyrHeartRateCharacteristicUUID)
						monitor.UpdateData(characteristic.Value);

					// If no update is currently occurring, set flag and update next times
					if (!_bUpdateOccurring)
					{
						// set flag for a new update session
						_bUpdateOccurring = true;

						// calculate ending time of current session
						_dtCurrentUpdateEnd = dtNow.AddSeconds(_secondsToGatherUpdates);

						// calculate next starting time based on ending time
						_dtNextTimeToUpdate = _dtCurrentUpdateEnd.AddSeconds(_secondsBetweenUpdateProcessing);
					}
				}
				else
				{
					_bUpdateOccurring = false;
				}
			}

		}
	}
}
