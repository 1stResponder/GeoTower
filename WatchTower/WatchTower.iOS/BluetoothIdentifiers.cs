using System;
using CoreBluetooth;

namespace WatchTower.iOS
{
	public class BluetoothIdentifiers
	{
		public static readonly CBUUID HeartRatePeripheralUUID = CBUUID.FromPartial(0x180D);

		// see - https://www.bluetooth.com/specifications/gatt/services
		public static readonly CBUUID  POCServiceUUID = CBUUID.FromString(BluetoothConstants.MVSS_SERVICE);
		public static readonly CBUUID POCHeartRateMeasurementCharacteristicUUID = CBUUID.FromString(BluetoothConstants.MVSS_CHAR);// maybe 2

		public static readonly CBUUID ZephyrServiceUUID = CBUUID.FromString(BluetoothConstants.HEART_RATE_SERVICE);
		public static readonly CBUUID ZephyrHeartRateCharacteristicUUID = CBUUID.FromString(BluetoothConstants.HEART_RATE_CHAR);// maybe 2
	}
}
