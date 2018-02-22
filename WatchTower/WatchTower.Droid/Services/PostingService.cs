using System;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using System.Collections.Generic;
using Android.Locations;
using System.Threading;
using Android.Gms.Common.Apis;
using Android.Gms.Common;
using Android.Gms.Location;
using System.ComponentModel;
using System.Threading.Tasks;
using EMS.NIEM.Sensor;
using EMS.NIEM.Resource;
using EMS.NIEM.NIEMCommon;
using System.Linq;

namespace WatchTower.Droid.Services
{

    [Service]
    public class PostingService : Service, GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener, Android.Gms.Location.ILocationListener
    {
        private static readonly string TAG = typeof(PostingService).Name;
        private Dictionary<string, SensorDetail> sensorDetailMap;
        private SensorReadingBroadcastReceiver receiver;
        private static System.Timers.Timer sendUpdateTimer;
        private LocationBroadcastReceiver locationReceiver;

        public IBinder Binder { get; private set; }
        public GoogleApiClient apiClient;
        LocationRequest locRequest;

        #region intial

        public override void OnCreate()
        {
            // This method is optional to implement
            base.OnCreate();
            Log.Debug(TAG, "OnCreate");

            Initalize();
        }

        public void Initalize()
        {
            Log.Info("MainActivity", "Google Play Services is installed on this device.");
            apiClient = new GoogleApiClient.Builder(this, this, this)
                .AddApi(LocationServices.API).Build();

            // generate a location request that we will pass into a call for location updates

            locRequest = new LocationRequest();
            locRequest.SetPriority(100);
            // Setting interval between updates, in milliseconds
            // NOTE: the default FastestInterval is 1 minute. If you want to receive location updates more than 
            // once a minute, you _must_ also change the FastestInterval to be less than or equal to your Interval
            locRequest.SetFastestInterval(500);
            locRequest.SetInterval(1000);

            // setting up the time
            sendUpdateTimer = new System.Timers.Timer(AppConfig.PostInterval * 1000);
            sendUpdateTimer.AutoReset = false;
            sendUpdateTimer.Elapsed += (sender, e) => onPostInterval(sender, e);

            // Map of sensor details
            sensorDetailMap = new Dictionary<string, SensorDetail>();

            // Broadcast re
            receiver = new SensorReadingBroadcastReceiver();
            receiver.OnSensorReading += (sender, e) => onNewSensor(sender, e);

            // location rec
            locationReceiver = new LocationBroadcastReceiver();
        }

        #endregion

        #region callback


        /// <summary>
        /// Callback for new sensor detail
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Sensor event args</param>
        public void onNewSensor(object sender, SensorEventArgs e)
        {
            addSensorDetail(e.Address, e.Detail);
        }

        /// <summary>
        /// Callback for the post timer
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="e">E.</param>
        public void onPostInterval(object source, ElapsedEventArgs e)
        {
            // If updates have been requested
            if (AppConfig.PostUpdates)
            {
                sendUpdateTimer.Enabled = false;
                Task t = new Task(() => sendUpdate());

                t.RunSynchronously();
                
                // check that interval has not changed and then reenable the timer
	            sendUpdateTimer.Interval = AppConfig.PostInterval * 1000;
	            sendUpdateTimer.Enabled = true;      
	                
	            clearSensorDetail();
            }
            else
            {
                
                sendUpdateTimer.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the update
        /// </summary>
        private void sendUpdate()
        {
            try
            {
                PostUpdates();

                // Notifiyingg
                Intent intent = new Intent();
                intent.SetAction(AppUtil.LOCATION_LAST_SENT_ACTION);

                Bundle intentBundle = new Bundle();
                intentBundle.PutString(AppUtil.LAST_SENT_KEY, DateTime.Now.ToString());
                intent.PutExtras(intentBundle);

                Android.App.Application.Context.SendBroadcast(intent);
            }
            catch (Exception e)
            {

                // log
                string t = "";

            }
        }


        #endregion
        #region Sensor methods       

        private void PostUpdates()
        {
            // Getting values from preferences
            string deviceInfo = Android.OS.Build.Model;

            // Log 
            Log.Debug(TAG, "Foreground updating");
            Log.Debug(TAG, "Device info: " + deviceInfo);
            Log.Debug(TAG, "Post url: " + AppConfig.PostUrl);

            // Getting lat/lon 
            Location location = LocationServices.FusedLocationApi.GetLastLocation(apiClient);
            double lat = location.Latitude;
            double lon = location.Longitude;

            // Getting the resource detail
            ResourceDetail det = HTTPSender.createResourceDetail(AppConfig.UserID, AppConfig.Agency);

            List<EventDetails> detailsList = new List<EventDetails>();
            detailsList.Add(det);

            if (sensorDetailMap.Count > 0)
            {
                Log.Debug(TAG, "Adding Sensor Details");
                detailsList.AddRange(sensorDetailMap.Values.ToList());
            }

            HTTPSender.sendUpdate(lat, lon, AppConfig.UserID, AppConfig.Agency, AppConfig.PostUrl, AppConfig.SelectedResource, detailsList);
        }

        #endregion


        #region Helper Methods
        
        private void SendBroadcast(Intent intent)
        {
            Android.App.Application.Context.SendBroadcast(intent, null);
        }
        
        /// <summary>
        /// Adds the sensor detail to the detail map
        /// </summary>
        /// <param name="add">Address of bluetooth device with this detail</param>
        /// <param name="det">The sensor detail object</param>
        private void addSensorDetail(string add, SensorDetail det)
        {

            if (sensorDetailMap.ContainsKey(add))
            {
                sensorDetailMap[add] = det;

            }
            else
            {
                sensorDetailMap.Add(add, det);
            }
        }

        /// <summary>
        /// Clears the sensor detail map
        /// </summary>
        private void clearSensorDetail()
        {
            sensorDetailMap.Clear();
        }

        #endregion

        #region Override default

        public override IBinder OnBind(Intent intent)
        {
            apiClient.Connect();

            // Setting up broadcast receiever
            IntentFilter fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_READING_UPDATE_ACTION);
            Android.App.Application.Context.RegisterReceiver(receiver, fil);

            fil = new IntentFilter();
            fil.AddAction(AppUtil.LOCATION_LAST_SENT_ACTION);
            fil.AddAction(AppUtil.LOCATION__CONNECT_ACTION);
            fil.AddAction(AppUtil.LOCATION__DISCONNECT_ACTION);
            fil.Priority = 100;
            Android.App.Application.Context.RegisterReceiver(locationReceiver, fil);

            sendUpdateTimer.Enabled = true;

            // This method must always be implemented
            Log.Debug(TAG, "OnBind");
            return this.Binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            Android.App.Application.Context.UnregisterReceiver(receiver);
            Android.App.Application.Context.UnregisterReceiver(locationReceiver);

            sendUpdateTimer.Enabled = false;

            // This method is optional to implement
            Log.Debug(TAG, "OnUnbind");
            return base.OnUnbind(intent);
        }

        public override void OnDestroy()
        {
            // This method is optional to implement
            Log.Debug(TAG, "OnDestroy");
            Binder = null;
            base.OnDestroy();
        }

        #endregion

        #region Implemented methods
        
        public void OnConnectionFailed(ConnectionResult result)
        {
            Log.Info("PostingService", "LocationService: Connection failed, attempting to reach google play services");
            
            // Notify fragment
            Intent intent = new Intent();
            intent.SetAction(AppUtil.LOCATION__DISCONNECT_ACTION);
            SendBroadcast(intent);
        }

        public void OnLocationChanged(Location location)
        {
            // do nothing
        }

        public void OnConnectedAsync(Bundle connectionHint)
        {
            Log.Info("PostingService", "Now connected to client");
        }

        public void OnConnectionSuspended(int cause)
        {
            Log.Info("PostingService", "Now disconnected from client");
        }

        public void OnConnected(Bundle connectionHint)
        {
            // Notify
            Intent intent = new Intent();
	        intent.SetAction(AppUtil.LOCATION__CONNECT_ACTION);	        
            SendBroadcast(intent);
        }
        
        #endregion

    }
}
