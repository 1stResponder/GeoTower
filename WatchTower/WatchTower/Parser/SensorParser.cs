using System;
using System.Text;
using EMS.NIEM.Sensor;

namespace WatchTower
{
	public abstract class SensorParser
	{
		protected SensorDetail detail;
		protected const int VALUE_ABSENT_FLAG = -1;
		protected byte[] sensorData;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data">Data.</param>
		public SensorParser(byte[] data, SensorDetail det)
		{
			detail = det;
			sensorData = data;
		}

		/// <summary>
		/// Returns the sensor detail object
		/// </summary>
		/// <returns>The sensor detail.</returns>
		public abstract SensorDetail getSensorDetail();

		/// <summary>
		/// Parsing the data
		/// </summary>
		protected abstract void Parse();

		///  <summary>
		/// Bytes the array to string.
		/// </summary>
		/// <returns>The array to string.</returns>
		/// <param name="ba">Byte array</param>
		protected static string ByteArrayToString(byte[] ba)
		{
			string hex = BitConverter.ToString(ba);
			//return hex;
			return hex.Replace("-", "");
		}

		/// <summary>
		/// Strings to byte array.
		/// </summary>
		/// <returns>The to byte array.</returns>
		/// <param name="hex">String data, hexadecimal</param>
		protected static byte[] StringToByteArray(String hex)
		{
			int NumberChars = hex.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}

		/// <summary>
		/// Reverses the two byte string.  Only works for strings representing two bytes,
		/// like A11B.  Return value would be 1BA1
		/// </summary>
		/// <returns>The two byte string.</returns>
		/// <param name="sByteString">S byte string.</param>
		protected string reverseTwoByteString(string sByteString)
		{
			StringBuilder sbReversed = new StringBuilder();
			if (sByteString.Length == 4)
			{
				sbReversed.Append(sByteString.Substring(2, 2));
				sbReversed.Append(sByteString.Substring(0, 2));
			}
			else
				sbReversed.Append(sByteString);

			return sbReversed.ToString();
		}

		protected int getIntFromHexString(string sHex)
		{
			return Convert.ToInt32(sHex, 16);
		}

		/// <summary>
		/// Reverse the order of the characters in the string
		/// </summary>
		/// <returns>The string to be reversed</returns>
		/// <param name="value">The reversed string</param>
		protected string reverseString(string value)
		{
			char[] charArray = value.ToCharArray();
			Array.Reverse(charArray);
			return new string(charArray);
		}
	}
}
