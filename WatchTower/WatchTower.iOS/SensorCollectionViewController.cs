using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace WatchTower.iOS
{
	public class SensorCollectionViewController : UICollectionViewController
	{
		static NSString sensorCellId = new NSString("SensorCell");

		//SensorCellData[] sensorCellData;

		public SensorCollectionViewController()
		{
			CollectionView.RegisterClassForCell(typeof(SensorCell), sensorCellId);

			//sensorCellData = new SensorCellData[5];
		}


		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var sensorCell = (SensorCell)collectionView.DequeueReusableCell(sensorCellId, indexPath);

			//var sensorData = sensorCellData[indexPath.Row];

			//sensorCell.UpdateCell(sensorData.DataString);

			return sensorCell;
		}

		public override void ItemHighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = collectionView.CellForItem(indexPath);
			cell.ContentView.BackgroundColor = UIColor.Yellow;
		}

		public override void ItemUnhighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = collectionView.CellForItem(indexPath);
			cell.ContentView.BackgroundColor = UIColor.White;
		}


		void PopulateSensorCells()
		{
			//SensorCellData a = new SensorCellData();
			//a.SensorName = "MVSS";
			//a.SensorHR = 102;

			//SensorCellData b = new SensorCellData();
			//a.SensorName = "Hexoskin";
			//a.SensorHR = 109;

			//sensorCellData[0] = a;
			//sensorCellData[1] = b;

		}
	}


	//public class SensorCellData
	//{
	//	public string SensorName { get; set; }
	//	public int SensorHR { get; set; }

	//	public string DataString
	//	{
	//		get
	//		{
	//			return $"{SensorName} \nHR = {SensorHR}";
	//		}
	//	}
	//}


	//public class SensorCell : UICollectionViewCell
	//{
		
	//	UILabel _textLabel;

	//	public string DataText { get; set; }
		
	//	[Export("initWithFrame:")]
	//	public SensorCell(CGRect frame) : base (frame)
 //       {
	//		BackgroundView = new UIView { BackgroundColor = UIColor.Orange };

	//		SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Green };

	//		ContentView.Layer.BorderColor = UIColor.LightGray.CGColor;
	//		ContentView.Layer.BorderWidth = 2.0f;
	//		ContentView.BackgroundColor = UIColor.White;
	//		ContentView.Transform = CGAffineTransform.MakeScale(0.8f, 0.8f);

	//		_textLabel = new UILabel();
	//		_textLabel.Text = DataText;


	//		ContentView.AddSubview(_textLabel);
	//	}
	//}
}
