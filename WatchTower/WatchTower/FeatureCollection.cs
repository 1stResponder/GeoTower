using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchTower
{
    public class FeatureCollection
    {
        public string type;
        public int totalFeatures;
        public List<Feature> features;
        public CRS crs;

        public static FeatureCollection FromString(string s)
        {
            FeatureCollection collection = JsonConvert.DeserializeObject<FeatureCollection>(s);
            return collection;
        }
    }
}
