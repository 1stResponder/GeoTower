using System;
using Android.Content;
using System.Collections.Generic;
using System.Linq;

namespace WatchTower.Droid
{
    public static class AppConfig
    {

        #region Constants
        // -- KEYS FOR VALUES
        private static readonly string RESOURCE_AGENCY = AppUtil.GetResourceString(Resource.String.agency);
        private static readonly string RESOURCE_USERID = AppUtil.GetResourceString(Resource.String.distID);
        private static readonly string RESOURCE_POSTURL = AppUtil.GetResourceString(Resource.String.postURL);
        private static readonly string RESOURCE_MAPSERVER_URL = AppUtil.GetResourceString(Resource.String.mapServerURL);
        private static readonly string RESOURCE_SELECTED_RESOURCE = AppUtil.GetResourceString(Resource.String.resourceSelected);
        private static readonly string RESOURCE_RESOURCE_INDEX = AppUtil.GetResourceString(Resource.String.resourceIndex);
        private static readonly string RESOURCE_POSTINTERVAL = AppUtil.GetResourceString(Resource.String.postInterval);
        private static readonly string RESOURCE_POSTUPDATES = AppUtil.GetResourceString(Resource.String.post_update);
        private static readonly string RESOURCE_POSTSENSORUPDATES = AppUtil.GetResourceString(Resource.String.post_sensor_update);
        private static readonly string SAVED_SENSOR = AppUtil.GetResourceString(Resource.String.saved_sensor);
        private static readonly string RESOURCE_ICON_SCALE = AppUtil.GetResourceString(Resource.String.iconScale);

        // -- DEFAULT VALUES
        private static readonly string RESOURCE_POST_URL_DEFAULT = AppUtil.GetResourceString(Resource.String.postURL_default);
        private static readonly string RESOURCE_SELECTED_DEFAULT = AppUtil.GetResourceString(Resource.String.resourceSelected_default);
        private static readonly string RESOURCE_MAPSERVER_URL_DEFAULT = AppUtil.GetResourceString(Resource.String.mapServerURL_default);
        private static readonly int RESOURCE_INDEX_DEFAULT = AppUtil.GetResourceInt(Resource.String.resourceIndex_default);
        private static readonly int RESOURCE_POST_INTERVAL_DEFAULT = AppUtil.GetResourceInt(Resource.String.postInterval_default);
        private static readonly string RESOURCE_ICON_SCALE_DEFAULT = AppUtil.GetResourceString(Resource.String.iconScale_default);
        private static readonly bool RESOURCE_POST_UPDATE_DEFAULT = AppUtil.GetResourceBool(Resource.String.post_update_default);
        private static readonly bool RESOURCE_POST_SENSOR_UPDATE_DEFAULT = AppUtil.GetResourceBool(Resource.String.post_sensor_update_default);
        #endregion


        #region public methods

        /// <summary>
        /// Initialize the values for the config.  Called when the app first starts
        /// </summary>
        public static void Initialize()
        {
            ResetDefaultPrefs();
            FillInDefault();
        }

        /// <summary>
        /// Sets the values for the prefs that should be reset each time the program runs
        /// </summary>
        public static void ResetDefaultPrefs()
        {
            PostUpdates = RESOURCE_POST_UPDATE_DEFAULT;
            PostSensorUpdates = RESOURCE_POST_SENSOR_UPDATE_DEFAULT;
        }

        /// <summary>
        /// Fills in the empty prefs with their default values if they were not already set
        /// </summary>
        public static void FillInDefault()
        {
            string defValue;

            defValue = Agency;
            Agency = defValue;

            defValue = UserID;
            UserID = defValue;

            defValue = PostUrl;
            PostUrl = defValue;

            defValue = SelectedResource;
            SelectedResource = defValue;

            defValue = iconScale;
            iconScale = defValue;

            defValue = MapServerURL;
            MapServerURL = defValue;

            int defInt;

            defInt = ResourceIndex;
            ResourceIndex = defInt;

            defInt = PostInterval;
            PostInterval = defInt;         
        }
        
        /// <summary>
        /// Adds the value to the SavedSensor Dictionary.  If the key already exists then value is updated
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="name">Name.</param>
        public static void addToSavedSensorDictionary(string address, string name)
        {
            Dictionary<string, string> tempDiction = SavedSensorDictionary;

            if (!SavedSensorDictionary.ContainsKey(address))
            {
                tempDiction.Add(address, name);
            }
            else
            {
                tempDiction[address] = name;
            }

            savedSensorDictionary = tempDiction;
        }
        
        /// <summary>
        /// Removes the given key from the SavedSensor Dictionary.
        /// </summary>
        /// <param name="address">Address.</param>
        public static void removeFromSavedSensorDictionary(string address)
        {
            if (SavedSensorDictionary.ContainsKey(address))
            {
                Dictionary<string, string> tempDiction = new Dictionary<string, string>(SavedSensorDictionary);
                tempDiction.Remove(address);
                savedSensorDictionary = new Dictionary<string, string>(tempDiction);
            }
        }

        /// <summary>
        /// Gets the name of the device based on what'sremov been saved.
        /// Can be used to get the device name when the device is not available
        /// </summary>
        /// <returns>The device name.</returns>
        /// <param name="address">Address.</param>
        public static string getDeviceName(string address)
        {
            if (SavedSensorDictionary.ContainsKey(address))
            {
                return SavedSensorDictionary[address];
            }
            else
            {
                return null;
            }
        }  
        
        /// <summary>
        /// Sets the Icon Scale From a string
        /// </summary>
        /// <param name="value">Value.</param>
        public static void setIconScale(string value)
        {
            iconScale = value;
        }
        
        /// <summary>
        /// Sets the icon scale from a double
        /// </summary>
        /// <param name="value">Value.</param>
        public static void setIconScale(double value)
        {
            setIconScale("" + value);        
        }
        
        #endregion


        #region private helper methods

        /// <summary>
        /// Gets the string dictionary for the Preference with the given key.
        /// If the Preference does not yet have a value, sets it to be the given default value
        /// </summary>
        /// <remarks>
        /// Asssumes the Dictionary is stored as a list of strings.  Where each list item
        /// is key<delimiter>value
        /// </remarks>
        /// <returns>The string map value.</returns>
        /// <param name="key">Preference Key</param>
        /// <param name="defValue">Default value.</param>
        /// <param name="delimiter">Delimiter for the stored dictionary</param>
        private static Dictionary<string, string> getStringDictionary(string key, Dictionary<string, string> defValue, char delimiter)
        {

            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            Dictionary<string, string> finalList;

            try
            {
                finalList = new Dictionary<string, string>();

                List<string> rawList = getStringListValue(key, new List<string>());

                foreach (string s in rawList)
                {
                    string address = s.Split(',')[0];
                    string name = s.Split(',')[1];

                    finalList.Add(address, name);
                }
            }
            catch (Exception e)
            {
                finalList = defValue;
            }
            return finalList;
        }

        /// <summary>
        /// Gets the string list value for the Preference with the given key.
        /// If the Preference does not yet have a value, sets it to be the given default value
        /// </summary>
        /// <returns>The string list value.</returns>
        /// <param name="key">Preference Key</param>
        /// <param name="defValue">Default value.</param>
        private static List<string> getStringListValue(string key, List<string> defValue)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            List<string> val;
            try
            {
                val = new List<string>(prefs.GetStringSet(key, defValue));
            }
            catch (Exception e)
            {
                val = defValue;
            }
            return val;
        }

        /// <summary>
        /// Gets the string value for the Preference with the given key.
        /// If the Preference does not yet have a value, sets it to be the given default value
        /// </summary>
        /// <returns>The string value.</returns>
        /// <param name="key">Preference Key</param>
        /// <param name="defValue">Default value.</param>
        private static string getStringValue(string key, string defValue)
        {

            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            string val;
            try
            {
                val = prefs.GetString(key, defValue);
            }
            catch (Exception e)
            {
                val = defValue;
            }
            return val;
        }

        /// <summary>
        /// Gets the int value for the Preference with the given key.
        /// If the Preference does not yet have a value, sets it to be the given default value
        /// </summary>
        /// <returns>The int value.</returns>
        /// <param name="key">Preference Key</param>
        /// <param name="defValue">Default value.</param>
        private static int getIntValue(string key, int defValue)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            int val;
            try
            {
                val = prefs.GetInt(key, defValue);
            }
            catch (Exception e)
            {
                val = defValue;
            }
            return val;
        }

        /// <summary>
        /// Gets the bool value for the Preference with the given key.
        /// If the Preference does not yet have a value, sets it to be the given default value
        /// </summary>
        /// <returns><c>true</c>, if bool value was gotten, <c>false</c> otherwise.</returns>
        /// <param name="key">Preference Key</param>
        /// <param name="defValue">Default value.</param>
        private static bool getBoolValue(string key, bool defValue)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            bool val;

            try
            {
                val = prefs.GetBoolean(key, defValue);
            }
            catch (Exception e)
            {
                val = defValue;
            }
            return val;
        }

        /// <summary>
        /// Sets the string value for the Preference with the given key
        /// </summary>
        /// <param name="key">Preference Key<</param>
        /// <param name="value">Value.</param>
        private static void setStringValue(string key, string value)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            ISharedPreferencesEditor editor = prefs.Edit();

            editor.PutString(
                key,
                value
            );

            editor.Apply();
        }

        /// <summary>
        /// Sets the string list value for the Preference with the given key
        /// </summary>
        /// <param name="key">Preference Key<</param>
        /// <param name="value">Value.</param>
        private static void setStringListValue(string key, List<string> value)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            ISharedPreferencesEditor editor = prefs.Edit();

            editor.PutStringSet(
                key,
                value
            );

            editor.Apply();
        }

        /// <summary>
        /// Sets the string dictionary value for the Preference with the given key
        /// </summary>
        /// <remarks>
        /// Asssumes the Dictionary is stored as a list of strings.  Where each list item
        /// is key<delimiter>value
        /// </remarks>
        /// <param name="key">Preference Key<</param>
        /// <param name="value">Value.</param>
        /// <param name="delimiter">Delimiter for the stored dictionary</param>
        private static void setStringDictionaryValue(string key, Dictionary<string, string> value, char delimiter)
        {
            setStringDictionaryValue(key, value, "" + delimiter);
        }

        /// <summary>
        /// Sets the string dictionary value for the Preference with the given key
        /// </summary>
        /// <remarks>
        /// Asssumes the Dictionary is stored as a list of strings.  Where each list item
        /// is key<delimiter>value
        /// </remarks>
        /// <param name="key">Preference Key<</param>
        /// <param name="value">Value.</param>
        /// <param name="delimiter">Delimiter for the stored dictionary</param>
        private static void setStringDictionaryValue(string key, Dictionary<string, string> value, string delimiter)
        {
            List<string> newValue = new List<string>();

            foreach (string address in value.Keys)
            {
                newValue.Add(address + delimiter + value[address]);
            }

            setStringListValue(key, newValue);
        }
        
        /// <summary>
        /// Sets the int value for the Preference with the given key
        /// </summary>
        /// <param name="key">Preference Key<</param>
        /// <param name="value">Value.</param>
        private static void setIntValue(string key, int value)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            ISharedPreferencesEditor editor = prefs.Edit();

            editor.PutInt(
                key,
                value
            );

            editor.Apply();
        }

        /// <summary>
        /// Sets the boolean value for the Preference with the given key
        /// </summary>
        /// <param name="key">Preference Key<</param>
        /// <param name="value">Value.</param>
        private static void setBoolValue(string key, bool value)
        {
            var prefs = Android.App.Application.Context.GetSharedPreferences("WatchTower.preferences", FileCreationMode.Private);
            ISharedPreferencesEditor editor = prefs.Edit();

            editor.PutBoolean(
                key,
                value
            );

            editor.Apply();
        }

        #endregion
        
        #region Data members

        /// <summary>
        /// Gets or sets the map server URL.
        /// </summary>
        /// <value>The map server URL.  This where features will be retrieved from</value>
        public static string MapServerURL
        {
            get
            {
                return getStringValue(RESOURCE_MAPSERVER_URL, RESOURCE_MAPSERVER_URL_DEFAULT);
            }
            
            set
            {
                setStringValue(RESOURCE_MAPSERVER_URL, value);
            }
        }

        /// <summary>
        /// Gets or sets the agency for this user
        /// </summary>
        /// <value>The agency.</value>
        public static string Agency
        {
            get
            {
                string defValue = Android.OS.Build.Model + "_sender";
                return getStringValue(RESOURCE_AGENCY, defValue);
            }

            set
            {
                setStringValue(RESOURCE_AGENCY, value);
            }

        }

        /// <summary>
        /// Gets or sets the User ID (or Unit ID).  This acts as the unique
        /// identifier for the Device within the DE message
        /// </summary>
        /// <remarks>
        /// This is known as the distribution ID within Fresh
        /// </remarks>
        /// <value>The user identifier.</value>
        public static string UserID
        {
            get
            {
                return getStringValue(RESOURCE_USERID, Android.OS.Build.Model);
            }

            set
            {
                setStringValue(RESOURCE_USERID, value);
            }
        }

        /// <summary>
        /// Gets or sets the post URL.  This is the URL that updates
        /// are sent to.
        /// </summary>
        /// <value>The post URL.</value>
        public static string PostUrl
        {
            get
            {
                return getStringValue(RESOURCE_POSTURL, RESOURCE_POST_URL_DEFAULT);
            }

            set
            {
                setStringValue(RESOURCE_POSTURL, value);
            }
        }

        /// <summary>
        /// Gets or sets the selected resource type for the user
        /// </summary>
        /// <value>The selected resource type.</value>
        public static string SelectedResource
        {
            get
            {
                return getStringValue(RESOURCE_SELECTED_RESOURCE, RESOURCE_SELECTED_DEFAULT);
            }

            set
            {
                setStringValue(RESOURCE_SELECTED_RESOURCE, value);
            }
        }

        /// <summary>
        /// Gets or Sets the list index of the Selected Resource
        /// </summary>
        /// <value>The list index of the Selected Resource.</value>
        public static int ResourceIndex
        {
            get
            {
                return getIntValue(RESOURCE_RESOURCE_INDEX, RESOURCE_INDEX_DEFAULT);
            }

            set
            {
                setIntValue(RESOURCE_RESOURCE_INDEX, value);
            }
        }

        /// <summary>
        /// Interval in milliseconds for how often the service should post updates
        /// </summary>
        /// <value>The post interval.</value>
        public static int PostInterval
        {
            get
            {
                return getIntValue(RESOURCE_POSTINTERVAL, RESOURCE_POST_INTERVAL_DEFAULT);
            }

            set
            {
                setIntValue(RESOURCE_POSTINTERVAL, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not updates should be posted
        /// </summary>
        /// <value>if <c>true</c> the service posts. Otherwise, updates are not posted</value>
        public static bool PostUpdates
        {
            get
            {
                return getBoolValue(RESOURCE_POSTUPDATES, RESOURCE_POST_UPDATE_DEFAULT);
            }

            set
            {
                setBoolValue(RESOURCE_POSTUPDATES, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not sensor updates should be posted
        /// </summary>
        /// <remarks>Depends on PostUpdates</remarks>
        /// <value>if <c>true</c> the service posts sensor updates. Otherwise, updates are not posted</value>
        public static bool PostSensorUpdates
        {
            get
            {
                return getBoolValue(RESOURCE_POSTSENSORUPDATES, RESOURCE_POST_SENSOR_UPDATE_DEFAULT);
            }

            set
            {
                setBoolValue(RESOURCE_POSTSENSORUPDATES, value);
            }
        }

        /// <summary>
        /// Gets or sets the scale for the icons
        /// </summary>
        /// <value>The icon scale.</value>
        /// <remarks>
        /// Had to be saved as a string due to how Resource can store values.
        /// </remarks>
        public static double IconScale
        {
            get
            {
                return Double.Parse(iconScale);
            }
            
            set
            {
                iconScale = "" + value;
            }

        }
        
        /// <summary>
        /// Gets the Dictionary which holds the saved Sesnors.
        /// </summary>
        /// <remarks>
        /// The Dictionary Key is the sensor address, and it's value is the sensor name.
        /// </remarks>
        /// <value>The Dictionary of saved Sensors.  The Dictionary key is the 
        /// sensor address and it's value is the sensor name.
        /// </value>
        public static Dictionary<string, string> SavedSensorDictionary
        {
            get
            {
                return savedSensorDictionary;
            }
        }

        #region Private Data Members

        /// <summary>
        /// Gets or sets the scale for the icons
        /// </summary>
        /// <value>The icon scale.</value>
        /// <remarks>
        /// Had to be saved as a string due to how Resource can store values.
        ///</remarks>
        private static string iconScale
        {
            get
            {
                return getStringValue(RESOURCE_ICON_SCALE, RESOURCE_ICON_SCALE_DEFAULT);
            }
            
            set
            {
                setStringValue(RESOURCE_ICON_SCALE, value);       
            }
        
        }
        
        /// <summary>
        /// Gets or sets the Dictionary which holds the saved Sesnors.
        /// </summary>
        /// <remarks>
        /// The Dictionary Key is the sensor address, and it's value is the sensor name.
        /// </remarks>
        /// <value>The Dictionary of saved Sensors.  The Dictionary key is the 
        /// sensor address and it's value is the sensor name.
        /// </value>
        private static Dictionary<string, string> savedSensorDictionary
        {
            get
            {
                return getStringDictionary(SAVED_SENSOR, new Dictionary<string, string>(), ',');
            }

            set
            {
                setStringDictionaryValue(SAVED_SENSOR, value, ',');
            }

        }

        #endregion    

        #endregion

    }
}
