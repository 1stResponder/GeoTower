using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace WatchTower.Droid.Services
{
    //This is our Binder subclass, the LocationServiceBinder
    public class LocationServiceBinder : Binder
    {
        public LocationService Service
        {
            get { return this.service; }
        }
        protected LocationService service;

        public bool IsBound { get; set; }

        // constructor
        public LocationServiceBinder(LocationService service)
        {
            this.service = service;
        }
    }
}
