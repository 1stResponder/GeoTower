using System;
using System.Diagnostics;
using System.Linq;
using EMS.NIEM.Sensor;

namespace WatchTower
{
	public class POCParser: SensorParser
	{
		public POCParser(byte[] data, SensorDetail det) : base(data, det)
		{ 
		}

		/// <summary>
		/// Returns the Sensor detail which was parsed from the data
		/// and cached from the given Sensor detail
		/// </summary>
		/// <returns>The sensor detail.</returns>
		public override SensorDetail getSensorDetail()
		{
			Parse();
			return detail;
		}

		/// <summary>
		/// Parses the data associated with this object 
		/// </summary>
		protected override void Parse()
		{
			if (isValid(sensorData))
			{
				byte[] tagDat = sensorData.Skip(3).ToArray();
				string tagTe = ByteArrayToString(tagDat);

				int index = 0;
				string sCurrentTag, sCurrentValue;
				int currentValueLength;

				/* Invariant - at beginning of loop, index is at starting point of next tag.
				 We can't look in data content for mapping value because the value associated with a tag
				 may contain a tag value.  So 0x02 may be a tag.  It may also be a data value associated with
				 the same or another tag.  If we search for the first index of that within data, we don't know if we're getting
				 the tag or a value. */
				while (index<tagTe.Length)
				{
					// Get the next tag from the data string
					sCurrentTag = tagTe.Substring(index, POC_Constants.TAG_LENGTH);

					// if that tag exists in the map, get the length of associated data
					if (POC_Constants.TagLengthMap.TryGetValue(sCurrentTag, out currentValueLength))
					{
						// now get the data associated with the tag
						sCurrentValue = tagTe.Substring(index + POC_Constants.TAG_LENGTH, currentValueLength);
						setValueReversed(sCurrentTag, sCurrentValue);
					}
					else // tag not in dictionary
					{
						// this situation would screw up all further processing because we don't know how many bytes used for
						// the associated value
						sCurrentValue = String.Empty;
						throw new ArgumentException("Tag value not in dictionary!");
					}

					// Advance index by length of tag + length of associated value
					index += POC_Constants.TAG_LENGTH + currentValueLength;

				}
			}
		}

		/// <summary>
		/// Sets the value for the SensorDetails object based on the tag that comes in
		/// </summary>
		/// <param name="tag">Tag</param>
		/// <param name="value">Value for the tag</param>
		private void setValueReversed(string tag, string value)
		{
			// If the value is all 0's, ignore it.
			string temp = value.Replace("0", "");
			Debug.WriteLine("Tag: " + tag + "Value: " + value);

			// If the resulting value is not null or empty (aka not all 0s)
			if (!String.IsNullOrWhiteSpace(temp))
			{
				switch (tag)
				{
					case POC_Constants.HEART_RATE_TAG:
						//Heart Rate
						detail.PhysiologicalDetails.HeartRate = getIntFromHexString(value);
						Debug.WriteLine("Heart Rate: " + value);
						break;
					case POC_Constants.SKIN_TEMPERATURE_TAG:
						//Skin Temp
						detail.PhysiologicalDetails.SkinTemperature = getIntFromHexString(value);
						Debug.WriteLine("Skin Temp: " + value);
						break;
					case POC_Constants.RESPIRATION_RATE_TAG:
						//Resp
						detail.PhysiologicalDetails.RespirationRate = getIntFromHexString(value);
						Debug.WriteLine("Resp Rate: " + value);
						break;
					case POC_Constants.SP02_TAG:
						//SP02
						detail.PhysiologicalDetails.SPO2 = getIntFromHexString(value);
						Debug.WriteLine("Sp02: " + value);
						break;
					case POC_Constants.PSI_TAG:
						//PSI
						detail.PhysiologicalDetails.PSI = getIntFromHexString(value);
						Debug.WriteLine("PSI: " + value);
						break;
					case POC_Constants.ENVIRONMENTAL_TEMPERATURE_TAG:
						//Enviromental Temperature
						detail.EnvironmentalDetails.Temperature = getIntFromHexString(value);
						Debug.WriteLine("Env Temp: " + value);
						break;
					case POC_Constants.ENVIRONMENTAL_HUMIDITY_TAG:
						//Humidity
						detail.EnvironmentalDetails.Humidity = getIntFromHexString(value);
						Debug.WriteLine("Env Hum: " + value);
						break;
					case POC_Constants.AXIS_ACCELEROMETER_TAG:
						//Accelomater, x , y, and then z
						//detail.LocationDetails.XAxisAcceleration = parseXAccel(value);
						//detail.LocationDetails.YAxisAcceleration = parseYAccel(value);
						break;
					case POC_Constants.PPG_TAG:
						//PPG Signal
						// Not used by our app
						break;
					default:
						// error
						break;
				}
			}
		}

		/// <summary>
		/// Parse the string for the acceleration of the Y Axis
		/// </summary>
		/// <returns>The Y axis acceleration</returns>
		/// <param name="value">Data String</param>
		private int parseYAccel(string value)
		{
			return parseAccel(value, 1);
		}

		/// <summary>
		/// Parse the string for the acceleration of the c Axis
		/// </summary>
		/// <returns>The X axis accelration.</returns>
		/// <param name="value">Data String</param>
		private int parseXAccel(string value)
		{
			return parseAccel(value, 0);
		}

		/// <summary>
		/// Parses the string for acceleration for the given axis
		/// </summary>
		/// <returns>The accel.</returns>
		/// <param name="value">Data String</param>
		/// <param name="index">Axis. X is 1, Y is 2, Z is 3</param>
		private int parseAccel(string value, int index)
		{
			string cord = value.Split('-')[index];
			return 0;
		}

		/// <summary>
		/// Checks if the given byte array is valid.  This means that its not null or empty and has 
		/// a valid start and command tag
		/// </summary>
		/// <returns><c>true</c>, if valid was ised, <c>false</c> otherwise.</returns>
		/// <param name="data">Data.</param>
		private static bool isValid(byte[] data)
		{
			if (data == null || data.Count() <= 0)
			{
				return false;
			}

			byte[] startTag = data.Take(2).ToArray();
			byte[] command = data.Skip(2).Take(1).ToArray();

			if (BitConverter.ToString(startTag) != POC_Constants.START_TAG)
			{
				return false;
			}

			if (BitConverter.ToString(command) != POC_Constants.REQ_TYPE)
			{
				return false;
			}

			return true;
		}

	} // End class
} // End namespace
