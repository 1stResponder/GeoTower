using System;
using System.Collections.Generic;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace WatchTower.iOS
{
	public sealed class BT_TableViewSource : UITableViewSource//: NSTableViewDataSource
	{
		Dictionary<string, CBPeripheral> _availableSensorDictionary = new Dictionary<string, CBPeripheral>();

		List<BluetoothSensorNameSimple> _sensorNames = new List<BluetoothSensorNameSimple>();

		string CellIdentifier = "TableCell";

		string _sSelectedCell = "";


		public class BluetoothSensorNameSimple : IEquatable<BluetoothSensorNameSimple>
		{
			public string Name { get; set; }
			public bool bIsHexoskinSensor { get; set; }

			public bool Equals(BluetoothSensorNameSimple other)
			{
				return String.Equals(this.Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
			}
		}


		//public List<CBPeripheral> Sensors
		//{
		//	get { return sensors; }
		//}

		public void Add(string peripheralName, CBPeripheral peripheral, bool bIsHexoskinSensor)
		{
			if (!_availableSensorDictionary.ContainsKey(peripheralName))
			{
				_availableSensorDictionary.Add(peripheralName, peripheral); // peripheral may be null for Hexoskin, but that's ok
				_sensorNames.Add(new BluetoothSensorNameSimple
									{
										Name = peripheralName,
										bIsHexoskinSensor = bIsHexoskinSensor
									});
			}

			//if (!sensors.Contains(peripheral))
			//	sensors.Add(peripheral);
		}

		public void Remove(string peripheralName)
		{
			_availableSensorDictionary.Remove(peripheralName);

			_sensorNames.Remove(new BluetoothSensorNameSimple { Name = peripheralName });

			//=> sensors.Remove(peripheral);
		}
			




		public string GetSelectedCell()
		{
			return _sSelectedCell;
		}

		public void ClearMonitorList()
		{
			_availableSensorDictionary.Clear();
			_sensorNames.Clear();

			//sensors.Clear();
		}


		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
			//string item = heartRateMonitors[indexPath.Row].ToString();

			string sName = _sensorNames[indexPath.Row].Name;//(heartRateMonitors[indexPath.Row]?.Name ?? "Unknown Peripheral");

			//---- if there are no cells to reuse, create a new one
			if (cell == null)
			{ cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier); }

			cell.TextLabel.Text = sName;

			return cell;
		}

		public override nint RowsInSection(UITableView tableView, nint section)
		{
			return _availableSensorDictionary.Count;
		}


		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			//base.RowSelected(tableView, indexPath);

			//_sSelectedCell = sHeartRateMonitors[indexPath.Row];
		}

		//public override NSObject GetObjectValue(NSTableView tableView, NSTableColumn tableColumn, nint row)
		//	=> new NSString(heartRateMonitors[(int)row]?.Name ?? "Unknown Peripheral");

		//public override nint GetRowCount(NSTableView tableView)
		//	=> heartRateMonitors.Count;

		public CBPeripheral GetPeripheralAtIndex(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= _availableSensorDictionary.Count)
				return null;

			// get the name of the sensor corresponding to the row index, then use that to index into the dictionary
			return _availableSensorDictionary[_sensorNames[rowIndex].Name];
		}

		public string GetNameAtIndex(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= _availableSensorDictionary.Count)
				return "";

			return _sensorNames[rowIndex].Name;
		}

		public bool IsItemAtIndexHexoskin(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= _availableSensorDictionary.Count)
				throw new Exception("invalid index!");
			else
			{
				return _sensorNames[rowIndex].bIsHexoskinSensor;
			}
		}

		//public CBPeripheral this[int row]
		//{
		//	get
		//	{
		//		if (row < 0 || row >= sensors.Count)
		//			return null;

		//		return sensors[row];
		//	}
		//}
	}
}
