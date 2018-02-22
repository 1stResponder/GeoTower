using System;
using System.Timers;
using CoreBluetooth;
using CoreFoundation;
using Foundation;
using UIKit;

namespace WatchTower.iOS
{
	public partial class ReportsViewController : UIViewController
	{
		MySimpleCBCentralManagerDelegate myDel;


		public ReportsViewController() : base("ReportsViewController", null)
		{
		}

		public ReportsViewController(IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			//myDel = new MySimpleCBCentralManagerDelegate();
			//var myMgr = new CBCentralManager(myDel, DispatchQueue.CurrentQueue);

			//myMgr.ConnectedPeripheral += (s, e) =>
			//{
			//	CBPeripheral activePeripheral = e.Peripheral;
			//	System.Console.WriteLine("Connected to " + activePeripheral.Name);


			//	if (activePeripheral.Delegate == null)
			//	{
			//		activePeripheral.Delegate = new SimplePeripheralDelegate();
			//		//Begins asynchronous discovery of services
			//		activePeripheral.DiscoverServices();
			//	}
			//};
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}


		public class SimplePeripheralDelegate : CBPeripheralDelegate
		{
			public override void DiscoveredService(CBPeripheral peripheral, NSError error)
			{
				System.Console.WriteLine("Discovered a service");
				foreach (var service in peripheral.Services)
				{
					Console.WriteLine(service.ToString());
					peripheral.DiscoverCharacteristics(service);
				}
			}

			public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError error)
			{
				System.Console.WriteLine("Discovered characteristics of " + peripheral);
				foreach (var c in service.Characteristics)
				{
					Console.WriteLine(c.ToString());
					peripheral.ReadValue(c);
				}
			}

			//public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error)
			//{
			//	Console.WriteLine("Value of characteristic " + descriptor.Characteristic + " is " + descriptor.Value);
			//}

			//public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
			//{
			//	Console.WriteLine("Value of characteristic " + characteristic.ToString() + " is " + characteristic.Value);
			//}
		}



		public class MySimpleCBCentralManagerDelegate : CBCentralManagerDelegate
		{
			Timer _timer;
			CBCentralManager _mgr;


			override public void UpdatedState(CBCentralManager mgr)
			{
				_mgr = mgr;
				if (mgr.State == CBCentralManagerState.PoweredOn)
				{
					//Passing in null scans for all peripherals. Peripherals can be targeted by using CBUIIDs
					CBUUID[] cbuuids = null;
					mgr.ScanForPeripherals(cbuuids); //Initiates async calls of DiscoveredPeripheral
													 //Timeout after 30 seconds
					_timer = new Timer(30 * 1000);
					_timer.Elapsed += OnTick;//(sender, e) => mgr.StopScan();
					_timer.Start();
				}
				else
				{
					//Invalid state -- Bluetooth powered down, unavailable, etc.
					System.Console.WriteLine("Bluetooth is not available");
				}
			}

			private void OnTick(object source, ElapsedEventArgs e)
			{
				_timer.Stop();
				_mgr = null;
				_timer.Close();

			} 

			public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
			{
				Console.WriteLine("Discovered {0}, data {1}, RSSI {2}", peripheral.Identifier, advertisementData, RSSI);

				//Connect to peripheral, triggering call to ConnectedPeripheral event handled above 
				_mgr.ConnectPeripheral(peripheral);
			}
		}
	}
}

