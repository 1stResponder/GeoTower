using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Android.Bluetooth;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Util;
using Java.Lang;
using Java.Util;

namespace WatchTower.Droid
{
    public class BGattCallback : BluetoothGattCallback
    {

        #region Private data members

        // Handler for SensorDetails
        private SensorHandler handle;

        // Holds the device details
        private Dictionary<string, string> deviceDetails;

        // Read Timer, used for timeuts if no data comes in for awhile
        private System.Timers.Timer readTimer;

        // List, used for keeping track of charistics needed
        private List<BluetoothGattCharacteristic> deviceChar;
        private List<string> notifyChars;

        // Datetime for last update
        private DateTime lastChange;

        // Broadcast Receivers
        private SensorReadingBroadcastReceiver detailReceiver;
        private SensorStateBroadcastReceiver updateReceiver;

        // For logging
        private static readonly string TAG = typeof(BGattCallback).Name;

        #endregion

        #region Intialize
        /// <summary>
        /// Default constructor
        /// </summary>
        public BGattCallback()
        {
            Log.Debug(TAG, "Creating callback for LE Bluetooth Devices");

            // Init lists
            notifyChars = new List<string>();
            deviceDetails = new Dictionary<string, string>();

            // Creating read timeout timer
            readTimer = new System.Timers.Timer(BluetoothConstants.LE_TIMEOUT);
            readTimer.AutoReset = true;

            // Setting the last change to be the minimum value
            lastChange = new DateTime();
            lastChange = DateTime.MinValue;

            // Setting up the Receivers
            detailReceiver = new SensorReadingBroadcastReceiver();
            IntentFilter fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_READING_UPDATE_ACTION);
            fil.Priority = 100;
            Android.App.Application.Context.RegisterReceiver(detailReceiver, fil);

            updateReceiver = new SensorStateBroadcastReceiver();
            fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_CONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_DISCONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_REMOVED_ACTION);
            fil.Priority = 100;
            Android.App.Application.Context.RegisterReceiver(updateReceiver, fil);
        }
        #endregion
        #region Override

        /// <summary>
        /// Called when a characteristic changes
        /// </summary>
        /// <param name="gatt">Gatt</param>
        /// <param name="characteristic">Characteristic.</param>
        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);
            
            // Setting the last update to be now
            lastChange = DateTime.Now;

            // If this is one of chars we are tracking
            if (notifyChars.Contains(characteristic.Uuid.ToString()))
            {
                Log.Debug(TAG, "The Characteristic Changed");

                // Getting and setting the data
                byte[] data = characteristic.GetValue();
                handle.updateData(data);

                // Notifying
                Intent message = new Intent();
                message.SetAction(AppUtil.SENSOR_READING_UPDATE_ACTION);
                
                Bundle intentBundle = new Bundle();
                intentBundle.PutString(AppUtil.ADDRESS_KEY, gatt.Device.Address);
                intentBundle.PutString(AppUtil.DETAIL_KEY, handle.xmlDetail);
                message.PutExtras(intentBundle);
                
                SendBroadcast(message);
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);
            Log.Debug(TAG, "The Characteristic Was Read");
            lastChange = DateTime.Now;

            /// Reading the device data from the sensor
            // Each char only varies by one digit so only need to look at that single digit
            string uuid = characteristic.Uuid.ToString();
            uuid = uuid.Substring(0, uuid.IndexOf('-'));
            uuid = "" + uuid.Last();

            // Adding the device name if needed
            if (!deviceDetails.ContainsKey(BluetoothConstants.DEVICE_NAME))
            {
                deviceDetails.Add(BluetoothConstants.DEVICE_NAME, gatt.Device.Name);
            }

            // Holds the key
            string key = "";
            string value = characteristic.GetStringValue(0);
            
            // Getting which char was read
            switch (uuid)
            {
                case "4":
                    key = BluetoothConstants.MODEL_NUMBER;
                    Log.Debug(TAG, "The model number is: " + value);
                    goto default;
                case "5":
                    key = BluetoothConstants.SERIAL_NUMBER;
                    Log.Debug(TAG, "The serial number is: " + value);
                    goto default;
                case "6":
                    key = BluetoothConstants.FW_REV;
                    Log.Debug(TAG, "The firmware revision is: " + value);
                    goto default;
                case "7":
                    key = BluetoothConstants.HW_REV;
                    Log.Debug(TAG, "The hardware revision is: " + value);
                    goto default;
                case "8":
                    key = BluetoothConstants.SW_REV;
                    Log.Debug(TAG, "The software revision is: " + value);
                    goto default;
                default:
                    if (deviceDetails.ContainsKey(key))
                    {
                        deviceDetails[key] = value;
                    }
                    else
                    {
                        deviceDetails.Add(key, value);
                    }
                    break;
            }

            // Removing char from the list, it's been read successfully
            deviceChar.Remove(characteristic);

            // Disabling notifications for the char
            gatt.SetCharacteristicNotification(characteristic, false);

            // If there are more to read
            if (deviceChar.Count > 0)
            {
                requestCharacteristic(gatt);
            }
            else
            {
                // All the device info chars have been read
                connectToService(gatt);
            }

        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            // Enabling the readTimer
            readTimer.Elapsed += (sender, e) => readTimeout(sender, e, ref gatt);
            readTimer.Enabled = true;

            // Populating lastChange with the current time
            if (lastChange == DateTime.MinValue)
            {
                lastChange = DateTime.Now;
            }

            

            // Getting the device state         
            if (newState == ProfileState.Connected) // If the device is now connected, discover the services
            {
                gatt.DiscoverServices();
                sendStateUpdate(gatt.Device.Address, true, "Device has connected");
            }
            else if (newState == ProfileState.Disconnected || newState == ProfileState.Disconnecting) // if the device is no longer connected
            {
                // Notfying the service
                sendStateUpdate(gatt.Device.Address, false, "Device has disconnected");
            }
        }

        /// <summary>
        /// Connets the service and enable notifications for the chars we need 
        /// </summary>
        /// <param name="gatt">Gatt.</param>
		public void connectToService(BluetoothGatt gatt)
        {
            try
            {
                if (handle == null)
                {
                    handle = new SensorHandler(deviceDetails, AppConfig.UserID);
                }

                BluetoothDevice d = gatt.Device;

                // Getting Char/services UUIDS
                UUID serviceUUID = null;
                UUID charUUID = null;
                UUID ccdUUID = UUID.FromString(BluetoothConstants.CCD_UUID);

                // Can add other ones later or maybe swap out config files?
                if (gatt.Device.Name.StartsWith("MVSS", StringComparison.CurrentCulture))
                {
                    serviceUUID = UUID.FromString(BluetoothConstants.MVSS_SERVICE);
                    charUUID = UUID.FromString(BluetoothConstants.MVSS_CHAR);
                }

                if (gatt.Device.Name.StartsWith("Zephyr", StringComparison.CurrentCulture))
                {
                    serviceUUID = UUID.FromString(BluetoothConstants.HEART_RATE_SERVICE);
                    charUUID = UUID.FromString(BluetoothConstants.HEART_RATE_CHAR);
                }


                /*	if (serviceUUID == null || charUUID == null || ccdUUID == null)
                    {
                        // dont continue
                        // throw error? 
                    }*/

                notifyChars.Add(serviceUUID.ToString());
                notifyChars.Add(charUUID.ToString());

                // Getting Service
                BluetoothGattService ser = gatt.GetService(serviceUUID);

                // Getting custom characteristic and enabling the notifications for it
                BluetoothGattCharacteristic cha = ser.GetCharacteristic(charUUID);

                // Getting Descriptor from characteristic
                BluetoothGattDescriptor ds = cha.GetDescriptor(ccdUUID);

                // Setting desc to notify
                ds.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                //ds.SetValue(BluetoothGattDescriptor.EnableIndicationValue.ToArray());
                gatt.WriteDescriptor(ds);

                // Enabling the notifications
                gatt.SetCharacteristicNotification(cha, true);

            }
            catch (NullReferenceException e)
            {
                // Service could not be reached
                // How to get this back to the user?
                string t = e.ToString();
            }
        }

        /// <summary>
        /// Starts the read for device info chars
        /// </summary>
        /// <param name="gatt">Gatt.</param>
        /// <param name="status">Status.</param>
		public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            lastChange = DateTime.Now;

            // Get the Device Details
            getDeviceDetails(gatt);

        }
        #endregion

        #region timer method

        public void readTimeout(object sender, EventArgs e, ref BluetoothGatt gatt)
        {
            TimeSpan duration = DateTime.Now - lastChange;

            if (duration.TotalMilliseconds > BluetoothConstants.LE_TIMEOUT)
            {
                readTimer.Enabled = false;
                // Sensor took too long to report our data
                gatt.Disconnect();
            }
        }

        #endregion

        private void sendStateUpdate(string deviceAddress, bool isConnected, string message)
        {
            // Creating the intent
            Intent intent = new Intent();
            Bundle intentBundle = new Bundle();
            intentBundle.PutString(AppUtil.ADDRESS_KEY, deviceAddress);
            intent.PutExtras(intentBundle);
            

            // set action depending on state
            if (isConnected)
            {
                intent.SetAction(AppUtil.SENSOR_CONNECT_ACTION);
            }
            else
            {
                intent.SetAction(AppUtil.SENSOR_DISCONNECT_ACTION);
            }

            // Send the notification
            SendBroadcast(intent);
        }

        #region Helper Methods
        
        private void SendBroadcast(Intent intent)
        {
            Android.App.Application.Context.SendBroadcast(intent, null);
        }


        #endregion
        
        #region Read Device Info Methods

        /// <summary>
        /// Used to read from the list of characteristics
        /// </summary>
        /// <param name="gatt">Gatt.</param>
        private void requestCharacteristic(BluetoothGatt gatt)
        {
            gatt.ReadCharacteristic(deviceChar.Last());
        }

        /// <summary>
        /// Get the UUID's for the device details and sets the list
        /// </summary>
        /// <param name="gatt">Gatt.</param>
		private void getDeviceDetails(BluetoothGatt gatt)
        {
            try
            {
                UUID deviceInfoUUID = UUID.FromString(BluetoothConstants.DEVICE_INFO_SERVICE);
                BluetoothGattService deviceInfoSer = gatt.GetService(deviceInfoUUID);

                deviceChar = new List<BluetoothGattCharacteristic> {
                deviceInfoSer.GetCharacteristic(UUID.FromString(BluetoothConstants.DEVICE_SERIALNUM)),
                deviceInfoSer.GetCharacteristic(UUID.FromString(BluetoothConstants.DEVICE_MODELNUM)),
                deviceInfoSer.GetCharacteristic(UUID.FromString(BluetoothConstants.DEVICE_SOFTWARE_REV)),
                deviceInfoSer.GetCharacteristic(UUID.FromString(BluetoothConstants.DEVICE_FIRMWARE_REV)),
                deviceInfoSer.GetCharacteristic(UUID.FromString(BluetoothConstants.DEVICE_HARDWARE_REV))
            };

                foreach (BluetoothGattCharacteristic c in deviceChar)
                {
                    try
                    {
                        gatt.SetCharacteristicNotification(c, false);
                        requestCharacteristic(gatt);
                    }
                    catch (System.Exception e)
                    {
                        string t = "";
                        // if the char dont exit for thiss
                    }
                }

            } catch (System.Exception e)
            {
                // stop ourselves
                sendStateUpdate(gatt.Device.Address, false, "Device could not be read from: " + e.Message);
            }
        }

        #endregion
        #region Implemented Default
        
        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorWrite(gatt, descriptor, status);
        }

        public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorRead(gatt, descriptor, status);
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicWrite(gatt, characteristic, status);
        }
        #endregion

    } // End Class
} // End namespace
