using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.IO;
using Android.Util;
using Android.Graphics.Drawables;
using static Android.Graphics.BitmapFactory;

namespace WatchTower.Droid
{
    public static class AppUtil
    {

        #region Constants
        
        // Key used for passing new devices address between activities/services
        public static string ADD_DEVICE_KEY = AppUtil.GetResourceString(Resource.String.add_device_key);
        // Key used for passing list of known connected devices
        public static string CONNECTED_DEVICE_KEY = AppUtil.GetResourceString(Resource.String.connected_device_key);

        // Value Keys
        public static readonly string ADDRESS_KEY = AppUtil.GetResourceString(Resource.String.sensorBroadcastAddressKey);
        public static readonly string DETAIL_KEY = AppUtil.GetResourceString(Resource.String.sensorBroadcastDetailKey);
        public static readonly string NAME_KEY = AppUtil.GetResourceString(Resource.String.sensorBroadcastNameKey);
        public static readonly string TIME_KEY = AppUtil.GetResourceString(Resource.String.sensorBroadcastTimeKey);

        public static readonly string LAT_KEY = AppUtil.GetResourceString(Resource.String.locationBroadcastLatKey);
        public static readonly string LON_KEY = AppUtil.GetResourceString(Resource.String.locationBroadcastLonKey);
        public static readonly string ALT_KEY = AppUtil.GetResourceString(Resource.String.locationBroadcastAltKey);
        public static readonly string ACC_KEY = AppUtil.GetResourceString(Resource.String.locationBroadcastAccKey);
        public static readonly string LAST_SENT_KEY = AppUtil.GetResourceString(Resource.String.locationBroadcastLastSentKey);

        // Intent Actions
        public static readonly string SENSOR_DISCONNECT_ACTION = AppUtil.GetResourceString(Resource.String.sensorDisconnectAction);
        public static readonly string SENSOR_CONNECT_ACTION = AppUtil.GetResourceString(Resource.String.sensorConnectAction);
        
        public static readonly string SENSOR_PAUSE_ACTION = AppUtil.GetResourceString(Resource.String.sensorBroadcastPauseIntent);
        
        public static readonly string SENSOR_ADDED_ACTION = AppUtil.GetResourceString(Resource.String.sensorBroadcastAddIntent);
        public static readonly string SENSOR_REMOVED_ACTION = AppUtil.GetResourceString(Resource.String.sensorBroadcastRemoveIntent);
        
        public static readonly string SENSOR_READING_UPDATE_ACTION = AppUtil.GetResourceString(Resource.String.sensorReadingUpdateIntent);

        public static readonly string LOCATION_UPDATE_ACTION = AppUtil.GetResourceString(Resource.String.locationUpdateAction);
        public static readonly string LOCATION_LAST_SENT_ACTION = AppUtil.GetResourceString(Resource.String.locationLastSentAction);
        public static readonly string LOCATION__DISCONNECT_ACTION = AppUtil.GetResourceString(Resource.String.locationDisconnectAction);
        public static readonly string LOCATION__CONNECT_ACTION = AppUtil.GetResourceString(Resource.String.locationConnectAction);

        

        public const long SENSOR_CONNECT_TIMEOUT = 1000 * 20;
        public const long HEXOSKIN_CONNECT_TIMEOUT = 1000 * 120;
        public static readonly long HEXOSKIN_INTERVAL = AppUtil.GetResourceInt(Resource.String.hexoSkinTimeSec) * 1000;
        
        // Log
        private const string TAG = "AppUtil";

        #endregion

        /// <summary>
        /// If bluetooth is not enabled, attemtps to connect it
        /// </summary>
        /// <returns><c>true</c>, If the Bluetooth was successfully enabled, <c>false</c> otherwise.</returns>
        /// <param name="adpt">Bluetooth Adapter</param>
        public static bool checkIfBluetoothEnabled(BluetoothAdapter adpt)
        {
   
            try
            {
                // If the bluetooth is not enabled, attempt to enable it
                if (!(adpt.IsEnabled))
                {
                    Log.Debug(TAG + "-checkIfBluetoothEnabled", "Bluetooth is not enabled.  Attempting to enable");
                    
                    bool isTime = false;

                    adpt.Enable();

                    // Time out for attempting to connect
                    System.Timers.Timer r = new System.Timers.Timer();
                    r.Interval = 20 * 1000;
                    r.Elapsed += (sender, e) => isTime = true;
                    r.Enabled = true;

                    while (!(adpt.IsEnabled || isTime))
                    {
                        // put a timer here
                    }
                }
            }
            catch (Exception e)
            {
                // Error occured when attempting to enable bluetooth
                // Throw some sort of error/waring here, look into what would be best. 
                Log.Error(TAG + "-checkIfBluetoothEnabled", "Failed to enable bluetooth: " + e.Message);
                return false;
            }

            if (!(adpt.IsEnabled))
            {
                Log.Warn(TAG + "-checkIfBluetoothEnabled", "Bluetooth is not enabled.");
                return false;
            }

            Log.Debug(TAG + "-checkIfBluetoothEnabled", "Bluetooth is enabled");
            return true;
        }

        /// <summary>
        /// Adds the already paired devices to list of devices.
        /// Will not duplicate if the paired device is already in the given list.
        /// </summary>
        /// <returns>The list of paired devices</returns>
        /// <param name="adpt">Bluetooth Adapter</param>
        /// <param name="devices">List to add devices to.  Optional parameter</param>
        public static List<BluetoothDevice> addPairedDevices(BluetoothAdapter adpt, List<BluetoothDevice> devices = null)
        {
            var deviceList = adpt.BondedDevices.ToList();
            
            Log.Debug(TAG + "-addPairedDevices", string.Format("Found {0} paired devices",deviceList.Count));

            if (devices != null)
            {
                devices.AddRange(deviceList);
                devices = devices.Distinct().ToList();
                return devices;
            }

            return deviceList;
        }

        /// <summary>
        /// Pairs the device if needed
        /// </summary>
        /// <param name="dev">Bluetooth device</param>
        public static void pairDevice(BluetoothDevice dev)
        {      
            if (!(dev.BondState == Bond.Bonded))
            {
                Log.Debug(TAG + "-pairDevice", "Attempting to pair device");
                dev.CreateBond();
            } else
            {
                Log.Debug(TAG + "-pairDevice", "Device Already Paired");
            }
        }

        /// <summary>
        /// Gets the bluetooth device with the given address
        /// </summary>
        /// <param name="address">Address of device</param>
        public static BluetoothDevice getDevice(string address, BluetoothAdapter adpt)
        {
            try
            {
                Log.Debug(TAG + "-getDevice", "Looking for the BluetoothDevice with the address: " + address);
            
                BluetoothDevice dev = adpt.GetRemoteDevice(address);
                return dev;
                
            } catch (Exception e)
            {
                 Log.Error(TAG + "-getDevice", string.Format("Could not get the BluetoothDevice with the address {0}.  Device was not found",address));
                 
                return null;
            }
        }
        
        /// <summary>
        /// Gets the name of the device with the given address
        /// </summary>
        /// <returns>The device name.</returns>
        /// <param name="address">Address.</param>
        /// <param name="adpt">Adpt.</param>
        public static string getDeviceName(string address, BluetoothAdapter adpt)
        {
            Log.Debug(TAG + "-getDeviceName", string.Format("Device with address {0} has the name {1}",address,getDevice(address, adpt).Name));
            
            return getDevice(address, adpt).Name;
        }

        /// <summary>
        /// Returns the type of connection the bluetooth device requires
        /// </summary>
        /// <param name="dev">Dev.</param>
        public static BluetoothDeviceType getDeviceType(BluetoothDevice dev)
        {
            if (dev != null)
            {
                Log.Debug(TAG + "-getDeviceType", string.Format("The type for this device is {0}", dev.Type));
                return dev.Type;
                
            } else
            {
                Log.Warn(TAG + "-getDeviceType","The BluetoothDevice was null!  Returning unknown type");
                return BluetoothDeviceType.Unknown;
                
            }
        }

        /// <summary>
        /// Converts the given resource id to a string array
        /// </summary>
        /// <returns>The resource string array</returns>
        /// <param name="resourceID">Resource identifier.</param>
        public static string[] GetResourceStringArray(int resourceID)
        {
            string[] value = Android.App.Application.Context.Resources.GetStringArray(resourceID);
            Log.Debug(TAG + "-GetResourceString", string.Format("The string array for resource ID {0} has {1} values", resourceID, value.Count()));
            return value;
        }
        
        /// <summary>
        /// Converts the given resource id to a string
        /// </summary>
        /// <returns>The resource string.</returns>
        /// <param name="resourceID">Resource identifier.</param>
        public static string GetResourceString(int resourceID)
        {
            String value = Android.App.Application.Context.Resources.GetString(resourceID);
        
            Log.Debug(TAG + "-GetResourceString", string.Format("The string value for resource ID {0} is {1}", resourceID, value));
            return value;
        }

        /// <summary>
        /// Converts the given resource id to it's int value
        /// </summary>
        /// <returns>The resource string's int value</returns>
        /// <param name="context">Context.</param>
        /// <param name="resourceID">Resource identifier.</param>
        public static int GetResourceInt(int resourceID)
        {
            int value;

            if(Int32.TryParse(GetResourceString(resourceID), out value))
            {
                Log.Debug(TAG + "-GetResourceInt", string.Format("The int value for resource ID {0} is {1}", resourceID, value));
                return value;
            } else
            {
                Log.Error(TAG + "-GetResourceInt", string.Format("Could not get the int value for the resource ID {0} i", resourceID));
                throw new ArgumentException(string.Format("Could not get the int value for the resource ID {0} i", resourceID));
            }  
        }

        /// <summary>
        /// Converts the given resource id to it's double value
        /// </summary>
        /// <returns>The resource string's int value</returns>
        /// <param name="context">Context.</param>
        /// <param name="resourceID">Resource identifier.</param>
        public static double GetResourceDouble(int resourceID)
        {
            double value;
            
            if(Double.TryParse(GetResourceString(resourceID), out value))
            {
                Log.Debug(TAG + "-GetResourceDouble", string.Format("The double value for resource ID {0} is {1}", resourceID, value));
                return value;
            } else
            {
                Log.Error(TAG + "-GetResourceDouble", string.Format("Could not get the double value for the resource ID {0} i", resourceID));
                throw new ArgumentException(string.Format("Could not get the double value for the resource ID {0} i", resourceID));
            } 
        }

        /// <summary>
        /// Converts the given resource id to it's bool value
        /// </summary>
        /// <returns>The resource string's bool value</returns>
        /// <param name="resourceID">Resource identifier.</param>
        /// <exception cref="ArgumentException">The bool value could not be parsed from the given value</exception> 
        public static bool GetResourceBool(int resourceID)
        {
            bool value;
            
            if (bool.TryParse(GetResourceString(resourceID), out value))
            {
                Log.Debug(TAG + "-GetResourceBool", string.Format("The bool value for resource ID {0} is {1}", resourceID, value));
                return value;
            }
            else
            {
                Log.Error(TAG + "-GetResourceBool", string.Format("Could not get the bool value for the resource ID {0} i", resourceID));
                throw new ArgumentException(string.Format("Could not get the bool value for the resource ID {0} i", resourceID));
            }  
        }

        /// <summary>
        /// Is this kind of bluetooth device currently supported by the Android app
        /// </summary>
        /// <returns><c>true</c>, if supported was ised, <c>false</c> otherwise.</returns>
        /// <param name="dev">Dev.</param>
        public static bool isSupported(BluetoothDevice dev)
        {    
            string name = "";

            foreach (char x in dev.Name)
            {
                if (Char.IsLetter(x))
                {
                    name = name + x;
                }
                else
                {
                    break;
                }
            }

            // parse for letters of name
            if (SensorConfig.getSupportedDevices().Contains(name))
            {
                Log.Debug(TAG + "-isSupported", string.Format("Device with name {0} is supported", name));
                return true;
            }

            Log.Debug(TAG + "-isSupported", string.Format("Device with name {0} is not supported", name));
            return false;
        }
        
        /// <summary>
        /// Get the equvialent bitmap for the icon and resizes it
        /// </summary>
        /// <returns>The icon bitmap.</returns>
        /// <param name="context">Context.</param>
        /// <param name="icon">Icon.</param>
        public static Bitmap getIconBitmap(String iconName) 
        {
            

            if (!string.IsNullOrWhiteSpace(iconName))
            {
                // Removing the extension from the icon name if it contains one
                string iconNameNoExt = null;

                if (iconName.Contains('.'))
                {
                    iconNameNoExt = iconName.Substring(0, iconName.IndexOf('.'));
                }
                
                Log.Debug(TAG + "-getIconBitmap", string.Format("The parsed icon name is {0}", iconNameNoExt));

                try
                {
                    int drawID = (int)typeof(Resource.Drawable).GetField(iconNameNoExt).GetValue(null);
                    Drawable iconDrawable = Android.App.Application.Context.GetDrawable(drawID);

                    Bitmap imageBitmap = ((BitmapDrawable)iconDrawable).Bitmap;

                    int newWidth = (int)Math.Floor(imageBitmap.Width / AppConfig.IconScale);
                    int newHeight = (int)Math.Floor(imageBitmap.Height / AppConfig.IconScale);

                    Log.Debug(TAG + "-getIconBitmap", string.Format("Got the drawable with the name {0}", iconNameNoExt));
                    
                    return getResizedBitmap(imageBitmap, newWidth, newHeight);
                }
                catch (Exception e)
                {
                    // The icon was not found as a drawable
                    Log.Debug(TAG + "-getIconBitmap", string.Format("Could not find a drawable with the name {0}: {1}", iconNameNoExt, e.Message));
                }
            } else
            {
                Log.Error(TAG + "-getIconBitmap","The icon name was null or empty");
            }      
 
            return null;  
		}
        
        public static Bitmap getResizedBitmap(Bitmap bitmap, int newWidth, int newHeight) {
        
            Log.Debug(TAG + "-getResizedBitmap", "Resizing the icon");
            
	        Bitmap resizedBitmap = Bitmap.CreateBitmap(newWidth, newHeight, Bitmap.Config.Argb8888);

            float scaleX = newWidth / (float)bitmap.Width;
            float scaleY = newHeight / (float)bitmap.Height;
	        float pivotX = 0;
	        float pivotY = 0;
	
	        Matrix scaleMatrix = new Matrix();
	        scaleMatrix.SetScale(scaleX, scaleY, pivotX, pivotY);
	
	        Canvas canvas = new Canvas(resizedBitmap);
            canvas.Matrix = scaleMatrix;
	        canvas.DrawBitmap(bitmap, 0, 0, new Paint(PaintFlags.FilterBitmap));
	
	        return resizedBitmap;
    }
    
    /// <summary>
    /// Converts the epoch UNIX string into a DateTime object
    /// </summary>
    /// <returns>The unix time.</returns>
    /// <param name="unixTime">Unix time.</param>
    public static DateTime FromUnixTime(long unixTime)
	{
	    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	    return epoch.AddSeconds(unixTime);
	}
    
    /// <summary>
    /// Returns true if this is a Hexoskin, false otherwise
    /// </summary>
    /// <returns><c>true</c>, if hexo skin was ised, <c>false</c> otherwise.</returns>
    /// <param name="dev">Dev.</param>
    public static bool isHexoSkin(BluetoothDevice dev)
    {
         // Setting up worker depending on sensor
        string manfName = new string(dev.Name.TakeWhile(Char.IsLetter).ToArray());

        if (manfName == "HX")
        {
            return true;
        } else
        {
            return false;
        }
    }

    } // End Class
} // End Namespace
