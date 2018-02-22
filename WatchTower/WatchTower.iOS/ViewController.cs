using System;

using UIKit;

namespace WatchTower.iOS
{
	public partial class ViewController : UIViewController
	{
public ViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.
			//Button.AccessibilityIdentifier = "myButton";
			//Button.TouchUpInside += delegate {
				//var title = string.Format ("{0} clicks!", count++);
				//Button.SetTitle (title, UIControlState.Normal);
			//};
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}

