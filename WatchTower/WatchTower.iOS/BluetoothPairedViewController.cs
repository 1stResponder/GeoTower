using Foundation;
using System;
using UIKit;
using CoreBluetooth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WatchTower.iOS
{
	/// <summary>
	/// Controller used to show list of available bluetooth devices
	/// </summary>
	public partial class BluetoothPairedViewController : UIViewController
	{
		readonly BT_TableViewSource _sensorListSource = new BT_TableViewSource();
		BluetoothSensorManager _bluetoothSensorManager = SingletonManager.BluetoothSensorManager;
		HexoskinManager _hexoskinManager = SingletonManager.HexoskinManager;

		// Background worker to handle scanning for bluetooth devices
		BackgroundWorkerWrapper _bgScanner;

		const int SCAN_INTERVAL = 30 * 1000;


		public BluetoothPairedViewController(IntPtr handle) : base(handle)
		{
		}


		/// <summary>
		/// Occurs after view associated with this controller loaded
		/// </summary>
		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			this.Title = "Sensor Pairing";

			// set data source for the table view
			DeviceTableView.Source = _sensorListSource;

			InitializeScanButton();

			// attach handlers to custom events so we'll know when peripheral discovered or sensor connected/disconnected
			_bluetoothSensorManager.DiscoveredPeripheral += OnDiscoveredPeripheral;
			_bluetoothSensorManager.SensorConnectionsChanged += OnSensorConnectionsChanged;

			// Set up background worker.  BG worker's method to do work just calls the stop scanning method in sensor manager
			_bgScanner = new BackgroundWorkerWrapper(CompleteScanning, FinishUIUpdates, SCAN_INTERVAL);
		}


		/// <summary>
		/// Occurs before associated view appears.  Set text here to avoid initial delay/flicker.
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			PairedDevicesLabel.Text = _bluetoothSensorManager.GetConnectedDevicesString();
		}


		/// <summary>
		/// Occurs after associated view loaded.
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);
		}


		/// <summary>
		/// Handler for the BluetoothSensorManager's DiscoveredPeripheral event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		void OnDiscoveredPeripheral(object sender, BluetoothDiscoveredPeripheralEventArgs e)
		{
			Console.WriteLine($"discovered {e.PeripheralName}");

			// add to underlying list and reload data
			_sensorListSource.Add(e.PeripheralName, e.Peripheral, e.bIsHexoskinPeripheral);
			DeviceTableView.ReloadData();
		}

		/// <summary>
		/// Handler for the BluetoothSensorManager's SensorConnectionsChanged event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		void OnSensorConnectionsChanged(object sender, BluetoothConnectionChangedEventArgs e)
		{
			// Invoke on main thread to trigger UI change
			InvokeOnMainThread(() =>
					{
					PairedDevicesLabel.Text = e.UpdatedConnectedDevicesString;
					});
		}


		/// <summary>
		/// Initializes the scan button.
		/// </summary>
		void InitializeScanButton()
		{
			ScanButton.SetTitle("Scan", UIControlState.Normal);
			ScanButton.SetTitle("Scanning...", UIControlState.Disabled);
		}


		/// <summary>
		/// Occurs when user presses scan button.
		/// </summary>
		/// <param name="sender">Sender.</param>
		partial void ScanUpInside(UIButton sender)
		{
			_sensorListSource.ClearMonitorList();

			ScanButton.Enabled = false;

			_bluetoothSensorManager.ScanForHeartRateMonitors();
			_bgScanner.StartWork(SCAN_INTERVAL);

		}


		/// <summary>
		/// Occurs when user presses connect button
		/// </summary>
		/// <param name="sender">Sender.</param>
		partial void ConnectButtonUpInside(UIButton sender)
		{
			ConnectToSelectedDevice();
		}


		/// <summary>
		/// Gets the selected peripheral.
		/// </summary>
		/// <returns>The selected peripheral.</returns>
		CBPeripheral GetSelectedPeripheral()
		{
			CBPeripheral peripheral = null;

			if (DeviceTableView.IndexPathForSelectedRow != null)
			{
				peripheral = _sensorListSource.GetPeripheralAtIndex(DeviceTableView.IndexPathForSelectedRow.Row);
				// _sensorListSource[DeviceTableView.IndexPathForSelectedRow.Row];
			}

			return peripheral;
		}


		/// <summary>
		/// Connects to selected device.
		/// </summary>
		void ConnectToSelectedDevice()
		{
			if (DeviceTableView.IndexPathForSelectedRow != null)
			{
				int rowIndex = DeviceTableView.IndexPathForSelectedRow.Row;

				// Hexoskins handled differently (for now) since we're not actually connecting via bluetooth.
				// Instead, we're hitting Hexoskin web API to get data.
				if (_sensorListSource.IsItemAtIndexHexoskin(rowIndex))
				{
					// First try to connect to hexoskin API
					if (!_bluetoothSensorManager.ConnectToHexoskinSensor())
					{
						// alert the user that we couldn't connect to the hexoskinr
						var alert = UIAlertController.Create("Failed to Connect", $"Could not connect to {_hexoskinManager.HexoskinName}.  Please check Watchtower settings and try again.", UIAlertControllerStyle.Alert);
						alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Cancel, null));

						PresentViewController(alert, animated: true, completionHandler: null);
					}
				}
				else
				{
					var peripheral = GetSelectedPeripheral();

					if (peripheral != null)
					{
						_bluetoothSensorManager.DisconnectFromSensor(peripheral.Identifier);
						_bluetoothSensorManager.ConnectToSensor(peripheral);
					}
				}
			}
		}


		/// <summary>
		/// Completes the scanning.
		/// </summary>
		void CompleteScanning()
		{
			_bluetoothSensorManager.StopScanning();

			InvokeOnMainThread(() =>
			{
				ScanButton.Enabled = true;
			});
		}

		/// <summary>
		/// Finishes the UI updates.
		/// </summary>
		void FinishUIUpdates()
		{

		}
	}
}
