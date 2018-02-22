using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMS.NIEM.Sensor;
using Newtonsoft.Json;

namespace WatchTower.Parser
{
  class HexoskinParser : SensorParser
  {
    public HexoskinParser(byte[] data, SensorDetail det) : base(data, det)
    {
    }

    double heartRateValue;

    public override SensorDetail getSensorDetail()
    {
      Parse();
      return detail;
    }

    protected override void Parse()
    {
      string dataString =Encoding.UTF8.GetString(sensorData, 0, sensorData.Length);
      Dictionary<string, string> dataJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataString);

      //Get the heart rate
      double.TryParse(dataJson["heartrate"], out heartRateValue);
      int hr = (int)heartRateValue;
      detail.PhysiologicalDetails.HeartRate = hr;




    }
  }
}
