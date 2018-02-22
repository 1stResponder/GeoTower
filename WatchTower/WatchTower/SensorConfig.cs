using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Xml.Linq;

namespace WatchTower
{
	public static class SensorConfig
	{
		private const string SUPPORT_SENSOR_RES_NAME = "WatchTower.SupportedDeviceList.xml";
		private const string SUPPORT_SENSOR_ROOT_NAME = "SupportedDeviceList";
		private const string DEVICE_NAME_TAG = "devicename";
		private static List<string> supportedDeviceList;

		public static void Initialize()
		{
			supportedDeviceList = new List<string>();

			readFiles(SUPPORT_SENSOR_RES_NAME, SUPPORT_SENSOR_ROOT_NAME, DEVICE_NAME_TAG, supportedDeviceList);
		}

		private static void readFiles(string resourceName, string rootname, string itemName, List<string> valueList)
		{
			var assembly = typeof(SensorConfig).GetTypeInfo().Assembly;
			Stream stream = assembly.GetManifestResourceStream(resourceName);

			string content = "";
			using (var reader = new StreamReader(stream))
			{
				content = reader.ReadToEnd();
			}

			// Converting raw text into xml file
			XDocument doc = XDocument.Parse(content);
			XElement root = doc.Element(rootname);

			foreach(XElement e in root.Elements(itemName))
			{
				supportedDeviceList.Add(e.Value);
			}
		}

		/// <summary>
		/// Returns the list of names for supported Devices
		/// </summary>
		/// <returns>A list of the names of supported devices</returns>
		public static List<string> getSupportedDevices()
		{
			if(supportedDeviceList.Count == 0)
			{
				Initialize();
			}

			return supportedDeviceList;
		}



	} // End Class
} // End Namespace
