using System;
using ExternalAccessory;

namespace WatchTower.iOS
{
	public class HeartRateMonitor_BTClassic
	{
		EAAccessoryManager _accessoryManager;

		EAAccessory _btAccessory;

		public HeartRateMonitor_BTClassic()
		{
			_accessoryManager = EAAccessoryManager.SharedAccessoryManager;
		}


		public void ScanForAccessories()
		{
			var accessories = _accessoryManager.ConnectedAccessories;
			_accessoryManager.ShowBluetoothAccessoryPicker(null, (obj) => DoSomething(obj));

			foreach (var accessory in accessories)
			{
				Console.WriteLine($"Got me an accessory: {accessory.Name}");
			}



		}


		void DoSomething(object obj)
		{
			_btAccessory = (EAAccessory)obj;

			Console.WriteLine($"got accessory {_btAccessory.Name}");
		}
	}
}
