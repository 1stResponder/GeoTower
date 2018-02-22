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
    [IntentFilter(new[] { "wt.sensor.update" })]
    public class SensorBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler<SensorEventArgs> sensorUpdate;
        private static readonly string TAG = typeof(SensorBroadcastReceiver).Name;

        public SensorBroadcastReceiver() : base()
        {

        }

        public override void OnReceive(Context context, Intent intent)
        {
            Bundle intentBundle = intent.Extras;

            if (intentBundle != null)
            {
                // Values
                string address, dataXML = "";
                SensorDetail det = null;


                Log.Debug(TAG, "Update Received");

                address = intentBundle.GetString(AppUtil.ADDRESS_KEY);
                dataXML = intentBundle.GetString(AppUtil.DETAIL_KEY);

                // deserializing the data xml 
                XmlSerializer serializer = new XmlSerializer(typeof(SensorDetail));

                StringReader stringReader = new StringReader(dataXML);
                XmlTextReader xmlReader = new XmlTextReader(stringReader);

                det = (SensorDetail)serializer.Deserialize(xmlReader);
                xmlReader.Close();
                stringReader.Close();

                // Creating event args
                SensorEventArgs arg = new SensorEventArgs(address, det);

                try
                {
                    sensorUpdate(this, arg);
                }
                catch (NullReferenceException e)
                {
                    // This exception occurs when nothign is subscribed to this event, it can be safely ignored                    
                    Log.Debug(TAG, "Nothing is subscribed to the event");
                }
            }
        }
    } // end class

    #region Custom Event args

    public class SensorEventArgs : EventArgs
    {
        private string address;
        private SensorDetail detail;
        private bool isConnected;
        private bool isReconnect;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:WatchTower.Droid.SensorEventArgs"/> class.
        /// </summary>
        /// <param name="add">Address of device.</param>
        /// <param name="det">Sensor detail for this device</param>
        /// <param name="isConnect">If set to <c>true</c> the device is connected</param>
        public SensorEventArgs(string add, SensorDetail det)
        {
            address = add;
            detail = det;
        }

        #region Public fields
        /// <summary>
        /// Holds the associated device address
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
        /// Holds the sensor detail 
        /// </summary>
        /// <value>The detail.</value>
        public SensorDetail Detail
        {
            get
            {
                return detail;
            }
        }
        #endregion

    } // end class

    #endregion
} // End namespace
