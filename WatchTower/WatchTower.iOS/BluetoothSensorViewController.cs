using Foundation;
using System;
using UIKit;
using System.Drawing;
using CoreGraphics;
using CoreBluetooth;
using System.Collections.Generic;
using ExternalAccessory;
using System.Linq;
using System.Text;

namespace WatchTower.iOS
{
	public partial class BluetoothSensorViewController : UIViewController
    {
		UICollectionView _connectedDevicesCollectionView;
		BT_SensorCollectionViewSource _sensorCollectionSource;
		BluetoothSensorManager _sensorManager = SingletonManager.BluetoothSensorManager;
		BackgroundWorkerWrapper _bgUIRefresh;

		const int REFRESH_INTERVAL = 5 * 1000;

		NSObject _notificationHandleEnterForeground;
		NSObject _notificationHandleEnterBackground;

		UIColor _lightGray = UIColor.FromRGB(216, 215, 218);

        public BluetoothSensorViewController (IntPtr handle) : base (handle)
        {
			
        }


		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			var collectionViewBounds = new CGRect();
			collectionViewBounds.Location = new CGPoint(20, 170);

			var collectionBoundWidth = UIScreen.MainScreen.Bounds.Width - 30;
			collectionViewBounds.Size = new CGSize(collectionBoundWidth, UIScreen.MainScreen.Bounds.Height - 240);


			float cellWidth = (float)(collectionBoundWidth) - 20;

					   var layout = new UICollectionViewFlowLayout
			{
				SectionInset = new UIEdgeInsets(20, 5, 5, 5),
				MinimumInteritemSpacing = 1,
				MinimumLineSpacing = 5,
				ItemSize = new SizeF(cellWidth, 50)
			};


			_connectedDevicesCollectionView = new UICollectionView(collectionViewBounds, layout);
			_connectedDevicesCollectionView.ContentSize = View.Frame.Size;
			_connectedDevicesCollectionView.BackgroundColor = _lightGray;

			_sensorCollectionSource = new BT_SensorCollectionViewSource();
			_connectedDevicesCollectionView.RegisterClassForCell(typeof(SensorCell), SensorCell.CellID);

			_connectedDevicesCollectionView.ShowsHorizontalScrollIndicator = false;
			_connectedDevicesCollectionView.Source = _sensorCollectionSource;

			_connectedDevicesCollectionView.ReloadData();

			/* Set up background worker.  Reload data at specified interval - this should refresh the list of connected devices and the displayed data.
			 * Note that this is an expensive operation.  However, it only occurs when the app is in the foreground and this view is visible.*/
			_bgUIRefresh = new BackgroundWorkerWrapper(DoUIRefreshWork, CompleteUIRefresh, REFRESH_INTERVAL);


			_notificationHandleEnterForeground = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, HandleAppWillEnterForeground);
			_notificationHandleEnterBackground = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, HandleAppDidEnterBackground);

			_sensorManager.SensorConnectionsChanged += OnSensorConnectionsChanged;

			View.AddSubview(_connectedDevicesCollectionView);
		}


		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);

			_connectedDevicesCollectionView.ReloadData();

			_bgUIRefresh.StartWork(REFRESH_INTERVAL);
		}


		public override void ViewWillDisappear(bool animated)
		{
			base.ViewWillDisappear(animated);

			_bgUIRefresh.StopWork();
		}

		void HandleAppWillEnterForeground(NSNotification notification)
		{
			if (this.IsVisible())
				_bgUIRefresh.StartWork(REFRESH_INTERVAL);
		}

		/// <summary>
		/// Handles the app did enter background.  Disables UI updates
		/// </summary>
		/// <param name="notification">Notification.</param>
		void HandleAppDidEnterBackground(NSNotification notification)
		{
			_bgUIRefresh.StopWork();
		}


		void OnSensorConnectionsChanged(object sender, BluetoothConnectionChangedEventArgs e)
		{
			DoUIRefreshWork();
		}


		bool IsVisible()
		{
			return this.IsViewLoaded
					   && this.View.Window != null;
		}

		void DoUIRefreshWork()
		{
			Console.WriteLine("ui refresh called!");

									// sensors may have been disconnected while this view wasn't being shown.  Check to see
			//_sensorManager.VerifySensorsConnectedAndRemoveIfNot(manager);

			InvokeOnMainThread(() =>
				{
				_connectedDevicesCollectionView.ReloadData();
				});
		}

		void CompleteUIRefresh()
		{

		}

		List<BluetoothSensorMonitor>  GetMonitorsForSelectedPeripherals()
		{
			List<BluetoothSensorMonitor>  bluetoothMonitors = new List<BluetoothSensorMonitor>();

			var selectedItems = _connectedDevicesCollectionView.GetIndexPathsForSelectedItems();

			foreach (var selectedItem in selectedItems)
			{
				BluetoothSensorMonitor sensorMonitor = _sensorCollectionSource.GetConnectedMonitor(selectedItem.Row);

				bluetoothMonitors.Add(sensorMonitor);

				//if (sensorMonitor.bIsHexoskinMonitor)
				//{

				//}
				//else
				//{
				//	peripherals.Add(sensorMonitor.Peripheral);
				//}
			}

			return bluetoothMonitors;
		}

partial void AddDeviceUpInside(UIButton sender)
		{
			

		}


partial void DisconnectUpInside(UIButton sender)
		{
						EAAccessoryManager manager = EAAccessoryManager.SharedAccessoryManager;

			//manager.ShowBluetoothAccessoryPicker(null, (obj) => HandleAccessoryPickerResult(obj));//Console.WriteLine($"object is null is {obj == null}"));

			//CBUUID[] x = { };
			//_cbCentralManager.RetrievePeripherals(x);

			//NSUuid[] nsuuids = { };
			//CBUUID[] cbuuids = { };

			//CBCentralManager _cbCentralManager = new CBCentralManager();

			//// get list of peripherals from manager, using the array we just created as a lookup
			//CBPeripheral[] peripherals = _cbCentralManager.RetrievePeripheralsWithIdentifiers(nsuuids);

			//Console.WriteLine($"count is {peripherals.Length}");



			//var allaccessorries = manager.ConnectedAccessories.ToList();

			//StringBuilder sb = new StringBuilder();

			//if (allaccessorries.Count > 0)
			//{
			//	foreach (var accessory in allaccessorries)
			//	{
			//		sb.Append(accessory.Name);
			//	}
			//}
			//else
			//	sb.AppendLine("no accessories found");

			//Console.WriteLine(sb);



			foreach (var sensorMonitor in GetMonitorsForSelectedPeripherals())
			{
				_sensorManager.DisconnectFromSensor(sensorMonitor.ID);
					
			}


			//GetSelectedPeripherals().ForEach(p => _sensorManager.DisconnectFromSensor(p.Identifier));
			_connectedDevicesCollectionView.ReloadData();
		}

		void HandleAccessoryPickerResult(object result)
		{
			//Console.WriteLine($"object is null is {result == null}");

			//if (result != null)
			//{
			//	NSError errors = (NSError)result;

			//	Console.WriteLine(errors.
			//}


			//EAAccessoryManager manager = EAAccessoryManager.SharedAccessoryManager;

			//			var allaccessorries = manager.ConnectedAccessories.ToList();

			//StringBuilder sb = new StringBuilder();

			//if (allaccessorries.Count > 0)
			//{
			//	foreach (var accessory in allaccessorries)
			//	{
			//		sb.Append(accessory.Name);
			//	}
			//}
			//else
			//	sb.AppendLine("no accessories found");

			//Console.WriteLine(sb);
		}
	}
}
