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

namespace WatchTower.Droid
{
    public class ReportsFragment : Fragment
    {


        View myView;

        
     public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            myView = inflater.Inflate(Resource.Layout.reports_layout, container, false);
            return myView;
        }
    }
}
