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
    [Register ("MapViewController")]
    partial class MapViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIView MappyMcMapView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITabBarItem MapTabBarItem { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (MappyMcMapView != null) {
                MappyMcMapView.Dispose ();
                MappyMcMapView = null;
            }

            if (MapTabBarItem != null) {
                MapTabBarItem.Dispose ();
                MapTabBarItem = null;
            }
        }
    }
}
