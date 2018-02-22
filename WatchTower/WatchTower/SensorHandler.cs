using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using EMS.EDXL.DE;
using EMS.EDXL.DE.v1_0;
using EMS.NIEM.EMLC;
using EMS.NIEM.NIEMCommon;
using EMS.NIEM.Resource;
using EMS.NIEM.Sensor;
using WatchTower.Parser;

namespace WatchTower
{
    public class SensorHandler
    {

        #region Note

		// The following keys are expected for Device Details:
		/* 
		    BluetoothConstants.FW_REV
			BluetoothConstants.HW_REV
			BluetoothConstants.SW_REV
			BluetoothConstants.SERIAL_NUMBER
			BluetoothConstants.MODEL_NUMBER  
            BluetoothConstants.DEVICE_NAME 
		*/
        // The only one which is required is the Device name

        #endregion

        #region private members

        private SensorDetail _currentDetail = null;
        private Dictionary<string, string> _deviceDetail;

        private string _manName, _userID;
        #endregion
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="T:WatchTower.SensorHandler"/> class.
        /// </summary>
        /// <param name="deviceDet">Device detail dictionary</param>
        public SensorHandler(Dictionary<string, string> deviceDet, string userID)
        {
            _userID = userID;
            _deviceDetail = deviceDet;
            resetData();
        }
        
        #endregion

        #region public data members
        /// <summary>
        /// The sensor detail object for this handler
        /// </summary>
        /// <value>The detail.</value>
        public SensorDetail Detail
        {
            get
            {
                return _currentDetail;
            }
        }
        
        
        public string xmlDetail
        {
            get
            {
                return toXML();
            }
        }
            
        
        #endregion

        #region public methods

        /// <summary>
        /// Updates the sensorDetail object with the given byte data
        /// </summary>
        /// <param name="data">Data.</param>
        public void updateData(byte[] data)
        {
            SensorParser parser = GetParser(data);

            try
            {
                _currentDetail = parser.getSensorDetail();
            }
            catch (Exception e)
            {
                // An invalid tag was found in the data, ignore this round of values
                Debug.WriteLine("Invalid data.  Not updating the detail object");
            }
        }

        /// <summary>
        /// Resets the data in the sensorDetail object
        /// </summary>
        public void resetData()
        {

            _currentDetail = new SensorDetail();
            _currentDetail.DeviceDetails = new DeviceDetails();
            _currentDetail.PowerDetails = new PowerDetails();
            _currentDetail.PhysiologicalDetails = new PhysiologicalSensorDetails();
            _currentDetail.EnvironmentalDetails = new EnvironmentalSensorDetails();

            // Setting the device details and main fields for the sensor detail object 
            setDeviceDetails();
        }

        #endregion
        #region Helper Methods
        
        /// <summary>
        /// Sets the device details based on the device details map
        /// </summary>
        private void setDeviceDetails()
        {

            string key = BluetoothConstants.FW_REV;
            if (_deviceDetail.ContainsKey(key)) _currentDetail.DeviceDetails.FirmwareRevision = _deviceDetail[key];
            
            key = BluetoothConstants.HW_REV;
			if(_deviceDetail.ContainsKey(key)) _currentDetail.DeviceDetails.HardwareRevision = _deviceDetail[key];
			
			key = BluetoothConstants.SW_REV;
			if(_deviceDetail.ContainsKey(key)) _currentDetail.DeviceDetails.SoftwareRevision = _deviceDetail[key];
			
			key = BluetoothConstants.SERIAL_NUMBER;
			if(_deviceDetail.ContainsKey(key)) _currentDetail.DeviceDetails.SerialNumber = _deviceDetail[key];
			
			key = BluetoothConstants.MODEL_NUMBER;
			if(_deviceDetail.ContainsKey(key)) _currentDetail.DeviceDetails.ModelNumber = _deviceDetail[key];

            // Parsing numbers from the device name in order to get the manf name
            _manName = "";
            string _deviceName = _deviceDetail[BluetoothConstants.DEVICE_NAME];

            foreach (char x in _deviceName)
            {
                if (Char.IsLetter(x))
                {
                    _manName = _manName + x;
                }
                else
                {
                    break;
                }
            }

            // Setting the device manf
            setDeviceManf();

            // Setting the main fields
            _currentDetail.ID = _userID + "_" + _manName + "_sensor";
            _currentDetail.Status = SensorStatusCodeList.Normal;
        }

        /// <summary>
        /// Gets the parser.  Parser may have been created previously, in which case
        /// this call just updates the data associated with the parser.
        /// </summary>
        /// <returns>The parser.</returns>
        /// <param name="data">Data.</param>
        private SensorParser GetParser(byte[] data)
        {
            switch (_manName)
            {
                case "MVSS":
                    return new POCParser(data, _currentDetail);
                    break;
                case "HX":
                    return new HexoskinParser(data, _currentDetail);
                    break;
                case "Zephyr":
                    return new ZephyrHeartRateParser(data, _currentDetail);
                    break;
                default:
                    // not supported device, should not of made it this far
                    Debug.WriteLine("Not a supported device");
                    throw new ArgumentException(_manName + " sensor is not a supported device.");
            }

            return null;
        }

        /// <summary>
        /// Sets the device manf.  Based on the device name
        /// </summary>
		private void setDeviceManf()
        {
            switch (_manName)
            {
                case "MVSS":
                    _currentDetail.DeviceDetails.ManufacturerName = "POC";
                    break;
                case "HX":
                     _currentDetail.DeviceDetails.ManufacturerName = "HX";
                    break;
                case "Zephyr":
                    _currentDetail.DeviceDetails.ManufacturerName = "Zephyr";
                    break;
            }
        }

        /// <summary>
        /// Serialzes the sensor detail object
        /// </summary>
        /// <returns>The xml.</returns>
        protected string toXML()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(Detail.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, Detail);
                return textWriter.ToString();
            }
        }

        #endregion
    }
}
