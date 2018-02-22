using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Locations;
using Android.Util;
using Android.Text;

namespace WatchTower.Droid
{
    public class LocationFragment : Fragment
    {
        View myView;
        TextView latText;
        TextView longText;
        TextView accText;
        TextView altText;
        TextView lastSentTimeText;
        TextView lastSentDateText;
        TextView lastUpdateText;
        public Switch updates;
        DateTime lastUpdate;

        System.Timers.Timer sentTimeUpdate;

        private LocationBroadcastReceiver locationUpdateReceiver;
        private static readonly string defaultValue = AppUtil.GetResourceString(Resource.String.def_value);

        #region init

        private void Initialize()
        {
            locationUpdateReceiver = new LocationBroadcastReceiver();
            locationUpdateReceiver.locationUpdate += onNewLocationUpdate;
            locationUpdateReceiver.SendUpdate += onNewSentUpdate;
            locationUpdateReceiver.ConnectionStateChange += onStateChange;

            sentTimeUpdate = new System.Timers.Timer();
            sentTimeUpdate.Interval = 1000 * 5;
            sentTimeUpdate.Elapsed += updateLastSent;
            sentTimeUpdate.AutoReset = true;

            lastUpdate = DateTime.MinValue;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Initialize();

            myView = inflater.Inflate(Resource.Layout.location_layout, container, false);
            latText = myView.FindViewById<TextView>(Resource.Id.lat);
            longText = myView.FindViewById<TextView>(Resource.Id.longx);
            altText = myView.FindViewById<TextView>(Resource.Id.alt);
            accText = myView.FindViewById<TextView>(Resource.Id.acc);
            lastSentTimeText = myView.FindViewById<TextView>(Resource.Id.last_sent_time);
            lastSentDateText = myView.FindViewById<TextView>(Resource.Id.last_sent_date);
			lastUpdateText = myView.FindViewById<TextView>(Resource.Id.last_location);
            lastUpdateText.SetFilters(new IInputFilter[] { new Filter.TimeFilter() });

            return myView;
        }

        #endregion

        #region Override

        public override void OnResume()
        {
            base.OnResume();
            sentTimeUpdate.Enabled = true;
        }

        public override void OnStart()
        {
            base.OnStart();

            // broadcast receiver
            IntentFilter fil = new IntentFilter(AppUtil.LOCATION_UPDATE_ACTION);
            fil.AddAction(AppUtil.LOCATION_UPDATE_ACTION);
            fil.AddAction(AppUtil.LOCATION_LAST_SENT_ACTION);
            fil.AddAction(AppUtil.LOCATION__CONNECT_ACTION);
            fil.AddAction(AppUtil.LOCATION__DISCONNECT_ACTION);
            fil.Priority = 90;
            Android.App.Application.Context.RegisterReceiver(locationUpdateReceiver, fil);
        }

        public override void OnStop()
        {
            base.OnStop();
            sentTimeUpdate.Enabled = false;
            Android.App.Application.Context.UnregisterReceiver(locationUpdateReceiver);
        }

        public override void OnPause()
        {
            base.OnPause();
            sentTimeUpdate.Enabled = false;
        }

        #endregion


        #region callback

        /// <summary>
        /// Callback for the last update timer.  
        /// Updates the time since the last update has been received
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        private void updateLastSent(object sender, System.Timers.ElapsedEventArgs args)
        {
			((MainActivity)Activity).RunOnUiThread(delegate
            {
	            if (lastUpdate != DateTime.MinValue)
	            {
	                lastUpdateText.Text = lastUpdate.ToString();
	            }
            });
        }

        /// <summary>
        /// Callback for location updates
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void onNewLocationUpdate(object sender, LocationEventArgs e)
        {           
            setValues(e.Latitude, e.Longitude, e.Altitude, e.Accuracy);
        }
        
        /// <summary>
        /// Callback for location state changes.  Called when the location service connects or disconnects
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void onStateChange(object sender, LocationStateEventArgs e)
        {
            string t = "";
            
            // notify user... lets see how picky this is first
        }

        
        /// <summary>
        /// Callback for send updates
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void onNewSentUpdate(object sender, LocationEventArgs e)
        {           
            setValues(e.LastSent);
        }

        #endregion
        #region Helper methods

        public void updateView(Location location, DateTime lastUpdateSent, DateTime lastLocationUpdate)
        {
        }

        /// <summary>
        /// Sets the values for the text views, based on the param
        /// </summary>
        /// <param name="lastSentx">Last sent message DateTime</param>
        private void setValues(DateTime lastSentx)
        {
            if (lastSentx != DateTime.MinValue)
            {
                lastSentTimeText.Text = lastSentx.ToLongTimeString();
                lastSentDateText.Text = lastSentx.ToShortDateString();
            }   
        }

        /// <summary>
        /// Sets the values for the text views, based on the param
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="alt">Altitude</param>
        /// <param name="acc">Accuracy</param>
        private void setValues(double? lat, double? lon, double? alt, double? acc)
        {
            ((MainActivity)Activity).RunOnUiThread(delegate
            {
            if (lat != null)
            {
                latText.Text = String.Format("{0:f3}", lat);
            }

            if (lon != null)
            {
                longText.Text = String.Format("{0:f3}", lon);
            }

            if (alt != null)
            {
                altText.Text = String.Format("{0:f3}", alt);
            }

            if (acc != null)
            {
                accText.Text = String.Format("{0:f3}", acc);
            }
            
                lastUpdate = DateTime.Now;
                lastUpdateText.Text = lastUpdate.ToString();
                
            });
        }

        /// <summary>
        /// Clears the values for the text views
        /// </summary>
        private void clearValues()
        {
            ((MainActivity)Activity).RunOnUiThread(delegate
            {
                string defValueString = AppUtil.GetResourceString(Resource.String.def_value);

                lastSentTimeText.Text = defValueString;
                lastSentDateText.Text = "";
                lastUpdateText.Text = defValueString;
                accText.Text = defValueString;
                altText.Text = defValueString;
                latText.Text = defValueString;
                longText.Text = defValueString;

                lastUpdate = DateTime.MinValue;
            });
        }
        #endregion

    }
}




