
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
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace WatchTower.Droid
{
    [Activity(Label = "BluetoothListActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class BluetoothListActivity : AppCompatActivity
    {
        private static List<BluetoothDevice> _devicesFound; // List of supported devices already found
        private static List<string> _connectedDevicesList; // List of the device names already connected
        private static List<string> _pairedDeviceList; // List of supported devices paired
        private static List<string> _newDeviceList; // List of supported devices which are not paired

        private static Context appContext; // Context for this activity
        private static BluetoothAdapter _adpt;
        private static LeScanCallback _scanCallBack;
        private BackgroundWorker _worker;

        // Array Adapters that handles ListViews
        private static ArrayAdapter<string> pairedDevicesArrayAdapter;
        private static ArrayAdapter<string> newDevicesArrayAdapter;

        // Timer used for scan timeouts
        private static System.Timers.Timer _scanTimeout;

        // List views
        private static ListView _pairedDeviceListView;
        private static ListView _newDeviceListView;

        private static View _loadingDial;

        private static Button scanButton;
       // private static Button cancelButton;

        private static Activity myAct; // reference to this activity

        // Constants for UI
        private static string SENSOR_WARNING_NO_PAIRED_DEVICE = AppUtil.GetResourceString(Resource.String.le_sensor_warning_no_paired);
        private static string SENSOR_WARNING_NO_AVAL_DEVICE = AppUtil.GetResourceString(Resource.String.le_sensor_warning_no_aval);
        private static string NO_BLUETOOTH_WARNING = AppUtil.GetResourceString(Resource.String.no_bluetooth_warning);


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ListBluetooth);

            myAct = this;

            // getting the app's context
            appContext = Android.App.Application.Context;

            // Initalizing the Views
            _pairedDeviceListView = FindViewById<ListView>(Resource.Id.paired_devices);
            _newDeviceListView = FindViewById<ListView>(Resource.Id.new_devices);
            _loadingDial = FindViewById(Resource.Id.progressBar1);

            _pairedDeviceList = new List<string>();
            _newDeviceList = new List<string>();

            // Setting up the Apapters and List Views 
            pairedDevicesArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.device_name, _pairedDeviceList);
            _pairedDeviceListView.Adapter = pairedDevicesArrayAdapter;
            _pairedDeviceListView.ItemClick += deviceClick;

            newDevicesArrayAdapter = new ArrayAdapter<string>(this, Resource.Layout.device_name, _newDeviceList);
            
            
             
            _newDeviceListView.Adapter = newDevicesArrayAdapter;
            _newDeviceListView.ItemClick += deviceClick;
           /* _newDeviceListView.Touch += delegate {
    
                
    
            };*/

            // Initializing button
            scanButton = (Button)FindViewById(Resource.Id.scanBTDevice);
            scanButton.Click += (sender, e) => scanClick(sender, e);

          //  cancelButton = (Button)FindViewById(Resource.Id.cancelScan);
           // cancelButton.Click += (sender, e) => cancelClick(sender, e);

            // Initializing rest of the objects
            Initialize();

            // Getitng list of connected devices (if any were provided)
            string resourceName = AppUtil.CONNECTED_DEVICE_KEY;

            if (Intent.GetStringArrayListExtra(resourceName) != null)
            {
                _connectedDevicesList = Intent.GetStringArrayListExtra(resourceName).ToList();
            }

            // Setting up toolbar
            Android.Support.V7.Widget.Toolbar toolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);   
            SetSupportActionBar(toolbar);
  
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            
            
        }

        /// <summary>
        /// Initialize the Activity.
        /// </summary>
        private void Initialize()
        {
            // Initalizing Bluetooth Adapter
            _adpt = BluetoothAdapter.DefaultAdapter;

            // Initializng lists
            _devicesFound = new List<BluetoothDevice>();
            _connectedDevicesList = new List<string>();

            // Setting timer for scan timeout
            _scanTimeout = new System.Timers.Timer(POC_Constants.SCAN_TIMEOUT);
            _scanTimeout.AutoReset = false;
            _scanTimeout.Elapsed += onTimeOut;

            // Setting up background worker
            _worker = new BackgroundWorker();
            _worker.DoWork += startLEScan;
            _worker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Checks that the bluetooth device is connected when resuming
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();

            onResumeUI();
            
            if (AppUtil.checkIfBluetoothEnabled(_adpt))
            {
                addBoundedDevices();
                _worker.RunWorkerAsync();
            }
            else
            {
                // Bluetooth not enabled
                Toast.MakeText(appContext, NO_BLUETOOTH_WARNING, ToastLength.Short).Show();
            }
        }
        
        protected override void OnPause()
        {
            base.OnPause();
             stopScan();
        }

        public override void OnBackPressed()
        {
            stopScan();
            
            // Set result and finish this Activity
            Intent intent = new Intent();
            SetResult(Result.Ok, intent);
            Finish();
        }
        
        public override bool OnOptionsItemSelected(IMenuItem item)
		{
		    //Back button pressed -> toggle event
		    if (item.ItemId == Android.Resource.Id.Home)
		        this.OnBackPressed(); 
		
		    return base.OnOptionsItemSelected(item);
		}

        #region paired device methods

        /// <summary>
        /// Add the already paired devices to the paired array adapter
        /// </summary>
        private void addBoundedDevices()
        {
            var deviceList = AppUtil.addPairedDevices(_adpt);

            // Clearing the list
            myAct.RunOnUiThread(delegate
            {
                _pairedDeviceList.Clear();
            });

            // If there are paired devices, add each one to the ArrayAdapter
            if (deviceList.Count > 0)
            {
                //_pairedDeviceListView.ItemClick += deviceClick;
                foreach (BluetoothDevice device in deviceList)
                {
                    bool isPaired = _pairedDeviceList.Any(p => string.Equals(p, device.Address, StringComparison.CurrentCulture));
                    bool isConnected = _connectedDevicesList.Any(p => string.Equals(p, device.Address, StringComparison.CurrentCulture));

                    // If the device was not already found and is not currently connected
                    if ((!isPaired) && (!isConnected))
                    {
                        // If the device is supported
                        if (AppUtil.isSupported(device))
                        {

                            pairedDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
                            
                            pairedDevicesArrayAdapter.NotifyDataSetChanged();

                            _pairedDeviceList.Add(device.Address);
                        }
                    }
                }
            }

            boundDeviceScanUI();
        }
        #endregion

        #region BLE Scan

        /// <summary>
        /// Starts the BLE Scan
        /// </summary>
        public void startLEScan()
        {
            startScanUIPrep();
            _devicesFound = new List<BluetoothDevice>();

            // Starting Timer 
            _scanTimeout.Enabled = true;

            // Starting Scan
            _scanCallBack = new LeScanCallback();
            _adpt.StartLeScan(_scanCallBack);
        }

        /// <summary>
        /// Stops both bluetooth scans
        /// </summary>
        public void stopScan()
        {
            // Stopping Timer
            _scanTimeout.Enabled = false;

            // Ending scan
            _adpt.StopLeScan(_scanCallBack);

            // Making UI changes
            endScanUIPrep();
        }

        #endregion

        #region Event Handlers/Callback

        /// <summary>
        /// Call back for worker.  Starts the BLE Scan
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Arguments</param>
        // Starts scan for LE Bluetooth devices
        private void startLEScan(object sender, DoWorkEventArgs args)
        {
            startLEScan();
        }

        /// <summary>
        /// Starts the scan on the button click
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        public void scanClick(object sender, EventArgs e)
        {
            _worker.RunWorkerAsync();
        }

        /// <summary>
        /// Starts the scan on the button clickf
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
      /*  public void cancelClick(object sender, EventArgs e)
        {
            stopScan();

            // disabling cancel button
            Button b = (Button)sender;
            b.Enabled = false;

            // Set result and finish this Activity
            Intent intent = new Intent();
            SetResult(Result.Ok, intent);
            Finish();
        }*/

        /// <summary>
        /// Callback for Timer.  Ends the scan
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="e">e.</param>
        private void onTimeOut(object source, System.Timers.ElapsedEventArgs e)
        {
            // Stopping the LEScan
            myAct.RunOnUiThread(stopScan);
        }

        /// <summary>
        /// Handles the device name clicks in the list
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void deviceClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // Stop scan, device has been selected
            stopScan();

            // Get the device MAC address, which is the last 17 chars in the View
            var info = (e.View as TextView).Text.ToString();
            string address = info.Substring(info.Length - 17);
            string name = info.Substring(0, info.Length - 17);
            name = name.Replace("\n", string.Empty);
            name = name.Replace("\\n", string.Empty);

            // Create the result Intent and include the MAC address
            // Got to send this back to other activity now
            Intent intent = new Intent();
            Bundle b = new Bundle();
            b.PutString(AppUtil.ADD_DEVICE_KEY, address);
            b.PutString(AppUtil.NAME_KEY, name);
            intent.PutExtras(b);
            
            // Set result and finish this Activity
            SetResult(Result.Ok, intent);
            Finish();
        }

        #endregion
        #region UI helper method

        /// <summary>
        /// UI changes needed onResume
        /// </summary>
        private void onResumeUI()
        {
            myAct.RunOnUiThread(delegate
            {
                // Clearing the List Views and enabling the list views
                clearPairedDeviceListView();
                clearNewDeviceListViews();

                setPairedDeviceListViewClick(true);
                setNewDeviceListViewClick(true);

                // Setting Visibility for the load dial
                setLoadDialVisibility(false);

                // If the bluetooth is enabled, set the UI appropietly
                if (AppUtil.checkIfBluetoothEnabled(_adpt))
                {
                    // Enabling button
                    scanButton.Enabled = true;
                }
            });
        }

        /// <summary>
        /// Makes the UI changes for the start of the scan
        /// </summary>
        private void startScanUIPrep()
        {
            myAct.RunOnUiThread(delegate
            {
	            // Setting the Load Dial to be visibile
	            setLoadDialVisibility(true);
	
	            // Clear the list
	            clearNewDeviceListViews();
	            _devicesFound.Clear();
                
                setNewDeviceListViewClick(false);
                newDevicesArrayAdapter.Add("Searching For Compatible Sensors...");

                // Disabling scan button
                scanButton.Enabled = false;
            });
        }

        /// <summary>
        /// Makes the UI changes for the end of the scan
        /// </summary>
        private void endScanUIPrep()
        {
            myAct.RunOnUiThread(delegate
            {
                // Re-enabling scan button
                scanButton.Enabled = true;

                // Hiding the load dial
                setLoadDialVisibility(false);

                // Checking For results, if there are no new devices let the user know
                if (_devicesFound.Count == 0)
                {
                    clearNewDeviceListViews();
                    
                    newDevicesArrayAdapter.Add(SENSOR_WARNING_NO_AVAL_DEVICE);
                    setNewDeviceListViewClick(false);
                }
            });
        }

        /// <summary>
        /// Handles the UI for the bound device Scan
        /// </summary>
        /// <param name="devList">List of found devices</param>
        private void boundDeviceScanUI()
        {
            myAct.RunOnUiThread(delegate
            {
                if (_pairedDeviceList.Count > 0)
                {
                    setPairedDeviceListViewClick(true);

                }
                else
                {
                    pairedDevicesArrayAdapter.Add(SENSOR_WARNING_NO_PAIRED_DEVICE);
                    setPairedDeviceListViewClick(false);
                }
            });
        }

        /// <summary>
        /// Enable or disable the clickability of the new list view items
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        private static void setNewDeviceListViewClick(bool enabled)
        {
            myAct.RunOnUiThread(delegate
            {
                _newDeviceListView.Enabled = enabled;
            });
        }

        /// <summary>
        /// Enable or disable the clickability of the paired list view items
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        private void setPairedDeviceListViewClick(bool enabled)
        {
            myAct.RunOnUiThread(delegate
            {
                _pairedDeviceListView.Enabled = enabled;
            });
        }

        /// <summary>
        /// Clears new device list views
        /// </summary>
        private static void clearNewDeviceListViews()
        {
            myAct.RunOnUiThread(delegate
            {
                _newDeviceListView.Adapter = null;
                newDevicesArrayAdapter.Clear();
                newDevicesArrayAdapter.NotifyDataSetChanged();
                _newDeviceListView.Adapter = newDevicesArrayAdapter;
            });
        }

        /// <summary>
        /// Clears the paired device list view.
        /// </summary>
        private void clearPairedDeviceListView()
        {
            myAct.RunOnUiThread(delegate
            {
                _pairedDeviceListView.Adapter = null;
                pairedDevicesArrayAdapter.Clear();
                pairedDevicesArrayAdapter.NotifyDataSetChanged();
                _pairedDeviceListView.Adapter = pairedDevicesArrayAdapter;
            });
        }

        /// <summary>
        /// Sets the visibility of the loading dial
        /// </summary>
        /// <param name="visible">If set to <c>true</c> visible.</param>
        private static void setLoadDialVisibility(bool visible)
        {
            myAct.RunOnUiThread(delegate
            {
                if (visible)
                {
                    _loadingDial.Visibility = ViewStates.Visible;
                    scanButton.Text = "Scanning";
                    scanButton.Alpha = .5f;
                }
                else
                {
                    _loadingDial.Visibility = ViewStates.Gone;
                    scanButton.Text = AppUtil.GetResourceString(Resource.String.sensor_scan_button_text);
                    scanButton.Alpha = 1f;
                }
            });
        }
        #endregion


        #region BLE Scan callback

        /// <summary>
        /// Callback for the LowEnergy Bluetooth Scanner
        /// </summary>
        private class LeScanCallback : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
        {
            public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
            {
                if (device.Name != null && device.Address != null)
                {
                    bool isFound = _devicesFound.Any(p => string.Equals(p.Address, device.Address, StringComparison.CurrentCulture));
                    bool isPaired = _pairedDeviceList.Any(p => string.Equals(p, device.Address, StringComparison.CurrentCulture));
                    bool isConnected = _connectedDevicesList.Any(p => string.Equals(p, device.Address, StringComparison.CurrentCulture));

                    // if the device was not already found in this scan, not already connected, or not already in the list of paired devices
                    if ((!isFound) && (!isPaired) && (!isConnected))
                    {
                        // If the device is supported
                        if (AppUtil.isSupported(device))
                        {

                           if(_devicesFound.Count == 0)
                            {
                                clearNewDeviceListViews();
                                setNewDeviceListViewClick(true);
                                string t = "";
                            }

                            newDevicesArrayAdapter.Add(device.Name + "\n" + device.Address);
                            _devicesFound.Add(device);
                        }
                    }
                }


            }
        }

        #endregion

    } // End Class
} // End Namespace


