using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ExternalAccessory;
using Newtonsoft.Json.Linq;

namespace WatchTower.iOS
{
	public class HexoskinManager
	{
		WatchTowerSettings _watchTowerSettings;

		BackgroundWorkerWrapper _bgHexoskin;

		public int HeartRate { get; private set; }
		public string HexoskinName { get { return _watchTowerSettings.HexoskinID; } }
		public bool bConnectedToData { get; private set; }

		const string BASE_URL = "https://s3.amazonaws.com/pscloud-watchtower/";

		EAAccessoryManager _classicalBluetoothManager;

		public HexoskinManager()
		{
			_watchTowerSettings = SingletonManager.WatchTowerSettings;
			_bgHexoskin = new BackgroundWorkerWrapper(GetData, FinishProcessingHexoskinData, _watchTowerSettings.UpdateInterval);

			//for (int i = 0; i < v.ConnectedAccessories.Length; i++)
			//	Debug.WriteLine($"classic bt device name is {v.ConnectedAccessories[i].Name}");
			//StartGettingData();
		}

		public void StartGettingData()
		{
			_bgHexoskin.StartWork(_watchTowerSettings.UpdateInterval);
		}

		public void StopGettingData()
		{
			_bgHexoskin.StopWork();
		}

		void GetData()
		{
			GetHexoskinDataAsync();
		}

		/// <summary>
		/// Retrieves data and sets heart rate for hexoskin, and returns TRUE if the data was retrieved successfully.
		/// </summary>
		/// <returns><c>true</c>, if get data was caned, <c>false</c> otherwise.</returns>
		public bool CanGetData()
		{
			if (!String.IsNullOrEmpty(_watchTowerSettings.HexoskinID))
			{
				string hexoskinID = _watchTowerSettings.HexoskinID;

				// synchronous call
				string sHexoskinJson = HTTPSender.getHexoskinDataSynchronous(BASE_URL, hexoskinID);

				ParseHexoskinJson(sHexoskinJson);
			}

			return bConnectedToData;
		}


		void ParseHexoskinJson(string sHexoskinJson)
		{
			if (!String.IsNullOrEmpty(sHexoskinJson))
			{
				Debug.WriteLine(sHexoskinJson);

				JObject result = JObject.Parse(sHexoskinJson);

				int heartRate = (int)result["heartrate"];
				Debug.WriteLine($"heartrate from hexoskin = {heartRate}");

				HeartRate = heartRate;

				//Debug.WriteLine("setting bConnectedToData to TRUE");
				bConnectedToData = true;
			}
			else
				bConnectedToData = false;
		}


		async Task GetHexoskinDataAsync()
		{
			if (!String.IsNullOrEmpty(_watchTowerSettings.HexoskinID))
			{
				string hexoskinID = _watchTowerSettings.HexoskinID;

				Task<String> hexoskinTask = HTTPSender.getMessageWithResponse(BASE_URL, hexoskinID);

				string sHexoskinJson = await hexoskinTask;

				ParseHexoskinJson(sHexoskinJson);
			}
			else
			{
				bConnectedToData = false;
			}
		}


		void FinishProcessingHexoskinData()
		{

		}
	}
}

