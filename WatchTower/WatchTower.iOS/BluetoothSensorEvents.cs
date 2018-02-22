using System;
using CoreBluetooth;

namespace WatchTower.iOS
{
	/// <summary>
	/// Custom event arg to use for Bluetooth Connection Changed event
	/// </summary>
	public class BluetoothConnectionChangedEventArgs : EventArgs
	{
		public string UpdatedConnectedDevicesString { get; set; }
		public CBPeripheral Peripheral { get; set; }
	}


	/// <summary>
	/// Custom event arg to use for Bluetooth Discovered Peripheral event
	/// </summary>
	public class BluetoothDiscoveredPeripheralEventArgs : EventArgs
	{
		public CBPeripheral Peripheral { get; set; }
		public string PeripheralName { get; set; }
		public bool bIsHexoskinPeripheral { get; set; }
	}
}
