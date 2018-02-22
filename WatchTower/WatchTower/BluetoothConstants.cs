using System;
namespace WatchTower
{
    public static class BluetoothConstants
    {
        // Zephyr Heart Rate Montior, uses standard UUID's
        public const string HEART_RATE_SERVICE = "0000180d-0000-1000-8000-00805f9b34fb";
        public const string HEART_RATE_CHAR = "00002a37-0000-1000-8000-00805f9b34fb";
        public const string CCD_UUID = "00002902-0000-1000-8000-00805f9b34fb";

        // Device Info uuid
        public const string DEVICE_INFO_SERVICE = "0000180A-0000-1000-8000-00805f9b34fb";
        public const string DEVICE_MODELNUM = "00002A24-0000-1000-8000-00805f9b34fb";
        public const string DEVICE_SERIALNUM = "00002A25-0000-1000-8000-00805f9b34fb";
        public const string DEVICE_FIRMWARE_REV = "00002A26-0000-1000-8000-00805f9b34fb";
        public const string DEVICE_HARDWARE_REV = "00002A27-0000-1000-8000-00805f9b34fb";
        public const string DEVICE_SOFTWARE_REV = "00002A28-0000-1000-8000-00805f9b34fb";
        public const string ZEPHYR_DEVICE_MANF = "Zephyr";

        // MVSS POC patch
        public const string MVSS_SERVICE = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        public const string MVSS_CHAR = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        // Device Details
        public const string MODEL_NUMBER = "model_number";
        public const string SERIAL_NUMBER = "serial_number";
        public const string FW_REV = "fw_rev";
        public const string HW_REV = "hw_rev";
        public const string SW_REV = "sw_rev";
        public const string DEVICE_NAME = "device_name";

        public const double LE_TIMEOUT = 1000 * 20;

    }

}
