
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
using Android.Locations;
using Android.OS;
using Android.Util;
using Newtonsoft.Json;

namespace WatchTower.Droid
{
    [Service(Label = "LESensorRead")]
    [IntentFilter(new String[] { "com.yourname.LESensorRead" })]
    public class LESensorService : Service
    {
        IBinder binder;

        private BluetoothAdapter _adpt;

        private SensorReadingBroadcastReceiver _sensorReadingReceiver;
        private SensorStateBroadcastReceiver _sensorStateReceiver;

        private static Dictionary<string, BackgroundWorker> sensorThreads;

        #region Constants       
        private const string LOG_TAG = "LESensorService";
        #endregion

        #region Initialize

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public LESensorService()
        {
            // Setting up adapter
            _adpt = BluetoothAdapter.DefaultAdapter;
            
            // Broadcast rec
            _sensorReadingReceiver = new SensorReadingBroadcastReceiver();
            _sensorStateReceiver = new SensorStateBroadcastReceiver();

            _sensorStateReceiver.SensorAdded += AddNewSensor;
            _sensorStateReceiver.SensorRemoved += RemoveSensor;
            _sensorStateReceiver.SensorReportingPaused += onReportingPaused;
            _sensorStateReceiver.SensorDisconnect += OnSensorDisconnect;

            // List of background workers
            sensorThreads = new Dictionary<string, BackgroundWorker>();
        }
        
        #endregion

        #region Override methods

        #region Default 
        
        public override void OnCreate()
        {
            base.OnCreate();
        }
        
        #endregion

        public override IBinder OnBind(Intent intent)
        {
            binder = new LESensorServiceBinder(this);

            // Setting up broadcast receiever
            IntentFilter fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_READING_UPDATE_ACTION); 
            fil.Priority = 99;   
            Android.App.Application.Context.RegisterReceiver(_sensorReadingReceiver, fil);

            fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_CONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_DISCONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_ADDED_ACTION);
            fil.AddAction(AppUtil.SENSOR_REMOVED_ACTION);
            fil.AddAction(AppUtil.SENSOR_PAUSE_ACTION);
            fil.Priority = 99;
            Android.App.Application.Context.RegisterReceiver(_sensorStateReceiver, fil);
            
            return binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            Android.App.Application.Context.UnregisterReceiver(_sensorReadingReceiver);
            Android.App.Application.Context.UnregisterReceiver(_sensorStateReceiver);

            foreach(BackgroundWorker worker in sensorThreads.Values)
            {
                worker.CancelAsync();
            }
            
            return base.OnUnbind(intent);
        }

        #endregion

        #region sensor methods
        
        private void OnSensorDisconnect(object sender, SensorStateEventArgs e)
        {
            removeSensorFromService(e.Address);
        }
        
        private void removeSensorFromService(string address)
        {
            if(sensorThreads.ContainsKey(address))
            {
                BackgroundWorker toCancel = sensorThreads[address];
                toCancel.CancelAsync();
                
                sensorThreads.Remove(address);
            }
            else
            {
                Log.Warn(LOG_TAG, "Sensor was not connected to the service");
            } 
        }

        /// <summary>
        /// Notifies broadcast receiver that error occured and a device (or all devices) have become inactive
        /// </summary>
        /// <param name="deviceAddress">(Optional) Device address.  If null, assumed to be relevant to every device</param>
        /// <param name="message">(Optional) Message for this update</param>
        private void sendDisconnectStateUpdate(string deviceAddress = null, string message = null)
        {
            // Creating the intent
            Intent intent = new Intent();
            Bundle intentBundle = new Bundle();
            intentBundle.PutString(AppUtil.ADDRESS_KEY, deviceAddress);
            
            intent.PutExtras(intentBundle);
            intent.SetAction(AppUtil.SENSOR_DISCONNECT_ACTION);
    
            // Send the notification
            SendBroadcast(intent);
            
            
        }      

        public void HexoSkinConnect(Object sender, DoWorkEventArgs e, BluetoothDevice dev)
        {
            hexoSkinConnect(dev);
        }
        
        public void hexoSkinConnect(BluetoothDevice dev)
        {
            DateTime lastChange = DateTime.MinValue;
            
            System.Timers.Timer t = new System.Timers.Timer(AppUtil.HEXOSKIN_INTERVAL);
            System.Timers.Timer timeOut = new System.Timers.Timer(AppUtil.HEXOSKIN_CONNECT_TIMEOUT);
            
            timeOut.AutoReset = true;
            timeOut.Elapsed += delegate {
            
                // Checking if HexoSkin Timed out
                TimeSpan duration = DateTime.Now - lastChange;

                if (duration.TotalMilliseconds > AppUtil.HEXOSKIN_CONNECT_TIMEOUT)
                {
                    timeOut.Enabled = false;
                    t.Enabled = false;

                    // Send disconnect notice
                    sendStateUpdate(dev.Address, false, "Hexoskin stopped reporting data");
                }

                timeOut.Interval = AppUtil.SENSOR_CONNECT_TIMEOUT;
            };  
            
           
            t.AutoReset = true;
            t.Elapsed += async delegate
            {
                timeOut.Enabled = true;
                
                Log.Debug(LOG_TAG, "Getting hexoskin reading");


              try
              {
                string data = await HTTPSender.getMessage("https://s3.amazonaws.com/pscloud-watchtower/", dev.Name);

                // If we got valid data
                if (!string.IsNullOrWhiteSpace(data))
                {
                  if (lastChange == DateTime.MinValue)
                  {
                    // First time we read data.
                    sendStateUpdate(dev.Address, true, "Hexoskin started reporting data");
                  }

                  lastChange = DateTime.Now;

                  // Sending data to SensorHandler
                  Dictionary<string, string> deviceDetails = new Dictionary<string, string>();
                  deviceDetails.Add("device_name", dev.Name);

                  // Get the sensor handler
                  SensorHandler sh = new SensorHandler(deviceDetails, AppConfig.UserID);

                  byte[] dataArr = Encoding.UTF8.GetBytes(data);
                  sh.updateData(dataArr);
                  string xmlDetail = sh.xmlDetail;

                  Log.Debug(LOG_TAG, xmlDetail);

                  // Notifying
                  Intent message = new Intent(AppUtil.SENSOR_READING_UPDATE_ACTION);
                  Bundle intentBundle = new Bundle();
                  intentBundle.PutString(AppUtil.ADDRESS_KEY, dev.Address);
                  intentBundle.PutString(AppUtil.DETAIL_KEY, xmlDetail);

                  // Get Time last reading was taken
                  Dictionary<string, string> dataJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                  double epochValue;

                  if (double.TryParse(dataJson["timestamp"], out epochValue))
                  {
                    epochValue = epochValue / 256;

                    DateTime hexoSkinRead = AppUtil.FromUnixTime((long)epochValue);
                    hexoSkinRead = DateTime.SpecifyKind(hexoSkinRead, DateTimeKind.Utc);
                    hexoSkinRead = hexoSkinRead.ToLocalTime();

                    intentBundle.PutString(AppUtil.TIME_KEY, hexoSkinRead.ToString()
                    );
                  }

                  message.PutExtras(intentBundle);
                  SendBroadcast(message);
                }
              } catch(Exception ex)
              {
                Log.Debug(LOG_TAG, "Getting Hexoskin data failed");
              }
            };

            t.Enabled = true;
        }

        #region Helper Methods
        
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

        #endregion

        #endregion

        #region Callback 


        /// <summary>
        /// Removes all the sensors from the service
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void onReportingPaused(object sender, SensorStateEventArgs e)
        {
            foreach(string key in sensorThreads.Keys.ToList())
            {
                removeSensorFromService(key);
            }  
            
            Log.Debug(LOG_TAG, "Sensor Reporting was turned off");      
        }
        
        /// <summary>
        /// Removes the sensor from the service
        /// </summary>
        /// <param name="">.</param>
        private void RemoveSensor(object sender, SensorStateEventArgs e)
        {
            
            if(sensorThreads.ContainsKey(e.Address))
            {
                removeSensorFromService(e.Address);
            }
            else
            {
                Log.Warn(LOG_TAG, "Sensor was not connected to the service");
            }        
        }
              
        /// <summary>
        /// Adds the sensor from the service
        /// </summary>
        /// <param name="">.</param>
        private void AddNewSensor(object sender, SensorStateEventArgs e)
        {

            if (AppConfig.PostSensorUpdates)
            {
                BluetoothDevice dev = AppUtil.getDevice(e.Address, _adpt);

                // If the devie can found and is not already connected to
                if (dev != null)
                {
                    if (!sensorThreads.ContainsKey(e.Address))
                    {
                        // Starting the connection
                        connectToSensor(dev);
                    }
                    else
                    {
                        Log.Warn(LOG_TAG, String.Format("The sensor with address {0} was already added to the service.", e.Address));
                    }
                }
                else
                {
                    // Device could not be connected to
                    Log.Warn(LOG_TAG, String.Format("The sensor with address {0} could not be found.  Setting the sensor inactive.", e.Address));
                    sendDisconnectStateUpdate(e.Address, String.Format("The sensor with address {0} could not be found. Setting the sensor inactive.", e.Address));
                }
            }
        }
        
        private void SendBroadcast(Intent intent)
        {
            Android.App.Application.Context.SendBroadcast(intent, null);
        }
        
        /// <summary>
        /// Connects the new device to the service
        /// </summary>
        /// <param name="dev">Dev.</param>
        private void connectToSensor(BluetoothDevice dev)
        {
            // Getting worker
            BackgroundWorker worker = getSensorWorker(dev);

            // Add the device to our map
            sensorThreads.Add(dev.Address, worker);

            // Start work
            worker.RunWorkerAsync();
        }
        
        
        /// <summary>
        /// Gets the worker needed for this device type
        /// </summary>
        /// <returns>The sensor worker.</returns>
        /// <param name="dev">Dev.</param>
        private BackgroundWorker getSensorWorker(BluetoothDevice dev)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerCompleted += delegate 
                {
                    // if the worker stops it means the device has become disconnected
                  //  sendDisconnectStateUpdate(dev.Address);
                };
            
            
            // Setting up worker depending on sensor
            string manfName = new string(dev.Name.TakeWhile(Char.IsLetter).ToArray());

            if(manfName == "HX")
            {
                worker.DoWork += (sender, e) => HexoSkinConnect(sender, e, dev);
                return worker;
            }
            
            // Get the type of device
            BluetoothDeviceType t = AppUtil.getDeviceType(dev);

            switch (t)
            {
                case BluetoothDeviceType.Le:
                    goto case BluetoothDeviceType.Dual;
                case BluetoothDeviceType.Dual:
                    worker.DoWork += (sender, e) => LeSensorConnect(sender, e, dev);
                    break;
                default:
                    // Unsupported Bluetooth Device Types
                    Log.Debug(LOG_TAG, "Unsupported Bluetooth Device!");
                    throw new NotSupportedException(t.ToString() + " Bluetooth Devices are not supported");
            }
       
            return worker;
        }
        
        
        private void LeSensorConnect(Object sender, DoWorkEventArgs e, BluetoothDevice dev)
        {
            SensorStateBroadcastReceiver rec = new SensorStateBroadcastReceiver();
            System.Timers.Timer sensorTimeOut = new System.Timers.Timer();
            
            BGattCallback bcallback = new BGattCallback();
            BluetoothGatt g = dev.ConnectGatt(Application.Context, true, bcallback);
   
            sensorTimeOut.AutoReset = false;
            sensorTimeOut.Interval = AppUtil.SENSOR_CONNECT_TIMEOUT;
            sensorTimeOut.Elapsed += delegate {

                sensorTimeOut.Enabled = false;
                sendDisconnectStateUpdate(dev.Address);
                Android.App.Application.Context.UnregisterReceiver(rec);
            };
             
            rec.SensorConnect += delegate {
                sensorTimeOut.Enabled = false;
                Android.App.Application.Context.UnregisterReceiver(rec);
            };
            
            rec.SensorDisconnect += delegate {
                sensorTimeOut.Enabled = false;
                Android.App.Application.Context.UnregisterReceiver(rec);
            };

            sensorTimeOut.Enabled = true;
            
            IntentFilter fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_CONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_DISCONNECT_ACTION);
            fil.Priority = 98;
            Android.App.Application.Context.RegisterReceiver(rec, fil);
            
            System.Timers.Timer cancelCheck = new System.Timers.Timer();
            cancelCheck.AutoReset = true;
            cancelCheck.Interval = 5 * 1000;
            cancelCheck.Elapsed += delegate {
                
                BackgroundWorker worker = (BackgroundWorker)sender;

	            if(worker.CancellationPending)
	            {
	                g.Close();
                    cancelCheck.Enabled = false;
	            }
                
            };

            cancelCheck.Enabled = true;
        }
        
   
        #endregion
    
    } // end class    

    #region Service Binder

    public class LESensorServiceBinder : Binder
    {
        readonly LESensorService service;

        public LESensorServiceBinder(LESensorService service)
        {
            this.service = service;
        }

        public LESensorService GetLESensorService()
        {
            return service;
        }
    }

    #endregion
} // end namespace
