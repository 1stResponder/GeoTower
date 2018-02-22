using System;
using Android.Text;
using Java.Lang;

namespace WatchTower.Droid.Filter
{
	public class TimeFilter : Java.Lang.Object, IInputFilter
	{
        #region constants

        // Number of seconds a time would have had to differ to show it to the user
        private const double SECOND_THRESHOLD_DEFAULT= 10;

        #endregion

        private double SECOND_THRESHOLD;

        public TimeFilter()
        {
            // Use the default second filter
            SECOND_THRESHOLD = SECOND_THRESHOLD_DEFAULT;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:WatchTower.Droid.Filter.TimeFilter"/> class.
        /// </summary>
        /// <param name="threshold">The time difference between seconds that we want to show.</param>
        public TimeFilter(double threshold)
        {
            SECOND_THRESHOLD = threshold;
        }


        public ICharSequence FilterFormatted(ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
		{
            try
            {
                string value = source.ToString();             

                TimeSpan t = DateTime.Now - DateTime.Parse(value);
                return new Java.Lang.String(getTimeDifString(t));

            } catch (System.Exception e)
            {
                return new Java.Lang.String(AppUtil.GetResourceString(Resource.String.def_value));
            }
		}

        private string getTimeDifString(TimeSpan dif)
		{
            double count = 0;
            string unit = "";

            if (dif.Days > 0)
            {
                count = System.Math.Floor(dif.TotalDays);
                unit = "Day";
			}
            else if (dif.Hours > 0)
			{
                count = System.Math.Floor(dif.TotalHours);
				unit = "Hour";
			}
            else if (dif.Minutes > 0)
			{
                count = System.Math.Floor(dif.TotalMinutes);
				unit = "Minute";
			}
            else if (dif.Seconds > SECOND_THRESHOLD)
			{
                count = System.Math.Floor(dif.TotalSeconds);
				unit = "Second";
			}
            else if (dif.Milliseconds > 0)
			{
				return "Just Now";
            } else
            {
                return AppUtil.GetResourceString(Resource.String.def_value);
            }

			// If its multiple, add a s
			if(count > 1)
            {
                unit += "s";
            } 

            return count + " " + unit + " Ago";
		}
	}
}
