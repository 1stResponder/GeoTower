
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using EMS.NIEM.Sensor;
using Java.Lang;

namespace WatchTower.Droid
{
    public class SensorFragment : Fragment
    {
        #region private members
        View myView;
        Context appContext;
        private static ArrayAdapter<string> _connectedDevicesArrayAdapter;
        private static ArrayAdapter<string> _spinnerDevicesArrayAdapter;
        private static ArrayAdapter<string> _disconnectedDevicesArrayAdapter;

        private RadioButton activeDeviceButton;
        private RadioButton inactiveDeviceButton;
        private RadioGroup radioGroupList;
        
        private ListView _connectDeviceListView;
        private ListView _disconnectedDeviceListView;
        
        private SensorReadingBroadcastReceiver sensorReadingReceiver;
        private SensorStateBroadcastReceiver sensorStateReceiver;
        private Button _addDeviceButton;
        private BluetoothAdapter _adpt;
        private Switch _scanSwitch;

        private Spinner sensorChoice;
        private View liveLayout;
        private static TextView hrRate;
        private static TextView hrLastUpdate;
        private BluetoothDevice selectedDevice;
        DateTime lastUpdate;
        System.Timers.Timer sentTimeUpdate;
        
        private List<string> _connectedDevices;
        private List<string> _disconnectedDevices;

        private Dictionary<string, bool> _connectedDeviceMap;

        #endregion
        #region constants

        private const string TAG = "SensorFragment";

        // The constant strings values from the Resource
        private static readonly string LE_SENSOR_WARNING_NO_CONNECT = AppUtil.GetResourceString(Resource.String.le_sensor_warning_no_connect);
        private static readonly string LE_SENSOR_WARNING_REMOVEDLAST = AppUtil.GetResourceString(Resource.String.le_sensor_warning_removedLast);
        private static readonly string NEW_DEVICE_ADDED = AppUtil.GetResourceString(Resource.String.new_device_added);
        private static readonly string DISCONNECT_CONFIRM_MESSAGE = AppUtil.GetResourceString(Resource.String.disconnect_confirm_message);
        private static readonly string DISCONNECT_CONFIRM_TITLE = AppUtil.GetResourceString(Resource.String.disconnect_confirm_title);
        private static readonly string DISCONNECT_CONFIRM_TOAST = AppUtil.GetResourceString(Resource.String.disconnect_confirm_toast);
        
        private static readonly string RECONNECT_CONFIRM_MESSAGE = AppUtil.GetResourceString(Resource.String.reconnect_confirm_message);
        private static readonly string RECONNECT_CONFIRM_TITLE = AppUtil.GetResourceString(Resource.String.reconnect_confirm_title);
        private static readonly string RECONNECT_CONFIRM_TOAST = AppUtil.GetResourceString(Resource.String.reconnect_confirm_toast);
        
        private static readonly string NO_CONNECTED_DEVICES = AppUtil.GetResourceString(Resource.String.no_connected_devices);
        private static readonly string NO_BLUETOOTH_WARNING = AppUtil.GetResourceString(Resource.String.no_bluetooth_warning);
        private static readonly string DEF_VALUE = AppUtil.GetResourceString(Resource.String.def_value);

        #endregion

        #region init

        /// <summary>
        /// On Create view, creates the adapters
        /// </summary>
        /// <returns>The create view.</returns>
        /// <param name="inflater">Inflater.</param>
        /// <param name="container">Container.</param>
        /// <param name="savedInstanceState">Saved instance state.</param>
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            myView = inflater.Inflate(Resource.Layout.sensors_layout, container, false);
            appContext = container.Context;

            // Intializing Spinner
            sensorChoice = (Spinner)myView.FindViewById<Spinner>(Resource.Id.sensor_spinner);

            liveLayout = myView.FindViewById(Resource.Id.liveLayoutView);
            hrRate = (TextView)myView.FindViewById<TextView>(Resource.Id.hr_value);
            hrLastUpdate = (TextView)myView.FindViewById<TextView>(Resource.Id.hr_live_last_update);
            hrLastUpdate.SetFilters(new IInputFilter[] { new Filter.TimeFilter(3) });
            
            // Inializing Values
            Initialize();

            // Initializng Adapters
            _connectDeviceListView = myView.FindViewById<ListView>(Resource.Id.connected_devices);
            _disconnectedDeviceListView = myView.FindViewById<ListView>(Resource.Id.disconnected_devices);

            _connectedDevicesArrayAdapter = new ArrayAdapter<string>(this.Activity, Resource.Layout.device_name, _connectedDevices);
            _connectDeviceListView.Adapter = _connectedDevicesArrayAdapter;
            
            _spinnerDevicesArrayAdapter = new ArrayAdapter<string>(this.Activity, Resource.Layout.spinner_item, _connectedDevices);
            sensorChoice.Adapter = _spinnerDevicesArrayAdapter;
            
            _disconnectedDevicesArrayAdapter = new ArrayAdapter<string>(this.Activity, Resource.Layout.device_name_inactive, _disconnectedDevices);
            _disconnectedDeviceListView.Adapter = _disconnectedDevicesArrayAdapter;

            activeDeviceButton = myView.FindViewById<RadioButton>(Resource.Id.activeConnectedRadioButton);
            activeDeviceButton.SetTextColor(Color.Green);
            
            inactiveDeviceButton = myView.FindViewById<RadioButton>(Resource.Id.inActiveConnectedRadioButton);
            inactiveDeviceButton.SetTextColor(Color.DarkGray);

            radioGroupList = myView.FindViewById<RadioGroup>(Resource.Id.rdGrpList);
            radioGroupList.CheckedChange += OnRadioButtonClick;   
            
            sensorChoice.ItemSelected += spinner_ItemSelected;

            _connectDeviceListView.ItemClick += activedeviceClick;
            _disconnectedDeviceListView.ItemClick += inactivedeviceClick;
            return myView;
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        public void Initialize()
        {
            _adpt = BluetoothAdapter.DefaultAdapter;

            _addDeviceButton = (Button)myView.FindViewById(Resource.Id.addDevice);
            _addDeviceButton.Click += (sender, e) => buttonClick(sender, e);

            _connectedDevices = new List<string>();
            _disconnectedDevices = new List<string>();

            // Switch that controls whether messages are sent about this sensor
            _scanSwitch = (Switch)myView.FindViewById(Resource.Id.sensor_send_switch);
            _scanSwitch.CheckedChange += switchClick;

            // Broadcast res
            sensorReadingReceiver = new SensorReadingBroadcastReceiver();
            sensorReadingReceiver.OnSensorReading += onNewReading;

            sensorStateReceiver = new SensorStateBroadcastReceiver();
            sensorStateReceiver.SensorDisconnect += DisconnectSensor;
            sensorStateReceiver.SensorAdded += AddSensor;
            sensorStateReceiver.SensorRemoved += RemoveSensor;
            sensorStateReceiver.SensorReportingPaused += PauseSensor;
            sensorStateReceiver.SensorConnect += ConnectSensor;

            resetLiveView();

			sentTimeUpdate = new System.Timers.Timer();
			sentTimeUpdate.Interval = 1000 * 5;
			sentTimeUpdate.Elapsed += updateLastSent;
			sentTimeUpdate.AutoReset = true;

            _connectedDeviceMap = new Dictionary<string, bool>();
            
            // broadcast receiver
            IntentFilter fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_READING_UPDATE_ACTION);
            fil.Priority = 99;
            Android.App.Application.Context.RegisterReceiver(sensorReadingReceiver, fil);
            

            fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_CONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_DISCONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_ADDED_ACTION);
            fil.AddAction(AppUtil.SENSOR_REMOVED_ACTION);
            fil.AddAction(AppUtil.SENSOR_PAUSE_ACTION);
            fil.Priority = 91;
            Android.App.Application.Context.RegisterReceiver(sensorStateReceiver, fil);
        }
        

        #endregion
        #region Override methods
        
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);   
        }
        
        /// <summary>
        /// On Fragment Start, Inialize the objects
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
        }

        public override void OnResume()
        {
            base.OnResume();
            
            resetLiveView();
            enableBLView();
            
            sentTimeUpdate.Enabled = true;

            disableSwitch();
            if (AppConfig.PostSensorUpdates)
            {
                _scanSwitch.Checked = true;
            }
            else
            {
                _scanSwitch.Checked = false;
            }
            enableSwitch();

            // Populating list view with already connected devices
            populateConnectedDeviceList();

            if (!AppUtil.checkIfBluetoothEnabled(_adpt))
            {
                // Bluetooth not enabled
                Toast.MakeText(appContext, NO_BLUETOOTH_WARNING, ToastLength.Short).Show();
                Log.Error(TAG, "Bluetooth could not be enabled");

                disableBLView();
            }
            
           // radioGroupList.ClearCheck();
        }

        public override void OnStop()
        {
            base.OnStop();
        }

        public override void OnPause()
        {
            disableSwitch();
            base.OnPause();
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            Android.App.Application.Context.UnregisterReceiver(sensorReadingReceiver);
            Android.App.Application.Context.UnregisterReceiver(sensorStateReceiver);
        }

        #endregion
        #region Callback methods

        private void OnRadioButtonClick(object sender, EventArgs e)
        {
            RadioGroup g = (RadioGroup)sender;

            int radioID = g.CheckedRadioButtonId;
            RadioButton b = (RadioButton)myView.FindViewById(radioID);

            if(b == activeDeviceButton)
            {   
                ((MainActivity)Activity).RunOnUiThread(delegate
                {
                    b.SetTextColor(Color.Green);
	                inactiveDeviceButton.SetTextColor(Color.DarkGray);
	                
	                _disconnectedDeviceListView.Visibility = ViewStates.Gone;
	                _connectDeviceListView.Visibility = ViewStates.Visible;
                 });
                
                
            } else
            {
                ((MainActivity)Activity).RunOnUiThread(delegate
                {
                    b.SetTextColor(Color.Green);
	                activeDeviceButton.SetTextColor(Color.DarkGray);
	
	                _connectDeviceListView.Visibility = ViewStates.Gone;
	                _disconnectedDeviceListView.Visibility = ViewStates.Visible;
                });    
            }        
        }

        /// <summary>
        /// Callback for spinner that holds all active connected devices
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            List<string> activeAddress = _connectedDeviceMap.Where(kvp => kvp.Value == true).Select(kvp => kvp.Key).ToList();

            if (activeAddress.Count > 0)
            {
                // parse address
                var info = (e.View as TextView).Text.ToString();
                string address = info.Substring(info.Length - 17);

                // get device
                selectedDevice = AppUtil.getDevice(address, _adpt);
            }
            resetLiveView();  
        }

        private void DisconnectSensor(object sender, SensorStateEventArgs e)
        {
            ResetSensor();
        }

        private void AddSensor(object sender, SensorStateEventArgs e)
        {
            ResetSensor();
        }
        
        private void RemoveSensor(object sender, SensorStateEventArgs e)
        {
            ResetSensor();
        }
        
        private void ConnectSensor(object sender, SensorStateEventArgs e)
        {           
        }
        
        private void PauseSensor(object sender, SensorStateEventArgs e)
        {
        }

        private void ResetSensor()
        {
            populateConnectedDeviceList();
        }     
        
		/// <summary>
		/// Callback for new sensor detail
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Sensor event args</param>
		private void onNewReading(object sender, SensorEventArgs e)
		{
			// check if the address is the one we want
			if (selectedDevice != null && selectedDevice.Address == e.Address)
			{
				((MainActivity)Activity).RunOnUiThread(delegate
				{
					hrRate.SetText("" + e.Detail.PhysiologicalDetails.HeartRate, TextView.BufferType.Normal);

                    // If this is a hexoskin and the reading time is not the minmum
                    if ((AppUtil.isHexoSkin(selectedDevice)) && (e.ReadingTime != DateTime.MinValue))
                    {
                        hrLastUpdate.SetText(e.ReadingTime.ToString(), TextView.BufferType.Normal);
                    }
                    else
                    {
                        hrLastUpdate.SetText(DateTime.Now.ToString(), TextView.BufferType.Normal);
                    }
				});
			}
		}

		/// <summary>
		/// Callback for active bluetooth device click
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void activedeviceClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			// get the address
			var info = (e.View as TextView).Text.ToString();
			string address = info.Substring(info.Length - 17);

			confirmDisconnect(address);
		}
        
        /// <summary>
        /// Callback for inactive bluetooth device click
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void inactivedeviceClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // need to give choice to remove or disconnect as well!
            var info = (e.View as TextView).Text.ToString();
            string address = info.Substring(info.Length - 17);
            inactiveDeviceClickDialog(address);
        }

		/// <summary>
		/// Call back for when switch is changed
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void switchClick(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			Switch s = (Switch)sender;

            if (s.Checked)
            {
                if (!AppConfig.PostSensorUpdates)
                {
                    if (_connectedDeviceMap.Count > 0)
                    {
                        AppConfig.PostSensorUpdates = true;
                        showLiveView(); 

                        populateConnectedDeviceList();
                        
                        // Resetting status of each device and Notifying
                        foreach(string address in _connectedDeviceMap.Keys.ToList())
                        {
                          //  _connectedDeviceMap[address] = true;
                            AddDevice(address);
                        }
                        
                    } else
                    {
                        Log.Warn(TAG, "No connected devices.  Sensor reporting will remain off");

	                    // Disabling check
	                    s.Checked = false;
	
	                    // Give message
	                    Toast.MakeText(appContext, NO_CONNECTED_DEVICES, ToastLength.Short).Show();
                    }
                }
            } 
            else
            {
                if (AppConfig.PostSensorUpdates)
                {
                    AppConfig.PostSensorUpdates = false;
                    hideLiveView();
                    PauseSensorReporting();
                }
            }
		}

		/// <summary>
		/// Callback for the bluetooth list activity.  Starts the service for specified device
		/// </summary>
		/// <param name="requestCode">Request code.</param>
		/// <param name="resultCode">Result code.</param>
		/// <param name="data">Data.</param>
		public override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			if (data != null && data.Extras != null)
			{
                Bundle b = data.Extras;
                
				string address = b.GetString(AppUtil.ADD_DEVICE_KEY);
                string name = b.GetString(AppUtil.NAME_KEY);
                
                Toast.MakeText(Android.App.Application.Context, "The Sensor Was Added", ToastLength.Short).Show();

				// If an address was returned
				if (address != null)
				{
                    AddDevice(address, name);
				} 
			}
		}
        
        private void AddDevice(string address, string name = "")
        {
            Log.Debug(TAG, System.String.Format("Sensor with address {0} was added", address));
            
            /*if( _connectedDeviceMap.ContainsKey(address))
            {
                _connectedDeviceMap[address] = true;
            } else
            {
                _connectedDeviceMap.Add(address, true);
            }*/
            
          //  resetListViews();
            
            // Notifying
            Intent message = new Intent();
            message.SetAction(AppUtil.SENSOR_ADDED_ACTION);
            Bundle intentBundle = new Bundle();
            intentBundle.PutString(AppUtil.ADDRESS_KEY, address);
            intentBundle.PutString(AppUtil.NAME_KEY, name);
            message.PutExtras(intentBundle);

            SendBroadcast(message);
        }
        
        private void RemoveDevice(string address)
        {
            Log.Debug(TAG, System.String.Format("Sensor with address {0} was removed", address));
            Toast.MakeText(Android.App.Application.Context, "The Sensor Was Removed", ToastLength.Short).Show();
            
            //_connectedDeviceMap.Remove(address);
            
           // resetListViews();
            
            // Notifying
            Intent message = new Intent();
            message.SetAction(AppUtil.SENSOR_REMOVED_ACTION);
                
            Bundle intentBundle = new Bundle();
            intentBundle.PutString(AppUtil.ADDRESS_KEY, address);
            message.PutExtras(intentBundle);
                
            SendBroadcast(message);
        }
        
        private void PauseSensorReporting()
        {
            // Notifying
            Intent message = new Intent();
            message.SetAction(AppUtil.SENSOR_PAUSE_ACTION);   
            SendBroadcast(message);
        }
        
        private void SendBroadcast(Intent intent)
        {
            Android.App.Application.Context.SendOrderedBroadcast(intent, null);
        }

		/// <summary>
		/// Updates the last sent timer
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void updateLastSent(object sender, System.Timers.ElapsedEventArgs args)
		{
			((MainActivity)Activity).RunOnUiThread(delegate
			{
				if (lastUpdate != DateTime.MinValue)
				{
					hrRate.SetText(lastUpdate.ToString(), TextView.BufferType.Normal);
				}
			});
		}

		/// <summary>
		/// Call back for button click.  Starts the BluetoothListActivity
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		public void buttonClick(object sender, EventArgs e)
		{
			scanForDevices();
		}


		#endregion

		#region Helper methods
        
        /// <summary>
        /// Handles the prompt for inactive devices
        /// </summary>
        /// <param name="address">Address.</param>
        public void inactiveDeviceClickDialog(string address)
        {
            Android.App.AlertDialog.Builder deleteAlert = new Android.App.AlertDialog.Builder(appContext);
            deleteAlert.SetTitle(DISCONNECT_CONFIRM_TITLE);
            deleteAlert.SetMessage(DISCONNECT_CONFIRM_MESSAGE);

            deleteAlert.SetNegativeButton(Resource.String.yes, (senderAlert, args) =>
            {
                RemoveDevice(address);
            });

            deleteAlert.SetPositiveButton(Resource.String.no, (senderAlert, args) =>
            {
            });
            
            deleteAlert.SetNeutralButton(Resource.String.reconnect, (senderAlert, args) =>
            {
                // Alert user
                Toast.MakeText(appContext, RECONNECT_CONFIRM_TOAST, ToastLength.Short).Show();
                Log.Debug(TAG, System.String.Format("Sensor with address {0} is being reconnected", address));
                
                AddDevice(address);

                if(!_scanSwitch.Checked)
                {
                    _scanSwitch.Checked = true;
                }
            });

            Dialog dialog = deleteAlert.Create();
            dialog.Show();
        }


		/// <summary>
		/// Handles the prompt for active devices
		/// </summary>
		/// <param name="address">Address.</param>
		public void confirmDisconnect(string address)
		{
			Android.App.AlertDialog.Builder deleteAlert = new Android.App.AlertDialog.Builder(appContext);
			deleteAlert.SetTitle(DISCONNECT_CONFIRM_TITLE);
			deleteAlert.SetMessage(DISCONNECT_CONFIRM_MESSAGE);

			deleteAlert.SetNegativeButton(Resource.String.yes, (senderAlert, args) =>
			{
                RemoveDevice(address);
			});

			deleteAlert.SetPositiveButton(Resource.String.no, (senderAlert, args) =>
			{
			});

			Dialog dialog = deleteAlert.Create();
			dialog.Show();
		}

		/// <summary>
		/// Starts the BluetoothListActivity, allows user to select a new device to connect
		/// </summary>
		public void scanForDevices()
		{
            List<string> addressList = _connectedDeviceMap.Keys.ToList();

			// Starting list activity
			var serverIntent = new Intent(appContext, typeof(BluetoothListActivity));
			serverIntent.PutStringArrayListExtra(AppUtil.CONNECTED_DEVICE_KEY, addressList);
			StartActivityForResult(serverIntent, 1);
		}

		/// <summary>
		/// Populates the list view with the given bluetooth addresses
		/// If the list is empty it gives the list a default value to indicate that
		/// </summary>
		private void populateConnectedDeviceList()
		{
            // Switch resets itself on Resume, making sure its correct
            disableSwitch();
            if (AppConfig.PostSensorUpdates)
            {
                _scanSwitch.Checked = true;
            }
            else
            {
                _scanSwitch.Checked = false;
            }
            enableSwitch();
            
            // Getting devices from Main
            _connectedDeviceMap = ((MainActivity)Activity).getDeviceMap();
             
           resetListViews();
		}

        #endregion

        #region UI helpers

        /// <summary>
        /// Enables the scan switch
        /// </summary>
        private void enableSwitch()
        {
            _scanSwitch.Enabled = true;
        }

        /// <summary>
        /// Disabled the scan switch
        /// </summary>
        private void disableSwitch()
        {
            _scanSwitch.Enabled = false;
        }

        /// <summary>
        /// Enables any the components of the layout that would require bluetooth to be enabled to work
        /// </summary>
        private void enableBLView()
        {
            _scanSwitch.Enabled = true;
            enableConnectedListViewClick();
            _addDeviceButton.Enabled = true;
        }

        /// <summary>
        /// Disables any the components of the layout that would require bluetooth to be enabled to work
        /// </summary>
        private void disableBLView()
        {
            _scanSwitch.Checked = false;
            disableConnectedListViewClick();
            _addDeviceButton.Enabled = false;
            
            Log.Warn(TAG, "No Bluetooth was detected");

             Snackbar.Make(
                    myView.FindViewById(Resource.Id.content_main),
                    "Bluetooth is not enabled",
                    Snackbar.LengthLong
                ).Show();   
        }

        /// <summary>
        /// Clears the connected devices list view and the Spinner
        /// </summary>
        private void clearConnectionList()
        {
            // active devices      
            _connectDeviceListView.Adapter = null;
            sensorChoice.Adapter = null;

            _spinnerDevicesArrayAdapter.Clear();
            _spinnerDevicesArrayAdapter.NotifyDataSetChanged();
            sensorChoice.Adapter = _spinnerDevicesArrayAdapter;
            
            _connectedDevicesArrayAdapter.Clear();
            _connectedDevicesArrayAdapter.NotifyDataSetChanged();
            _connectDeviceListView.Adapter = _connectedDevicesArrayAdapter;
                        
            // inactive devices
            _disconnectedDeviceListView.Adapter = null;
            _disconnectedDevicesArrayAdapter.Clear();
            _disconnectedDevicesArrayAdapter.NotifyDataSetChanged();
            _disconnectedDeviceListView.Adapter = _disconnectedDevicesArrayAdapter;
            
            disableConnectedListViewClick();
            sensorChoice.Enabled = false;
        }

        /// <summary>
        /// Enabled the connected device list view item click.
        /// </summary>
        private void enableConnectedListViewClick()
        {
            _connectDeviceListView.Enabled = true;
        }

        /// <summary>
        /// Disables the new device list view item click.
        /// </summary>
        private void disableConnectedListViewClick()
        {
            _connectDeviceListView.Enabled = false;
        }

        private void resetLiveView()
        {
            hrRate.SetText(DEF_VALUE, TextView.BufferType.Normal);
            lastUpdate = DateTime.MinValue;
            
            hrLastUpdate.SetText(DEF_VALUE, TextView.BufferType.Normal);
            hrLastUpdate.Invalidate();
        }
        
        private void hideLiveView()
        {
            ((MainActivity)Activity).RunOnUiThread(delegate
            {
                liveLayout.Visibility = ViewStates.Gone;
            });
            
            selectedDevice = null;
        }
        
        private void showLiveView()
        {
            ((MainActivity)Activity).RunOnUiThread(delegate
            {                   
                liveLayout.Visibility = ViewStates.Visible;  
                
                hrRate.SetText(DEF_VALUE, TextView.BufferType.Normal);
                hrRate.Invalidate();
                
                hrLastUpdate.SetText(DEF_VALUE, TextView.BufferType.Normal);
                hrLastUpdate.Invalidate();
            });            
        }
        
        private void resetListViews()
        {
            ((MainActivity)Activity).RunOnUiThread(delegate
            {
                clearConnectionList();

                List<string> activeAddress = _connectedDeviceMap.Where(kvp => kvp.Value == true).Select(kvp => kvp.Key).ToList();
                List<string> inactiveAddress = _connectedDeviceMap.Where(kvp => kvp.Value == false).Select(kvp => kvp.Key).ToList();

                if (_connectedDeviceMap.Count == 0)
                {
                    Log.Warn(TAG, "There are no connected devices");
                    _connectedDevicesArrayAdapter.Add(LE_SENSOR_WARNING_NO_CONNECT);

                    radioGroupList.Visibility = ViewStates.Gone;

                    // Hiding the sensor feedback        
                    hideLiveView();

                    // Turn off sensor feedback if it was on
                    if (_scanSwitch.Checked)
                    {
                        _scanSwitch.Checked = false;
                    }
                }
                else if (activeAddress.Count == 0)
                {
                    Log.Warn(TAG, "There are no active devices");
                    _connectedDevicesArrayAdapter.Add("There are no active devices");

                    // Hiding the sensor feedback        
                     hideLiveView();

                    // Turn off sensor feedback if it was on
                    if (_scanSwitch.Checked)
                    {
                        _scanSwitch.Checked = false;
                    }
                }
                
                if (activeAddress.Count > 0)
                {
                    radioGroupList.Visibility = ViewStates.Visible;
            
                    if(AppConfig.PostSensorUpdates)
                    {
                        showLiveView();
                    }
                
                    Log.Debug(TAG, "Active devices found, adding to list");

                    // Enabling click
                    enableConnectedListViewClick();

                    // Add the items
                    foreach (string a in activeAddress)
                    {
                        BluetoothDevice device = AppUtil.getDevice(a, _adpt);

                        if (device != null && (!string.IsNullOrEmpty(device.Name)))
                        {
	                        _connectedDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
	                        _spinnerDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
                        }
                        else
                        {
                            Log.Warn(TAG, System.String.Format("Sensor with address {0} was not available.  Getting name from cache", a));
                            _connectedDevicesArrayAdapter.Add(AppConfig.getDeviceName(a) + "\n" + a);
                            _spinnerDevicesArrayAdapter.Add(AppConfig.getDeviceName(a) + "\n" + a);
                        }
                    }

                    sensorChoice.SetSelection(0);
                    sensorChoice.Enabled = true;
                }
                
                if (inactiveAddress.Count > 0)
                {
                    Log.Debug(TAG, "Inactive devices found, adding to list");
                    radioGroupList.Visibility = ViewStates.Visible;

                    foreach (string a in inactiveAddress)
                    {
                        _disconnectedDevicesArrayAdapter.Add(AppConfig.getDeviceName(a) + "\n" + a);
                    }
                    
                } else
                {
                    _disconnectedDevicesArrayAdapter.Add("There are no inactive devices");               
                }

                _connectedDevicesArrayAdapter.NotifyDataSetChanged();
                _spinnerDevicesArrayAdapter.NotifyDataSetChanged();
                _disconnectedDevicesArrayAdapter.NotifyDataSetChanged();
            });
        }

		#endregion
	}
}
