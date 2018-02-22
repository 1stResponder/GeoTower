using System;
using Android.Content;
using Android.App;
using Android.Util;
using Android.OS;
using System.Collections.Generic;
using System.Linq;

namespace WatchTower.Droid
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { "wt.sensor.disconnect","wt.sensor.connect", "wt.sensor.add", "wt.sensor.remove", "wt.sensor.pause"})]
    public class SensorStateBroadcastReceiver : BroadcastReceiver
    {
        private static readonly string TAG = typeof(SensorStateBroadcastReceiver).Name;
        public event EventHandler<SensorStateEventArgs> SensorAdded;
        public event EventHandler<SensorStateEventArgs> SensorRemoved;
        public event EventHandler<SensorStateEventArgs> SensorConnect;
        public event EventHandler<SensorStateEventArgs> SensorDisconnect;
        public event EventHandler<SensorStateEventArgs> SensorReportingPaused;

        public SensorStateBroadcastReceiver() : base()
        {
        }

        public override void OnReceive(Context context, Intent intent)
        {
            // Getting Bundle
            Bundle intentBundle = intent.Extras;
            
            // Getting action for this intent
            string action = intent.Action;

            // Will hold desc for action
            string desc = "";
            

            if (intentBundle != null)
            {
                string address = "";
                string name = "";
                
                address = intentBundle.GetString(AppUtil.ADDRESS_KEY);
                name = intentBundle.GetString(AppUtil.NAME_KEY);
                SensorStateEventArgs args = new SensorStateEventArgs(address, name);  
                
                try
                {                 
	                if (action == AppUtil.SENSOR_DISCONNECT_ACTION)
	                {
                        Log.Debug(TAG, String.Format("Sensor with address: {0} disonnected", address));
	                    SensorDisconnect(this, args); 
	                }
	                else if (action == AppUtil.SENSOR_CONNECT_ACTION)
	                {
                        Log.Debug(TAG, String.Format("Sensor with address: {0} connected successfully ", address));
	                    SensorConnect(this, args);  
	                } 
	                else if (action == AppUtil.SENSOR_ADDED_ACTION)
	                {
                        Log.Debug(TAG, String.Format("Sensor with address: {0} was added", address));
	                    SensorAdded(this, args);
	                } 
	                else if (action == AppUtil.SENSOR_REMOVED_ACTION)
	                {
                        Log.Debug(TAG, String.Format("Sensor with address: {0} was removed", address));
	                    SensorRemoved(this, args);
	                } 
                    
                } 
                catch (NullReferenceException e)
                {
                    // Nothing was listening to this Event
                    Log.Debug(TAG,"Nothing is currently listening to this Event");
                }

            } else if (action == AppUtil.SENSOR_PAUSE_ACTION)
            {
                SensorStateEventArgs args = new SensorStateEventArgs("", "");
                Log.Debug(TAG, String.Format("Sensor reporting was paused"));

                try
                {
                    SensorReportingPaused(this, args);
                } catch (NullReferenceException e)
                {
                    
                }
            } 
        }    
    }

    #region Custom Event Args


    public class SensorStatusEventArgs : EventArgs
    {
        private List<string> address;

        public SensorStatusEventArgs(List<string> add)
        {
            address = add;
        }

        /// <summary>
        /// List of currently connected address
        /// </summary>
        /// <value>The address list.</value>
        public List<string> ConnectedAddressList
        {
            get
            {
                return address;
            }
        }        
    }

    public enum SensorStateBroadcastAction
    { 
        CONNECTED_SINGLE,
        DISCONNECTED_SINGLE,
        DISCONNECTED_ALL,
        ADDED,
        REMOVED
    };

    // SensorStateEventArgs
    public class SensorStateEventArgs : EventArgs
    {
        private string address, name;
    
        public SensorStateEventArgs(string add, string deviceName)
        {
            address = add;
            name = deviceName;
        }

        /// <summary>
        /// Address of the device
        /// </summary>
        /// <value>The address.</value>
        public string Address
        {
            get
            {
                return address;
            }
        }  
        
        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName
        {
            get
            {
                return name;
            }
        }     
    }
    

    #endregion
}
