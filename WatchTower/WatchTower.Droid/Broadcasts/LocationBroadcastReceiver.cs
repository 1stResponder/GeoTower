using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Android.Content;
using EMS.NIEM.Sensor;
using Android.App;
using System.Linq;
using Android.Util;
using Android.OS;

namespace WatchTower.Droid
{

    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { "location.update.data", "location.lastsent.update", "location.disconnect", "location.connect"})]
    public class LocationBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler<LocationEventArgs> locationUpdate;
        public event EventHandler<LocationEventArgs> SendUpdate;
        public event EventHandler<LocationStateEventArgs> ConnectionStateChange;
        private static readonly string TAG = typeof(LocationBroadcastReceiver).Name;

        public LocationBroadcastReceiver() : base()
        {

        }

        public override void OnReceive(Context context, Intent intent)
        {
            Bundle intentBundle = intent.Extras;

            if (intentBundle != null)
            {
                // Values
                double? lat = null;
                double? lon = null;
                double? alt = null;
                double? acc = null;

                double temp;
                DateTime lastSent;

                Log.Debug(TAG, "Update Received");

                if (Double.TryParse(intentBundle.GetString(AppUtil.LAT_KEY), out temp)) lat = temp;
                if (Double.TryParse(intentBundle.GetString(AppUtil.LON_KEY), out temp)) lon = temp;
                if (Double.TryParse(intentBundle.GetString(AppUtil.ALT_KEY), out temp)) alt = temp;
                if (Double.TryParse(intentBundle.GetString(AppUtil.ACC_KEY), out temp)) acc = temp;
                if (!DateTime.TryParse(intentBundle.GetString(AppUtil.LAST_SENT_KEY), out lastSent)) lastSent = DateTime.MinValue;

                // Creating event args
                LocationEventArgs arg = null;

                if (intent.Action == AppUtil.LOCATION_UPDATE_ACTION)
                {
                    arg = new LocationEventArgs(lat, lon, alt, acc);

                    try
                    {
                        locationUpdate(this, arg);
                    }
                    catch (NullReferenceException e)
                    {
                        // This exception occurs when nothign is subscribed to this event, it can be safely ignored                    
                        Log.Debug(TAG, "Nothing is subscribed to the event");
                    }
                }
                else if (intent.Action == AppUtil.LOCATION_LAST_SENT_ACTION)
                {
                    arg = new LocationEventArgs(lastSent);

                    try
                    {
                        SendUpdate(this, arg);
                    }
                    catch (NullReferenceException e)
                    {
                        // This exception occurs when nothign is subscribed to this event, it can be safely ignored                    
                        Log.Debug(TAG, "Nothing is subscribed to the event");
                    }
                }
                else if (intent.Action == AppUtil.LOCATION__CONNECT_ACTION)
                {
                    LocationStateEventArgs stateArg = new LocationStateEventArgs(true);
                    ConnectionStateChange(this, stateArg);
                }
                else if (intent.Action == AppUtil.LOCATION__DISCONNECT_ACTION)
                {
                    LocationStateEventArgs stateArg = new LocationStateEventArgs(false);
                    ConnectionStateChange(this, stateArg);
                }
            }
        }
    } // end class

    #region Custom Event args

    public class LocationStateEventArgs : EventArgs
    {
        private bool connectionState;
    
        /// <summary>
        /// Initializes a new instance of the <see cref="T:WatchTower.Droid.LocationStateEventArgs"/> class.
        /// </summary>
        /// <param name="isConnected">If set to <c>true</c> the location services are connected.</param>
        public LocationStateEventArgs(bool isConnected)
        {
            connectionState = isConnected;
        }
        
        public bool IsConnected
        {
            get
            {
                return connectionState;
            }
        }

    } // end class

    public class LocationEventArgs : EventArgs
    {
        double? latx, lonx, altx, accx;
        DateTime lastSent;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:WatchTower.Droid.LocationEventArgs"/> class.
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="acc">Accuracy</param>
        /// <param name="alt">Altitude</param>
        public LocationEventArgs(double? lat, double? lon, double? acc, double? alt)
        {
            latx = lat;
            lonx = lon;
            altx = alt;
            accx = acc;

            lastSent = DateTime.MinValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:WatchTower.Droid.LocationEventArgs"/> class.
        /// </summary>
        /// <param name="lastSend">Date/time of last update sent</param>
        public LocationEventArgs(DateTime lastSend) : this(null, null, null, null)
        {
            lastSent = lastSend;
        }

        #region Public fields
        /// <summary>
        /// Gets the longitude.
        /// </summary>
        /// <value>The longitude.</value>
        public double? Longitude
        {
            get
            {
                return lonx;
            }
        }

        /// <summary>
        /// Gets the latitude.
        /// </summary>
        /// <value>The latitude.</value>
        public double? Latitude
        {
            get
            {
                return latx;
            }
        }

        /// <summary>
        /// Gets the altitude.
        /// </summary>
        /// <value>The altitude</value>
        public double? Altitude
        {
            get
            {
                return altx;
            }
        }

        /// <summary>
        /// Gets the accuracy.
        /// </summary>
        /// <value>The accuracy.</value>
        public double? Accuracy
        {
            get
            {
                return accx;
            }
        }

        /// <summary>
        /// Gets the date/time for the last update sent
        /// </summary>
        /// <value>The last update sent date/time</value>
        public DateTime LastSent
        {
            get
            {
                return lastSent;
            }
        }

        #endregion

    } // end class

    #endregion
} // End namespace
