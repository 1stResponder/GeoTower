// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace WatchTower.iOS
{
    [Register ("SensorViewController")]
    partial class BluetoothSensorViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton AddDeviceButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton DisconnectButton { get; set; }

        [Action ("AddDeviceUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void AddDeviceUpInside (UIKit.UIButton sender);

        [Action ("DisconnectUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void DisconnectUpInside (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (AddDeviceButton != null) {
                AddDeviceButton.Dispose ();
                AddDeviceButton = null;
            }

            if (DisconnectButton != null) {
                DisconnectButton.Dispose ();
                DisconnectButton = null;
            }
        }
    }
}
