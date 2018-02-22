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
    [Register ("LocationViewController")]
    partial class LocationViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LastSentLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LatLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch LocationSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITabBarItem LocationTabBar { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel LonLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel ReportLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel VersionLabel { get; set; }

        [Action ("ReportingEnabledChanged:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ReportingEnabledChanged (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (LastSentLabel != null) {
                LastSentLabel.Dispose ();
                LastSentLabel = null;
            }

            if (LatLabel != null) {
                LatLabel.Dispose ();
                LatLabel = null;
            }

            if (LocationSwitch != null) {
                LocationSwitch.Dispose ();
                LocationSwitch = null;
            }

            if (LocationTabBar != null) {
                LocationTabBar.Dispose ();
                LocationTabBar = null;
            }

            if (LonLabel != null) {
                LonLabel.Dispose ();
                LonLabel = null;
            }

            if (ReportLabel != null) {
                ReportLabel.Dispose ();
                ReportLabel = null;
            }

            if (VersionLabel != null) {
                VersionLabel.Dispose ();
                VersionLabel = null;
            }
        }
    }
}
