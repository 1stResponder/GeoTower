using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Provider;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Preferences;
using Android.Content.PM;
using Android.Widget;
using Android.Util;

namespace WatchTower.Droid
{
    /// <summary>
    /// Parent setting activity, all ti does is load up the headers
    /// </summary>
    [Activity(Label = "Settings", Theme = "@style/AppTheme.NoActionBar", Icon = "@drawable/wt_launch_icon")]
    public class SettingsActivity : PreferenceActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {

        #region Private Fields

        private const string INVALID_POST_INTERVAL = "The Post Interval must be a number greator then 0";
        private static EditTextPreference postIntervalEditText;

        #endregion

        #region Listeners

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Log.Debug("SettingsActivity", "Settings created");
            this.AddPreferencesFromResource(Resource.Layout.preferences);

            postIntervalEditText = (EditTextPreference)FindPreference("post_interval");
            postIntervalEditText.PreferenceChange += onPostIntervalEditTextChanged;          
        }
        
        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            // Setting Value
            switch (key)
            {
                case "send_id":
                    saveSenderID();
                    break;
                case "distribution_id":
                    saveDistributionID();
                    break;
                case "resource_type":
                    saveResourceType();
                    break;
                case "post_url":
                    savePostURL();
                    break;
                case "post_interval":
                    savePostInterval();
                    break;
                case "icon_scale":
                    saveIconScale();
                    break;
                case "mapserver_url":
                    saveMapServerURL();
                    break;
            }

            setPrefSum(key);
        }

        protected override void OnPause()
        {
            base.OnPause();
            Log.Debug("SettingsActivity", "Settings paused");
            PreferenceManager.SharedPreferences.UnregisterOnSharedPreferenceChangeListener(this);
        }

        protected override void OnResume()
        {
            base.OnResume();
            Log.Debug("SettingsActivity", "Settings resumed");
            this.PreferenceManager.SharedPreferences.RegisterOnSharedPreferenceChangeListener(this);
            loadPrefSum();
        }
        
        /// <summary>
        /// Validates the value of the Post Interval when changed
        /// </summary>
        /// <param name="sender">Sender Object</param>
        /// <param name="e">PreferenceChangeEventArgs object</param>
        public void onPostIntervalEditTextChanged(object sender, Preference.PreferenceChangeEventArgs e)
        {
            int value = Int32.Parse(e.NewValue.ToString());

            if (value <= 0)
            {
                // Notify the user
                Toast.MakeText(Android.App.Application.Context, INVALID_POST_INTERVAL, ToastLength.Short).Show();
                e.Handled = false;
            }
        }

        #region Default Listener
        
        protected override void OnStart()
        {
            base.OnStart();
        }
        
        #endregion

        #endregion

        #region Helper Methods - Set Values

        public void saveSenderID()
        {
            EditTextPreference sid = (EditTextPreference)PreferenceManager.FindPreference("send_id");
            AppConfig.Agency = sid.Text;
        }

        public void saveDistributionID()
        {
            EditTextPreference did = (EditTextPreference)PreferenceManager.FindPreference("distribution_id");
            AppConfig.UserID = did.Text;
        }

        public void saveResourceType()
        {
            ListPreference rt = (ListPreference)PreferenceManager.FindPreference("resource_type");
            string[] vals = rt.GetEntryValues();
            int index = rt.FindIndexOfValue(rt.Value);

            AppConfig.SelectedResource = vals[index];
            AppConfig.ResourceIndex = index;
            Log.Debug("SettingsActivity", "Resource type is now " + rt.Entry + "  - " + vals[index]);
        }

        public void savePostURL()
        {
            EditTextPreference post = (EditTextPreference)PreferenceManager.FindPreference("post_url");
            AppConfig.PostUrl = post.Text;
        }

        public void savePostInterval()
        {
            EditTextPreference interval = (EditTextPreference)PreferenceManager.FindPreference("post_interval");
            AppConfig.PostInterval = Int32.Parse(interval.Text);
        }

        public void saveIconScale()
        {
            ListPreference rt = (ListPreference)PreferenceManager.FindPreference("icon_scale");
            string[] vals = rt.GetEntryValues();
            int index = rt.FindIndexOfValue(rt.Value);

            AppConfig.setIconScale(vals[index]);
            Log.Debug("SettingsActivity", "Icon Scale is now " + rt.Entry + "  - " + vals[index]);
        }
        
        public void saveMapServerURL()
        {
            EditTextPreference post = (EditTextPreference)PreferenceManager.FindPreference("mapserver_url");
            AppConfig.MapServerURL = post.Text;
        }

        #endregion

        #region Helper Methods - Populate Summary
        
        /// <summary>
        /// Sets the summary for the Preference with the given key
        /// </summary>
        /// <remarks>
        /// The summary will be the current value for the preference
        /// </remarks>
        /// <param name="key">Key.</param>
        private void setPrefSum(string key)
        {
            Preference pref = FindPreference(key);

            string curValue = null;

            switch (key)
            {
                case "send_id":
                    curValue = AppConfig.Agency;
                    break;
                case "distribution_id":
                    curValue = AppConfig.UserID;
                    break;
                case "resource_type":
                    // Getting list of possible resource types
                    string[] resourceTypes = AppUtil.GetResourceStringArray(Resource.Array.typeentries);
                                 
                    // Getting index of selected value
                    int curIndex = AppConfig.ResourceIndex;

                    // If that is not a valid index (less then 0) our value is a genric one, otherwise
                    curValue = (0 > curIndex) ? "Generic Unit" : resourceTypes[curIndex]; 
                    break;
                case "post_url":
                    curValue = AppConfig.PostUrl;
                    break;
                case "post_interval":
                    curValue = "" + AppConfig.PostInterval;
                    break;
                case "icon_scale":
                    
                    string givenValue = "" + AppConfig.IconScale;
                    
                    string[] scaleEntryList = AppUtil.GetResourceStringArray(Resource.Array.scaleEntries);
                    string[] scaleValueList = AppUtil.GetResourceStringArray(Resource.Array.scaleValues);

                    int index = System.Array.IndexOf(scaleValueList, givenValue);

                    curValue = scaleEntryList[index];
                    break;
                case "mapserver_url":
                    curValue = "" + AppConfig.MapServerURL;
                    break;
            }

            if (!String.IsNullOrWhiteSpace((curValue))) pref.Summary = curValue;
        }

        /// <summary>
        /// Loads each preference from a list in Resources to set it's summary
        /// </summary>
        /// <see cref="setPrefSum"/>
        private void loadPrefSum()
        {
            string[] keys = AppUtil.GetResourceStringArray(Resource.Array.prefKeys);

            foreach (string k in keys)
            {
                setPrefSum(k);
            }
        }
        
        #endregion

    }
}
