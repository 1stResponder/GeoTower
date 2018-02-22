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
using Android.Locations;
using Android.Util;
using Android.Gms.Common.Apis;
using Android.Gms.Common;
using Android.Gms.Location;

namespace WatchTower.Droid.Services
{
    [Service]
    public class LocationService : Service, GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener, Android.Gms.Location.ILocationListener
    {
        public event EventHandler<LocationChangedEventArgs> LocationChanged = delegate { };
        public event EventHandler<ProviderDisabledEventArgs> ProviderDisabled = delegate { };
        public event EventHandler<ProviderEnabledEventArgs> ProviderEnabled = delegate { };
        public event EventHandler<StatusChangedEventArgs> StatusChanged = delegate { };

        public LocationService()
        {
        }

        // Set our location manager as the system location service

        readonly string logTag = "LocationService";
        IBinder binder;
        GoogleApiClient apiClient;
        LocationRequest locRequest;

      public override void OnCreate()
      {
          base.OnCreate();
          setUpLocationClientIfNeeded();
          locRequest = new LocationRequest();
          apiClient.Connect();

    }


      // This gets called once, the first time any client bind to the Service
      // and returns an instance of the LocationServiceBinder. All future clients will
      // reuse the same instance of the binder
      public override IBinder OnBind(Intent intent)
      {
          Log.Debug(logTag, "Client now bound to service");
          binder = new WatchTower.Droid.Services.LocationServiceBinder(this);
          return binder;
      }

      protected void buildGoogleApiClient()
      {
          apiClient = new GoogleApiClient.Builder(this, this, this)
          .AddConnectionCallbacks(this)
          .AddOnConnectionFailedListener(this)
          .AddApi(LocationServices.API).Build();
      }

      private void setUpLocationClientIfNeeded()
      {
        if (apiClient == null)
          Log.Debug(logTag, "Building API Client");
          buildGoogleApiClient();
      }

      public async void RequestLocationUpdatesAsync()
      {
          locRequest.SetPriority(100);  // PRIORITY_HIGH_ACCURACY 
          locRequest.SetFastestInterval(500); // update interval in ms
          locRequest.SetInterval(1000);
      if (apiClient.IsConnected)
        {
          Log.Debug(logTag, "Requesting Location Updates");
          await LocationServices.FusedLocationApi.RequestLocationUpdates(apiClient, locRequest, this);

        }
      }


      public override void OnDestroy()
      {

          base.OnDestroy();
          Log.Debug(logTag, "Service has been terminated");

          // Stop getting updates from the location manager:
         
      }

      #region ILocationListener implementation
      // ILocationListener is a way for the Service to subscribe for updates
      // from the System location Service

      public void OnLocationChanged(Android.Locations.Location location)
      {
          this.LocationChanged(this, new LocationChangedEventArgs(location));

          // This should be updating every time we request new location updates
          // both when teh app is in the background, and in the foreground
          Log.Debug(logTag, String.Format("Latitude is {0}", location.Latitude));
          Log.Debug(logTag, String.Format("Longitude is {0}", location.Longitude));
          Log.Debug(logTag, String.Format("Altitude is {0}", location.Altitude));
          Log.Debug(logTag, String.Format("Speed is {0}", location.Speed));
          Log.Debug(logTag, String.Format("Accuracy is {0}", location.Accuracy));
          Log.Debug(logTag, String.Format("Bearing is {0}", location.Bearing));
      }

      public void OnProviderDisabled(string provider)
      {
          this.ProviderDisabled(this, new ProviderDisabledEventArgs(provider));
      }

      public void OnProviderEnabled(string provider)
      {
          this.ProviderEnabled(this, new ProviderEnabledEventArgs(provider));
      }

      public void OnStatusChanged(string provider, Availability status, Bundle extras)
      {
          this.StatusChanged(this, new StatusChangedEventArgs(provider, status, extras));
      }

      public void OnConnectionFailed(ConnectionResult result)
      {
        Log.Error(logTag, "Connection to Google API failure.");
      }

      public void OnConnected(Bundle connectionHint)
      {
        RequestLocationUpdatesAsync();
      }

      public void OnConnectionSuspended(int cause)
      {
        Log.Debug(logTag, "Connection to Google API suspended.");
      }

      #endregion

  }
}
