using System;
using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;



namespace WatchTower.Droid.Services
{
    public class PostingServiceBinder : Binder
    {
        public PostingServiceBinder(PostingService service)
        {
            this.Service = service;
        }

        public bool IsBound { get; set; }

        public PostingService Service { get; private set; }


       
    }
}
