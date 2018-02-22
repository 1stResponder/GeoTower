using Android.App;
using Android.Util;
using Android.Widget;
using Android.OS;
using Android.Content;
using System;

namespace WatchTower.Droid.Services
{
    public class LESensorServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public bool IsConnected { get; private set; }
        public LESensorServiceBinder Binder { get; set; }

        static readonly string TAG = typeof(LESensorServiceConnection).FullName;
        private MainActivity mainActivity;

        #region initialize

        public LESensorServiceConnection(MainActivity activity)
        {
            IsConnected = false;
            Binder = null;
            mainActivity = activity;
        }

        #endregion


        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as LESensorServiceBinder;
            IsConnected = this.Binder != null;
            Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

            if (IsConnected)
            {
                //mainActivity.timestampMessageTextView.SetText(Resource.String.service_started);
            }
            else
            {
                // mainActivity.timestampMessageTextView.SetText(Resource.String.service_not_connected);
            }

        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
            IsConnected = false;
            Binder = null;
            //mainActivity.timestampMessageTextView.SetText(Resource.String.service_not_connected);
        }



    }

}
