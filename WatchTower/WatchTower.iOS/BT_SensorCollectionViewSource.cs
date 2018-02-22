using System;
using System.Collections.Generic;
using System.Drawing;
using CoreBluetooth;
using CoreGraphics;
using Foundation;
using UIKit;

namespace WatchTower.iOS
{
	/// <summary>
	/// Bt sensor collection view source.
	/// 
	/// See https://forums.xamarin.com/discussion/11274/how-to-setup-uicollectionview-datasource
	/// </summary>
	public class BT_SensorCollectionViewSource : UICollectionViewSource
	{
		BluetoothSensorManager _sensorManager = SingletonManager.BluetoothSensorManager;

		UIColor _lightBlue = UIColor.FromRGB(132, 157, 255);


		public BT_SensorCollectionViewSource()
		{
			//Rows = new List<SensorCellData>();
		}

		public BluetoothSensorMonitor GetConnectedMonitor(int index)
		{
			return _sensorManager.ConnectedSensorsSorted[index];
		}

		//public void DisconnectFromPeripherals(List<CBPeripheral> peripherals)
		//{
		//	peripherals.ForEach(p => _sensorManager.DisconnectFromSensor(p.Identifier));
		//}

		public void AddSensorCellData(SensorCellData sensorCellData)
		{

		}

		public override nint NumberOfSections(UICollectionView collectionView)
		{
			return 1;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			return _sensorManager.ConnectedSensorCount;
		}

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = (SensorCell)collectionView.DequeueReusableCell(SensorCell.CellID, indexPath);

			BluetoothSensorMonitor sensorMonitor = _sensorManager.ConnectedSensorsSorted[indexPath.Row];

			//SensorCellData cellData = Rows[indexPath.Row];

			cell.UpdateCell(sensorMonitor.GetConnectedSensorUIString(), sensorMonitor.PresentButDisconnected);

			return cell;
		}

		//public override void ItemHighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		//{
		//	var cell = collectionView.CellForItem(indexPath);
		//	cell.ContentView.BackgroundColor = UIColor.Yellow;
		//}

		public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = collectionView.CellForItem(indexPath);
			cell.ContentView.BackgroundColor = UIColor.White;
		}

		public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = collectionView.CellForItem(indexPath);
			cell.ContentView.BackgroundColor = _lightBlue;
		}
	
	}


	/// <summary>
	/// Temporary class.  
	/// </summary>
	public class SensorCellData : IComparable<SensorCellData>
	{
		static int _order = 0;

		public string SensorName { get; private set; }
		public int SensorHR { get; private set; }
		public int Order { get; private set; }

		public SensorCellData(string sensorName, int sensorHR)
		{
			this.SensorName = sensorName;
			this.SensorHR = sensorHR;
			this.Order = ++_order;
		}

		public string DataString
		{
			get
			{
				return $"{SensorName} \nHR = {SensorHR}";
			}
		}

		public int CompareTo(SensorCellData other)
		{
			return other.Order.CompareTo(this.Order);
		}
	}


	public class SensorCell : UICollectionViewCell
	{
		public static NSString CellID = new NSString("SensorCell");

		UILabel _textLabel;

		[Export("initWithFrame:")]
		public SensorCell(CGRect frame) : base(frame)
		{
			BackgroundView = new UIView { BackgroundColor = UIColor.Orange };

			SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Green };

			ContentView.Layer.BorderColor = UIColor.LightGray.CGColor;
			ContentView.Layer.BorderWidth = 2.0f;
			ContentView.BackgroundColor = UIColor.White;
			ContentView.Transform = CGAffineTransform.MakeScale(0.8f, 0.8f);

			_textLabel = new UILabel();
			_textLabel.Lines = 2;
			_textLabel.BackgroundColor = UIColor.Clear;
			_textLabel.TextColor = UIColor.DarkGray;
			_textLabel.TextAlignment = UITextAlignment.Center;


			ContentView.AddSubview(_textLabel);
		}

		public void UpdateCell(string dataText, bool bPresentButDisconnected)
		{


			//if (bPresentButDisconnected)
			//{
			//	ContentView.BackgroundColor = UIColor.LightGray;
			//	_textLabel.TextColor = UIColor.Black;
			//}
			//else
			//{
			//	ContentView.BackgroundColor = UIColor.White;
			//	_textLabel.TextColor = UIColor.DarkGray;
			//}

			_textLabel.Text = dataText;

			_textLabel.Frame = new RectangleF(0, 0, (float)this.Frame.Width, (float)this.Frame.Height);
		}
	}
}
