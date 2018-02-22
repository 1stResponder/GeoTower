using System;
using System.Collections.Generic;
using System.Linq;

namespace WatchTower
{
	public static class POC_Constants
	{
		public const string START_TAG= "3C-C3";
		public const string REQ_TYPE = "00";
		public const long INTERVAL_SEND = 30 * 1000; // In milliseconds
		public const long INTERVAL_BETWEEN_READ = 30 * 1000; // In milliseconds
		public const long SCAN_TIMEOUT = 30 * 1000; // In milliseconds, for how long le scan should run
		public const long CONNECT_TIMEOUT = 60 * 1000; // In milliseconds
		public const long READ_TIME = 5 * 1000; // In milliseconds
		public const int TAG_SIZE = 2; // in bytes

		public const string EVENT_TYPE = "ATOM_GRDTRK_EQT_SENSOR";

		public const int TAG_LENGTH = 4; // number of characters

		// UUIDs
		public const string POC_CCCD_UUID = "00002902-0000-1000-8000-00805f9b34fb";
		public const string POC_SERVICE_UUID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
		public const string POC_NOTIFY_CHAR_UUID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";
		public const string POC_WRITE_CHAR_UUID = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";

		// Tag List
		/*
		public const string HEART_RATE_TAG = "0x01";
		public const string SKIN_TEMPERATURE_TAG = "0x02";
		public const string RESPIRATION_RATE_TAG = "0x03";
		public const string SP02_TAG = "0x04";
		public const string PSI_TAG = "0x05";
		public const string ENVIRONMENTAL_TEMPERATURE_TAG = "0x65";
		public const string ENVIRONMENTAL_PRESSURE_TAG = "0x66";
		public const string ENVIRONMENTAL_HUMIDITY_TAG = "0x67";
		public const string AXIS_ACCELEROMETER_TAG = "0xC9";
		public const string PPG_SIGNAL_TAG = "0x1388";*/

		public const string HEART_RATE_TAG = "0100";
		public const string SKIN_TEMPERATURE_TAG = "0200";
		public const string RESPIRATION_RATE_TAG = "0300";
		public const string SP02_TAG = "0400";
		public const string PSI_TAG = "0500";
		public const string ENVIRONMENTAL_TEMPERATURE_TAG = "6500";
		public const string ENVIRONMENTAL_PRESSURE_TAG = "6600";
		public const string ENVIRONMENTAL_HUMIDITY_TAG = "6700";
		public const string AXIS_ACCELEROMETER_TAG = "C900";
		public const string PPG_TAG = "8813";

		public const string KEY_RESOURCE_SELECTED= "resource_selected";
		public const string KEY_SENDER_ID = "send_id";
		public const string KEY_DISTRIBUTION_ID = "distribution_id";
		public const string KEY_POST_URL = "post_url";


		// This should eventually come from a config file
		public static readonly Dictionary<string, string> TagMap = new Dictionary<string, string>
		{
			{HEART_RATE_TAG,"1,Heart Rate"},
			{SKIN_TEMPERATURE_TAG,"2,Skin Temperature"},
			{RESPIRATION_RATE_TAG,"1,Respiration Rate"},
			{SP02_TAG,"1,SP02"},
			{PSI_TAG,"1,PSI"},
			{ENVIRONMENTAL_TEMPERATURE_TAG,"2,Environmental Temperature"},
			{ENVIRONMENTAL_PRESSURE_TAG,"1,Environmental Pressure"},
			{ENVIRONMENTAL_HUMIDITY_TAG,"1,Environmental Humidity"},
			{AXIS_ACCELEROMETER_TAG,"6,Axis Accelerometer"},
			{PPG_TAG,"14,PPG Signal"}
		};

		/// <summary>
		/// Gets the tag data
		/// </summary>
		/// <returns>If found, the tag data.  Otherwise, null</returns>
		/// <param name="tag">Tag</param>
		public static TagData getTagData(string tag)
		{
			// **** Not fully done yet *****

			int size = Int32.Parse((TagMap[tag].Split(',')[0]));
			string type = TagMap[tag].Split(',')[1];

			TagData t = new TagData(tag, type, size);
			return t;
		}

		public static readonly Dictionary<string, int> TagLengthMap = new Dictionary<string, int>
		{
			{HEART_RATE_TAG, 1 * 2}, // * 2 since that's how many string chars will be used for each 4 bits (half a byte)
			{SKIN_TEMPERATURE_TAG, 2 * 2},
			{RESPIRATION_RATE_TAG, 1 * 2},
			{SP02_TAG, 1 * 2},
			{PSI_TAG, 1 * 2},
			{ENVIRONMENTAL_TEMPERATURE_TAG, 2 * 2},
			{ENVIRONMENTAL_PRESSURE_TAG, 1 * 2},
			{ENVIRONMENTAL_HUMIDITY_TAG, 1 * 2},
			{AXIS_ACCELEROMETER_TAG, 6 * 2},
			{PPG_TAG, 14 * 2}
		};


		// Below work in progress
		public static int convertLSFToMSB(string byteSTR)
		{
			byteSTR = byteSTR.Replace("0x", "");
			int a = Int32.Parse(byteSTR, System.Globalization.NumberStyles.HexNumber);

			return a;
		}

		// Below work in progress
		public static byte[] StringToByteArray(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}


	}

	public struct TagData
	{
		public TagData(string tagV, string typeV, int sizeV)
		{
			Tag = tagV;
			Type = typeV;
			Size = sizeV;

		}

		public string Tag;
		public string Type;
		public int Size;
	}




}
