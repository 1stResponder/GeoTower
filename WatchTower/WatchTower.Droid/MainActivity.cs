
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Timers;
using Android.Locations;
using WatchTower.Droid.Services;
using Android.Util;
using Android.Preferences;
using Android.Widget;
using System.Security.Cryptography;
using System.Text;
using Android.Gms.Common.Apis;
using Android.Gms.Common;
using Android.Gms.Location;
using System.Linq;
using Android.Bluetooth;
using Android.Graphics;
using Android.Content.Res;

namespace WatchTower.Droid
{
    [Activity(Label = "WatchTower", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Icon = "@drawable/wt_launch_icon")]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback, ActivityCompat.IOnRequestPermissionsResultCallback, NavigationView.IOnNavigationItemSelectedListener, GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener
    {
        #region Private data members

        GoogleMap MyMap;
        Timer mapTimer;
        BackgroundWorker KMLWorker;
        FeatureCollection features;
        GoogleApiClient apiClient;

        private static LocationBroadcastReceiver locationReceiver;
        private static SensorStateBroadcastReceiver sensorStateReceiver;

        // Service Connections
        PostingServiceConnection postServiceConnection;
        LESensorServiceConnection leSensorServiceConnection;
        
        // Navigation Drawer
        protected NavigationView navigationView;
        protected DrawerLayout drawer;

        // Toggle
        protected Switch updateToggle;
        protected FloatingActionButton Fab2;

        // Fragment
        protected Android.App.FragmentManager fm;
        protected List<Android.App.Fragment> FragmentList;
        protected MapFragment mapFrag;
        protected LocationFragment locFrag;
        protected ReportsFragment repFrag;
        protected SensorFragment senFrag;

        protected TextView errorView;

        private Intent serviceIntent;
        
        // Blueooth
        private static BluetoothAdapter _adpt;

        // Dictonary of addresses and their connection state for this device
        Dictionary<string, bool> _connectedDeviceMap;

        #region Constants
        private static readonly string TAG = typeof(MainActivity).Name;
        private static readonly string LOC_TAG = typeof(MainActivity).Name + ":LocationService";
        private static readonly int REQUEST_LOC = 0;
        private static readonly string LE_SENSOR_WARNING_REMOVEDLAST = AppUtil.GetResourceString(Resource.String.le_sensor_warning_removedLast);
        private static readonly string LE_SENSOR_WARNING_ABRUPT = AppUtil.GetResourceString(Resource.String.service_ended_abruptly);
        
        const string logTag = "WatchTowerLocationService";
        const string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        #endregion

        #endregion


        #region Override Functions


        protected override void OnCreate(Bundle bundle)
        {
            // Set our view from the "main" layout resource
            startPostingService();
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            
            // Initializing Elements 
            Initialize();

            // Show the mapFragment 
           // fm.BeginTransaction().Show(mapFrag);
           
            Android.App.FragmentTransaction t = FragmentManager.BeginTransaction();
            t.Show(mapFrag);
            t.Commit();

            // Checking Permissions
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != (int)Permission.Granted)
            {
                RequestLocationPermission();
            }
            else
            {
                mapFrag.GetMapAsync(this);

                // Starting Location Service
                App.StartLocationService();
            }
        }
        
        /// <summary>
        /// On Start.  Creates the services if neccesarry and binds them. 
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
            apiClient.Connect();
        }

        protected override void OnPause()
        {
            base.OnPause();
            Log.Debug("MainActivity", "Main activity paused.");
            mapTimer.Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            apiClient.Connect();           
            mapTimer.Start();
            
            reloadKML();
        }
        
        protected override void OnDestroy()
        {
            Log.Debug(LOC_TAG, "OnDestroy: Location app is becoming inactive");
            base.OnDestroy();

            // Stop the services
            stopSensorService();
            stopPostingService();
            UnregisterReceiver();
             
            App.StopLocationService();
        }
        
        public override void OnBackPressed()
        {
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

#region default Override

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
        }
        
        protected override void OnStop()
        {
            base.OnStop();
        }

        #endregion

        #region Helper Methods
        
        private void SendOrderedBroadcast(Intent intent)
        {
            Android.App.Application.Context.SendBroadcast(intent, null);
        }
    
        /// <summary>
        /// Registers the Broadcast receivers
        /// </summary>
        private void RegisterReceiver()
        {
           IntentFilter fil;
                    
            // Location Receiver
            locationReceiver = new LocationBroadcastReceiver();
            fil = new IntentFilter(AppUtil.LOCATION_UPDATE_ACTION);
            fil.Priority = 98;
            Android.App.Application.Context.RegisterReceiver(locationReceiver, fil);
            
            fil = new IntentFilter();
            fil.AddAction(AppUtil.SENSOR_CONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_REMOVED_ACTION);
            fil.AddAction(AppUtil.SENSOR_ADDED_ACTION);
            fil.AddAction(AppUtil.SENSOR_DISCONNECT_ACTION);
            fil.AddAction(AppUtil.SENSOR_PAUSE_ACTION);
            fil.Priority = 97;
            Android.App.Application.Context.RegisterReceiver(sensorStateReceiver, fil);  
         }
        
        /// <summary>
        /// Unregisters the Broadcast receivers
        /// </summary>
        private void UnregisterReceiver()
        {
            // Unregistering Receievrs
            Android.App.Application.Context.UnregisterReceiver(locationReceiver);
            Android.App.Application.Context.UnregisterReceiver(sensorStateReceiver);
        }
    
        #endregion

        #endregion
        #region Initialization Methods

        public void Initialize()
        {
            // Setting default if the value was empty, otherwise it's set to it's current value
            AppConfig.Initialize();
            
            // Getting the init shared prefernces for the sensor
            SensorConfig.Initialize();
  
            initFragment();
            initUI();
            initReceiver();
            
            // Bluetooth
            _adpt = BluetoothAdapter.DefaultAdapter;
            
            // Init Dictionary for sensors 
            _connectedDeviceMap = new Dictionary<string, bool>();
            
            // Setting up Map Timer
            mapTimer = new Timer(5000);
            mapTimer.Elapsed += new ElapsedEventHandler(OnTick);

            
            // Setting up LocationService Events
            App.Current.LocationServiceConnected += (object sender, Services.ServiceConnectedEventArgs e) =>
            {
                Log.Debug(LOC_TAG, "ServiceConnected Event Raised");
                // notifies us of location changes from the system
                App.Current.LocationService.LocationChanged += HandleLocationChanged;
                //notifies us of user changes to the location provider (ie the user disables or enables GPS)
                App.Current.LocationService.ProviderDisabled += HandleProviderDisabled;
                App.Current.LocationService.ProviderEnabled += HandleProviderEnabled;
                // notifies us of the changing status of a provider (ie GPS no longer available)
                App.Current.LocationService.StatusChanged += HandleStatusChanged;
                
                
            };
 
            // Initialize Google API
            InitializeGoogleLocationServicesAsync();
            initService();         
        }

        #region Helper Methods

        /// <summary>
        /// Initializes the fragments
        /// </summary>
        private void initFragment()
        {
            // Fragments
            fm = this.FragmentManager;

            mapFrag = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            locFrag = new LocationFragment();
            repFrag = new ReportsFragment();
            senFrag = new SensorFragment();

            FragmentList = new List<Android.App.Fragment>();
            FragmentList.Add(locFrag);
            FragmentList.Add(repFrag);
            FragmentList.Add(senFrag);
            FragmentList.Add(mapFrag);
        }
        
        /// <summary>
        /// Initializes the user interface.
        /// </summary>
        private void initUI()
        {
            // Creating drawer
            drawer = (DrawerLayout)FindViewById(Resource.Id.drawer_layout);
            navigationView = (NavigationView)this.drawer.FindViewById(Resource.Id.nav_view);
            navigationView.Menu.GetItem(0).SetChecked(true);
            
            // Setting up Update toggle
            updateToggle = (Switch)FindViewById(Resource.Id.lu_switch);
            updateToggle.Visibility = ViewStates.Invisible;
            updateToggle.Checked = false;
            updateToggle.CheckedChange += updateToggleClick;  
            
            // Creating navr and action bars
            InitializeNavBar();
            
            // Set up Location Reporting Button
            Fab2 = (FloatingActionButton)FindViewById(Resource.Id.fab2);
            Fab2.SetImageResource(Resource.Drawable.ic_location_ab);
            Fab2.Click += LocationFabClick;
            Fab2.SetVisibility(ViewStates.Visible);

            // Set up error view
            errorView = (TextView)FindViewById(Resource.Id.errorMain);
            errorView.Alpha = .8f;
        }
        
        /// <summary>
        /// Initializes the Broadcast receivers
        /// </summary>
        private void initReceiver()
        {
            IntentFilter fil;
            
            // Location Receiver
            locationReceiver = new LocationBroadcastReceiver();

            // Sensor Receiver
            sensorStateReceiver = new SensorStateBroadcastReceiver();
            sensorStateReceiver.SensorAdded += SensorAdded;
            sensorStateReceiver.SensorRemoved += SensorRemoved;
            sensorStateReceiver.SensorConnect += SensorConnect;
            sensorStateReceiver.SensorDisconnect += SensorDisconnect;
            sensorStateReceiver.SensorReportingPaused += SensorPause;
            
            RegisterReceiver();
        }
        
        /// <summary>
        /// Initializes the services for Watchtower
        /// </summary>
        private void initService()
        {              
            // Sensor Service            
            // populating sensor list with saved sensors
            foreach(string s in AppConfig.SavedSensorDictionary.Keys)
            {
                _connectedDeviceMap.Add(s, true);
            }

            startSensorService();
        }
        

        /// <summary>
        /// Initializes the navigation bar
        /// </summary>
        private void InitializeNavBar()
        {
            // Setting up Navigation Vieww
            navigationView.SetNavigationItemSelectedListener(this);

            // Setting up toolbar
            Android.Support.V7.Widget.Toolbar toolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            // Setting up Navigation Bar Toggles
            Android.Support.V7.App.ActionBarDrawerToggle toggle = new Android.Support.V7.App.ActionBarDrawerToggle(
                  this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();
        }
        
        /// <summary>
        /// Initializes the google location service
        /// </summary>
        private void InitializeGoogleLocationServicesAsync()
        {
            apiClient = new GoogleApiClient.Builder(this, this, this)
                .AddApi(LocationServices.API)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .Build();
            apiClient.Connect();
        }
        #endregion
        #endregion

        #region Callback Methods

        private void LocationFabClick(object sender, EventArgs e)
        {
            // Treating this as if the user clicked the update toggle itsself.
            bool checkedState = updateToggle.Checked;
            updateToggle.Checked = !checkedState;
        }
            
            
        /// <summary>
        /// Callback for the Updatetoggle
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void updateToggleClick(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
            Switch s = (Switch)sender;

            if(s.Checked)
            {
                this.RunOnUiThread(delegate
	            {
                    Android.Content.Res.ColorStateList csl = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.ParseColor("#FF8C00") });
                    Fab2.BackgroundTintList = csl;
                    Toast.MakeText(Android.App.Application.Context, "Location Updates On", ToastLength.Short).Show();
                    AppConfig.PostUpdates = true;
	            });
                
            } else
            {
                this.RunOnUiThread(delegate
                {
                    Android.Content.Res.ColorStateList csl = new Android.Content.Res.ColorStateList(new int[][] { new int[0] }, new int[] { Android.Graphics.Color.ParseColor("#303F9F") });
                    Fab2.BackgroundTintList = csl;
                    Toast.MakeText(Android.App.Application.Context, "Location Updates Off", ToastLength.Short).Show();
                    AppConfig.PostUpdates = false;
                });
            }
		}
        
        #endregion

        #region Interface Functions

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            this.MenuInflater.Inflate(Resource.Menu.main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {

            switch (item.ItemId)
            {
                case Resource.Id.action_settings:
                    var intent = new Intent(this, typeof(SettingsActivity));
                    StartActivity(intent);
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        /// <summary>
        /// IOnMapReadyCallback Interface Function
        /// </summary>
        /// <param name="googleMap">The Map Yo!</param>
        public void OnMapReady(GoogleMap googleMap)
        {

            if (googleMap != null)
            {
                MyMap = googleMap;
                MyMap.MapType = GoogleMap.MapTypeHybrid;
                MyMap.MyLocationEnabled = true;
                MyMap.UiSettings.MapToolbarEnabled = true;
                MyMap.UiSettings.MyLocationButtonEnabled = true;
                MyMap.UiSettings.ZoomGesturesEnabled = true;
                apiClient.Connect();
                Location location = LocationServices.FusedLocationApi.GetLastLocation(apiClient);

                if (location != null)
                {
                    CameraUpdate center = CameraUpdateFactory.NewLatLng(new LatLng(location.Latitude, location.Longitude));
                    CameraUpdate zoom = CameraUpdateFactory.ZoomTo(15);
                    MyMap.MoveCamera(center);
                    MyMap.AnimateCamera(zoom);

                }

                // The GoogleMap object is ready to go.
                mapTimer.Enabled = true;
                mapTimer.Start();

            }
        }

        /// <summary>
        /// IOnRequestPermissionsResultCallback Interface Function
        /// </summary>
        /// <param name="requestCode">Code for the Permission</param>
        /// <param name="permissions">Permissions Strings</param>
        /// <param name="grantResults">Results</param>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == REQUEST_LOC)
            {
                // Received permission result for camera permission.
                //Log.Info(TAG, "Received response for Camera permission request.");

                // Check if the only required permission has been granted
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    // Camera permission has been granted, preview can be displayed
                    //Log.Info(TAG, "CAMERA permission has now been granted. Showing preview.");
                    Snackbar.Make(FindViewById(Resource.Id.content_main), Resource.String.permision_available_loc, Snackbar.LengthShort).Show();
                    mapFrag.GetMapAsync(this);

                    // Starting Location Service
                    App.StartLocationService();
                }
                else
                {
                    //Log.Info(TAG, "CAMERA permission was NOT granted.");
                    Snackbar.Make(FindViewById(Resource.Id.content_main), Resource.String.permissions_not_granted, Snackbar.LengthShort).Show();
                }
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

        /// <summary>
        /// IOnNavigationItemSelectedListener Interface Function
        /// </summary>
        /// <param name="menuItem"></param>
        /// <returns></returns>
        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            // Handle navigation view item clicks here.
            int id = menuItem.ItemId;

            // Variable that sets visibility for the drawers
            int itemID = -1;


            if (id == Resource.Id.nav_map)
            {
                itemID = 0;
                switchtoMap();
            }
            else if (id == Resource.Id.nav_sensors)
            {
                itemID = 2;
                switchtoSensorFragment();
            }
            else if (id == Resource.Id.nav_location)
            {
                itemID = 1;
                switchtoLocationFragment();
            }

            // Setting the View
            if (itemID >= 0)
            {
                navigationView.Menu.GetItem(0).SetChecked(false);
                navigationView.Menu.GetItem(1).SetChecked(false);
                navigationView.Menu.GetItem(2).SetChecked(false);
                navigationView.Menu.GetItem(itemID).SetChecked(true);
            }

            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }
        #endregion


        public void OnTick(object source, ElapsedEventArgs e)
        {
            reloadKML();
        }
        
        public void reloadKML()
        {
            mapTimer.Stop();
            KMLWorker = new BackgroundWorker();
            KMLWorker.RunWorkerCompleted += KMLWorker_RunWorkerCompleted;
            KMLWorker.DoWork += KMLWorker_DoWork;
            KMLWorker.RunWorkerAsync();   
        }

        private void KMLWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (features == null)
            {

                return;
            }
            try
            {
                List<string> assets = new List<string>(this.Resources.Assets.List(""));
                string icon;
                RunOnUiThread(() =>
                {
                    if(MyMap != null)
                    {
                
                        MyMap.Clear();
 
                        foreach (Feature feature in features.features)
                        {
                            icon = feature.properties.iconurl;
                            icon = icon.Substring(icon.LastIndexOf("/") + 1);

                            string baseicon = icon;

                            Bitmap resizedIcon = AppUtil.getIconBitmap(icon);

                            if (resizedIcon != null)
                            {
                                MyMap.AddMarker(new MarkerOptions()
                                    .SetPosition(new LatLng(feature.geometry.coordinates[1], feature.geometry.coordinates[0]))
                                    .SetTitle(feature.properties.title))
                                    .SetIcon(BitmapDescriptorFactory.FromBitmap(resizedIcon));
                            }
                            else
                            {
                                Log.Debug("Features", String.Format("Feature: type: {0} icon: {1} {2} {3}", feature.type, feature.properties.iconurl, feature.properties.description, feature.properties.friendlyname));
                                Log.Debug("IconIssues", String.Format("Icon not present: {0}, {1}, {2}", icon, baseicon, feature.properties.description));

                                MyMap.AddMarker(new MarkerOptions()
                                    .SetPosition(new LatLng(feature.geometry.coordinates[1], feature.geometry.coordinates[0]))
                                    .SetTitle(feature.properties.title));
                            }
                        }
                    }

                    mapTimer.Start();
                });
            }
            catch (Exception ex)
            {
                Log.Debug("KMLWorker_RunCompleted", "Exception occurred when drawing data: " + ex.InnerException);
            }

        }

        private void KMLWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                HttpWebRequest req;
                req = (HttpWebRequest)WebRequest.Create(AppConfig.MapServerURL);
                req.Method = WebRequestMethods.Http.Get;
                req.KeepAlive = true;
                req.Accept = accept;
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                HttpWebResponse webResponse = (HttpWebResponse)req.GetResponse();
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream);
                string s = streamReader.ReadToEnd();
                streamReader.Close();
                responseStream.Close();
                features = FeatureCollection.FromString(s);

                this.RunOnUiThread(delegate
                {
                    errorView.Visibility = ViewStates.Invisible;
                });
            }
            catch (Exception ex)
            {
                this.RunOnUiThread(delegate
                {
                    errorView.Visibility = ViewStates.Visible;
                });
            }
        }


        private void RequestLocationPermission()
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
            {
                Snackbar.Make(FindViewById(Resource.Id.content_main), Resource.String.location_justification, Snackbar.LengthIndefinite).SetAction(Resource.String.ok, new Action<View>(delegate (View obj)
                {
                    ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation }, REQUEST_LOC);
                })).Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation }, REQUEST_LOC);
            }
        }


        #region Android Location Service methods

        ///<summary>
        /// Updates UI with location data
        /// </summary>
        public void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            Location location = e.Location;

            double lat = location.Latitude;
            double lon = location.Longitude;
            LocationManager locationManager = (LocationManager)GetSystemService(Context.LocationService);
            Criteria criteria = new Criteria();
            if (location != null)
            {
               // CameraUpdate center = CameraUpdateFactory.NewLatLng(new LatLng(location.Latitude, location.Longitude));
               // MyMap.MoveCamera(center);

                Intent intent = new Intent();
                intent.SetAction(AppUtil.LOCATION_UPDATE_ACTION);

                Bundle intentBundle = new Bundle();
                intentBundle.PutString(AppUtil.LAT_KEY, location.Latitude.ToString());
                intentBundle.PutString(AppUtil.LON_KEY, location.Longitude.ToString());
                intentBundle.PutString(AppUtil.ALT_KEY, location.Altitude.ToString());
                intentBundle.PutString(AppUtil.ACC_KEY, location.Accuracy.ToString());

                intent.PutExtras(intentBundle);

                SendOrderedBroadcast(intent);
            }
        }



        #endregion


        #region UI methods
        
        private void switchtoLocationFragment()
        {
            updateToggle.Visibility = ViewStates.Visible;
            Fab2.SetVisibility(ViewStates.Invisible);
            
            Android.App.FragmentTransaction t = FragmentManager.BeginTransaction();
            t.Replace(Resource.Id.content_frame, locFrag);
            
            foreach (Android.App.Fragment f in FragmentList)
            {
                if (f != locFrag)
                {
                    t.Hide(f);
                }
            }
            
            t.Show(locFrag);
            t.Commit();
            
            // Switching fragments
           // fm.BeginTransaction().Hide(mapFrag).Commit();
           // fm.BeginTransaction().Replace(Resource.Id.content_frame, locFrag).Commit();     
        }
        
        private void switchtoSensorFragment()
        {
            updateToggle.Visibility = ViewStates.Invisible;
            Fab2.SetVisibility(ViewStates.Invisible);

            Android.App.FragmentTransaction t = FragmentManager.BeginTransaction();
            t.Replace(Resource.Id.content_frame, senFrag);
            
            foreach (Android.App.Fragment f in FragmentList)
            {
                if (f != senFrag)
                {
                    t.Hide(f);
                }
            }
            
            t.Show(senFrag);
            t.Commit();
            
            
	      //  fm.BeginTransaction().Replace(Resource.Id.content_frame, senFrag).Commit();
          //  fm.BeginTransaction().Show(senFrag).Commit();
        }
        
        private void switchtoMap()
        {    
            updateToggle.Visibility = ViewStates.Invisible;
            Fab2.SetVisibility(ViewStates.Visible);
            
            // Removing other fragments
          /*  foreach (Android.App.Fragment f in FragmentList)
            {
                fm.BeginTransaction().Hide(f).Commit();
                fm.BeginTransaction().Remove(f).Commit();
            }*/
            
            Android.App.FragmentTransaction t = FragmentManager.BeginTransaction();
            //t.Replace(Resource.Id.content_frame, mapFrag);
            //  t.AddToBackStack(null);

            foreach (Android.App.Fragment f in FragmentList)
            {
                if (f != mapFrag)
                {
                    t.Hide(f);
                }
            }
          
            t.Show(mapFrag);
            t.Commit();
            
            // Getting the map
            mapFrag.GetMapAsync(this);
            
            // Showing the map fragment
           // fm.BeginTransaction().Show(mapFrag).Commit();
            
            // If location isn't null AND map has been initialized (the location service can start updating before 
            Location location = LocationServices.FusedLocationApi.GetLastLocation(apiClient);
            if (location != null && MyMap != null)
            {
                CameraUpdate center = CameraUpdateFactory.NewLatLng(new LatLng(location.Latitude, location.Longitude));
                MyMap.MoveCamera(center);
            }          
        }



        #endregion

        #region Posting Service
        
        public void startPostingService()
        {
            if (postServiceConnection == null) postServiceConnection = new PostingServiceConnection(this);        
            
            Intent serviceToStart = new Intent(this, typeof(PostingService));
            BindService(serviceToStart, postServiceConnection, Bind.AutoCreate);  
        }
        
        public void stopPostingService()
        {
            if (postServiceConnection != null && AppConfig.PostUpdates) 
            {
                UnbindService(postServiceConnection);
            }
        }
        #endregion

        #region LESensorServiceMethods

        #region Start/Stop

        /// <summary>
        /// Starts the sensor service if it is not already running and there are devices to be connected
        /// </summary>
        private void startSensorService()
        {
            if (leSensorServiceConnection == null) leSensorServiceConnection = new LESensorServiceConnection(this);

            Intent serviceToStart = new Intent(this, typeof(LESensorService));
            BindService(serviceToStart, leSensorServiceConnection, Bind.AutoCreate);
        }
        
        /// <summary>
        /// Stops the sensor service
        /// </summary>
        private void stopSensorService()
        {
            if (leSensorServiceConnection != null)
            {
                 UnbindService(leSensorServiceConnection);           
            }
        }


        #endregion
        #region Devicelist methods      
        
        /// <summary>
        /// Gets the sensor map
        /// </summary>
        /// <returns>The sensor map.</returns>
        public Dictionary<string, bool> getDeviceMap()
        {
            return _connectedDeviceMap;
        }

        #endregion
        #region Callback

        /// <summary>
        /// Handles Sensor Added Event
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void SensorAdded(Object sender, SensorStateEventArgs e)
        {
        
            if (!_connectedDeviceMap.ContainsKey(e.Address))
            {
                Log.Debug(TAG, String.Format("Sensor with address {0} was added",e.Address));
                _connectedDeviceMap.Add(e.Address, true);
            } else
            {
                Log.Debug(TAG, String.Format("Sensor with address {0} is now active",e.Address));
                _connectedDeviceMap[e.Address] = true;
            }

            if(!AppConfig.SavedSensorDictionary.ContainsKey(e.Address))
            {
                AppConfig.addToSavedSensorDictionary(e.Address, e.DeviceName);
            }
            
        }
        
        /// <summary>
        /// Handles Sensor Removed Event
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void SensorRemoved(Object sender, SensorStateEventArgs e)
        {
            if (_connectedDeviceMap.ContainsKey(e.Address))
            {
                Log.Debug(TAG, String.Format("Sensor with address {0} was removed",e.Address));
                _connectedDeviceMap.Remove(e.Address);
            } else
            {
                Log.Warn(TAG, String.Format("Sensor with address {0} was not in the list of connected devices",e.Address));
            }
            
            if(AppConfig.SavedSensorDictionary.ContainsKey(e.Address))
            {
                AppConfig.removeFromSavedSensorDictionary(e.Address);
            }
        }
        
        /// <summary>
        /// Handles Sensor Connect Event
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void SensorConnect(Object sender, SensorStateEventArgs e)
        {
            Log.Debug(TAG, String.Format("Sensor with address {0} is now active",e.Address));
            Toast.MakeText(this, String.Format("{0} Has Connected Successfully", AppConfig.getDeviceName(e.Address)), ToastLength.Short).Show();
            
            if (!_connectedDeviceMap.ContainsKey(e.Address))
            {
                Log.Error(TAG, String.Format("Sensor with address {0} was not in the list of connected devices",e.Address));
            }
                
        }

        private void SensorPause(Object sender, SensorStateEventArgs e)
        {    
        }       
        
        /// <summary>
        /// Handles Sensor Disconnect Event
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        private void SensorDisconnect(Object sender, SensorStateEventArgs e)
        {
            Snackbar.Make(
                    FindViewById(Resource.Id.content_main),
                    String.Format("{0} has become inactive", AppConfig.getDeviceName(e.Address)),
	                Snackbar.LengthLong
                ).Show();    
                
            if (_connectedDeviceMap.ContainsKey(e.Address))
            {
                _connectedDeviceMap[e.Address] = false;
                Log.Debug(TAG, String.Format("Sensor with address {0} is now inactive",e.Address));
                
            } else
            {
                Log.Error(TAG, String.Format("Sensor with address {0} was not in the list of connected devices",e.Address));
            }
        }

        #endregion

        #endregion

        #region Location Handler/Service Callbacks

        public void OnConnectionFailed(ConnectionResult result)
        {
            Log.Info(LOC_TAG, "Connection failed, attempting to reach google play services");
        }

        public void OnConnectionSuspended(int cause)
        {
            Log.Info(LOC_TAG, "Now disconnected from client");
        }

        public void OnConnected(Bundle connectionHint)
        {
            Log.Info(LOC_TAG, "Now connected to the client");
            Location location = LocationServices.FusedLocationApi.GetLastLocation(apiClient);
            
            if (location != null && MyMap != null)
			{
                CameraUpdate center = CameraUpdateFactory.NewLatLngZoom(new LatLng(location.Latitude, location.Longitude), 15);
                MyMap.MoveCamera(center);
				MyMap.AnimateCamera(center);
			} 
        }
        
        public void HandleProviderDisabled(object sender, ProviderDisabledEventArgs e)
        {
            Log.Debug(LOC_TAG, "Location provider disabled event raised");
        }

        public void HandleProviderEnabled(object sender, ProviderEnabledEventArgs e)
        {
            Log.Debug(LOC_TAG, "Location provider enabled event raised");
        }

        public void HandleStatusChanged(object sender, StatusChangedEventArgs e)
        {
            Log.Debug(LOC_TAG, "Location status changed, event raised");
        }

        #endregion
        

    }




}
