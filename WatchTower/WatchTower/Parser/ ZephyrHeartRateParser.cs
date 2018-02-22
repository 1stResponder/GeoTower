using System;
using System.Collections.Generic;
using EMS.NIEM.Sensor;

namespace WatchTower
{
	public class ZephyrHeartRateParser : SensorParser
	{
		public  ZephyrHeartRateParser(byte[] data, SensorDetail det) : base(data, det)
		{
		}

		public override SensorDetail getSensorDetail()
		{
			Parse();
			return detail;
		}

		protected override void Parse()
		{
			string temp = BitConverter.ToString(sensorData);

			// Getting the flag field
			int flag = sensorData[0];

			// Getting binary value for flag field
			string binForm = Convert.ToString(flag, 2);
			binForm = reverseString(binForm);

			// If the last bit got left off, assume that it is 0
			if(binForm.Length != 5)
			{
				binForm += "0";
			}

			// Getting bits of the flag field
			char formatFlag = binForm[0];
			bool goodSensorContact = binForm[1] == '1';
			bool sensorContactSupport = binForm[2] == '1';
			bool energyExpendedIncluded = binForm[3] == '1';
			bool rrIncluded = binForm[4] == '1';


			// Getting the heart rate
			int heartRateValue = 0;

			switch (formatFlag)
			{
				case '0':
					// 8 bit, value is one byte
					heartRateValue = sensorData[1];
					break;
				case '1':
					// 16 bit, value is 2 bytes
					string hexVal = "" + sensorData[2] + sensorData[1];
					heartRateValue = getIntFromHexString(hexVal);
					break;
			}

			// Can deal with RR and energy expended later if it is desired



			if(heartRateValue != 0)
			{
				detail.PhysiologicalDetails.HeartRate = heartRateValue;
			}





			/*
			if ((flag & 0x01) == 0)
			{
				// If format is 8 bit, next bit is value
				format = 8;

            } else {
				// If format is 16 bit, next 2 bits are the value.
				format = 16;
            }
			*/


		}




	}
}
