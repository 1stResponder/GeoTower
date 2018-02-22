
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;

namespace WatchTower.Droid
{
	[Activity(Label = "LeBluetoothActivity")]
	public class LeBluetoothActivity : Activity
	{

		private BluetoothAdapter _BtAdpt;
		private BackgroundWorker _Worker;
		private BluetoothDevice _LeDevice;
		private static System.Timers.Timer _ConnectionTimeout;
		private static System.Timers.Timer _ReadTimer;



		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
		}

		/*
		/// <summary>
		/// Initialize the Activity.
		/// </summary>
		private void Initialize()
		{
			// Initalizing Objects
			_BtAdpt = BluetoothAdapter.DefaultAdapter;

			// Setting timer for connection timeout
			//_ConnectionTimeout = new System.Timers.Timer(SCAN_TIMEOUT);
			//_ConnectionTimeout.AutoReset = false;
			//_ConnectionTimeout.Elapsed += onTimeOut;

			// Setting up background worker
			_Worker = new BackgroundWorker();
			_Worker.DoWork += connectDevice;
			_Worker.WorkerSupportsCancellation = true;
		}

		/// <summary>
		/// Starts the connection to the LE Bluetooth device. 
		/// </summary>
		/// <param name="dev">Dev.</param>
		public void startConnection(BluetoothDevice dev)
		{
			// Setting device for this connection
			_LeDevice = dev;

			// Starting thread
			_Worker.RunWorkerAsync();
		}

		/// <summary>
		/// Ends the connection to the LeBluetooth Device
		/// </summary>
		public void endConnection()
		{
			_Worker.CancelAsync();
			// also end gatt possibly 
		}

		/// <summary>
		/// Creates the connection for the new device and sets the callback
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void connectDevice(object sender, DoWorkEventArgs args)
		{
			BGattCallback bcallback = new BGattCallback();

			BluetoothGatt bg = _LeDevice.ConnectGatt(Application.Context, true, bcallback);
			//bg.Connect(POC_Constants.
		}

		/// <summary>
		/// Call back for reader.
		/// When the amount of time to read has been reached, this will cause the thread to sleep
		/// For the specified interval.
		/// </summary>
		/// <param name="source">Source.</param>
		/// <param name="e">E.</param>
		private void onReadTime(object source, System.Timers.ElapsedEventArgs e)
		{
			_ReadTimer.Enabled = false;
			//Thread.Sleep(): sleep for constant value
		}

		/// <summary>
		/// Bluetooth Connection Timeout
		/// Called if the bluetooth socket is not accepted within the specified time frame
		/// </summary>
		/// <param name="source">Source.</param>
		/// <param name="e">E.</param>
		private void onConnectionTimeOut(object source, System.Timers.ElapsedEventArgs e)
		{
			endConnection();
			// Log
		}


		/// <summary>
		/// Callback for Gatt Bluetooth Connection.  Class handles how data is handled
		/// </summary>
		private class BGattCallback : BluetoothGattCallback
		{
			// May be able to remove some of these later.  Despite these being optional (and not used) Android sometimes requires them to work properly.

			public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
			{
				base.OnCharacteristicChanged(gatt, characteristic);

				// Debug stuff
				byte[] data = characteristic.GetValue();
				string hexDa = BitConverter.ToString(data);

				// Code
				SensorHandler handle = new SensorHandler("MVSS");
				//handle.sendData(data);

				Thread.Sleep(POC_Constants.INTERVAL_BETWEEN_READ);
			}

			public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
			{
				base.OnCharacteristicRead(gatt, characteristic, status);
			}

			public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
			{
				base.OnCharacteristicWrite(gatt, characteristic, status);
			}

			public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
			{
				base.OnConnectionStateChange(gatt, status, newState);

				// Once it's connected start reading the data
				// We may want a timeout for this
				if (newState == ProfileState.Connected)
				{
					gatt.DiscoverServices();
				}
			}

			public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
			{
				base.OnDescriptorRead(gatt, descriptor, status);
			}

			public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
			{
				base.OnServicesDiscovered(gatt, status);

				// Getting Char/services UUIDS
				UUID serviceUUID = null;
				UUID charUUID = null;
				UUID ccdUUID = null;

				//UUID notCharUUID = null;
				//UUID charIDWRITE = null;
				//UUID clientUUID = null;

				// Can add other ones later or maybe swap out config files?
				if (gatt.Device.Name.StartsWith("MVSS", StringComparison.CurrentCulture))
				{
					serviceUUID = UUID.FromString(POC_Constants.POC_SERVICE_UUID);
					charUUID = UUID.FromString(POC_Constants.POC_NOTIFY_CHAR_UUID);
					ccdUUID = UUID.FromString(POC_Constants.POC_CCCD_UUID);
				}

				if (serviceUUID == null || charUUID == null || ccdUUID == null)
				{
					// dont continue
					// throw error? 
				}

				// Getting Service
				BluetoothGattService ser = gatt.GetService(serviceUUID);

				// Getting custom characteristic and enabling the notifications for it
				BluetoothGattCharacteristic cha = ser.GetCharacteristic(charUUID);
				gatt.SetCharacteristicNotification(cha, true);

				// Getting Descriptor from characteristic
				BluetoothGattDescriptor ds = cha.GetDescriptor(ccdUUID);

				// Setting desc to notify and indicate (ussually a charestic only has one of these properties but setting it when it don't exist wont cause problems)
				ds.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
				ds.SetValue(BluetoothGattDescriptor.EnableIndicationValue.ToArray());
				gatt.WriteDescriptor(ds);


			/* should not be needed
			 * 
					BluetoothGattCharacteristic chaWr = ser.GetCharacteristic(charIDWRITE);
					//chaWr.setWriteType(BluetoothGattCharacteristic.WriteTypeDefault);

					byte[] request = null;

					chaWr.SetValue(request);

					gatt.SetCharacteristicNotification(chaWr, true);

					// Getting Heart Rate Service 
					BluetoothGattService serHR = gatt.GetService(HEART_RATE_MEASUREMENT_SERVICE_UUID);

					// Getting Hear Rate Mes Char and enabling notifications
					BluetoothGattCharacteristic hr = serHR.GetCharacteristic(HEART_RATE_MEASUREMENT_CHARACTERISTIC_UUID);

					gatt.SetCharacteristicNotification(hr, true);

					// Getting Des
					BluetoothGattDescriptor dsHR = hr.GetDescriptor(clientUUID);

					// Setting desc to notify
					dsHR.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
					gatt.WriteDescriptor(dsHR);


					// Getting body sensor location

					BluetoothGattService blLoc = gatt.GetService(HEART_RATE_MEASUREMENT_SERVICE_UUID);

					BluetoothGattCharacteristic lc = blLoc.GetCharacteristic(BODY_SENSOR_LOCATION_CHARACTERISTIC_UUID);

					gatt.SetCharacteristicNotification(lc, true);*/
				/*
			}

			public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
			{
				base.OnDescriptorWrite(gatt, descriptor, status);
			}
		}
		*/

	} // End Class
} // End namespace
