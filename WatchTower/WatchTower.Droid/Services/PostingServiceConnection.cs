using Android.App;
using Android.Util;
using Android.Widget;
using Android.OS;
using Android.Content;
using System;

namespace WatchTower.Droid.Services
{
    public class PostingServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(PostingServiceConnection).FullName;

        MainActivity mainActivity;
        public PostingServiceConnection(MainActivity activity)
        {
            IsConnected = false;
            Binder = null;
            mainActivity = activity;
        }

        public bool IsConnected { get; private set; }
        public PostingServiceBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as PostingServiceBinder;
            IsConnected = this.Binder != null;
            Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");


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
