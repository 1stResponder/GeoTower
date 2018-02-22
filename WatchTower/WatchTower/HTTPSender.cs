using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Net.Http;
using Plugin.Geolocator;
using System.Xml.Linq;
using System.IO;
using Plugin.Geolocator.Abstractions;
using EMS.EDXL.DE;
using EMS.EDXL.DE.v1_0;
using EMS.EDXL.Shared;

using EMS.NIEM.NIEMCommon;
using EMS.NIEM.EMLC;
using EMS.NIEM.Resource;
using EMS.NIEM.Sensor;
using System.Diagnostics;

namespace WatchTower
{
	public static class HTTPSender
	{
		private static string[] UNIT_TYPES = new string[] { "Law Enforcement",
	  "Dignitary/Motorcade Escort", "SWAT CAT 200", "SWAT Mobile 300",
	  "Field Commander", "Crowd Control Team", "Helicopter", "Fixed Wing",
	  "Segway Team" };

		public static async Task SendLocation(double lat, double lon, string url, string name, string agency, string resourceType)
		{
			DEv1_0 de = new DEv1_0();
			Event newEvent = new Event();
			ResourceDetail resource = new ResourceDetail();
			resource.Status = new ResourceStatus();
			//resource.Status.SecondaryStatus = new List<AltStatus>();
			resource.setPrimaryStatus(ResourcePrimaryStatusCodeList.Available);
			resource.OwningOrg = new ResourceOrganization();
			resource.OwningOrg.ResourceID = name;
			// resource.AddSecondaryStatusText("Test", "001");
			newEvent.EventTypeDescriptor = new EventTypeDescriptor();
			newEvent.EventTypeDescriptor.EventTypeCode = resourceType;
			//EventTypeCodeList.ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS_AMBULANCE.ToString();
			de.SenderID = name + "." + agency + "@watchtower";
			de.DistributionID = name;
			de.DateTimeSent = DateTime.Now;
			de.DistributionStatus = StatusValue.Test;
			de.DistributionType = TypeValue.Update;
			de.CombinedConfidentiality = "Unclassified";

			newEvent.Details = new List<EventDetails> { resource };
			newEvent.EventID = de.DistributionID;
			newEvent.EventMessageDateTime = DateTime.Now;
			newEvent.EventValidityDateTimeRange.StartDate = DateTime.Now;
			newEvent.EventValidityDateTimeRange.EndDate = DateTime.Now.AddMinutes(30);

			newEvent.EventLocation = new EventLocation();
			newEvent.EventLocation.LocationCylinder = new LocationCylinder();
			newEvent.EventLocation.LocationCylinder.LocationPoint = new LocationPoint();
			newEvent.EventLocation.LocationCylinder.LocationPoint.Point = new Point();
			newEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lat = lat;
			newEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lon = lon;


			de.ContentObjects = new List<EMS.EDXL.DE.ContentObject>();

			XElement xml = XElement.Parse(newEvent.ToString());
			EMS.EDXL.DE.ContentObject co = new EMS.EDXL.DE.ContentObject();
			co.XMLContent = new XMLContentType();
			co.XMLContent.AddEmbeddedXML(xml);
			de.ContentObjects.Add(co);

			await sendMessage(de, url);
		}

		/// <summary>
		/// Creates the resource detail with the given name
		/// </summary>
		/// <returns>The resource detail.</returns>
		/// <param name="name">Name.</param>
		/// <param name="agency">Agency</param>
		public static ResourceDetail createResourceDetail(string name, string agency)
		{
			ResourceDetail resource = new ResourceDetail();
			resource.Status = new ResourceStatus();
			resource.setPrimaryStatus(ResourcePrimaryStatusCodeList.Available);

			resource.OwningOrg = new ResourceOrganization();
			resource.OwningOrg.ResourceID = name;
			resource.OwningOrg.OrgID = agency;

			return resource;
		}


		/// <summary>
		/// Creates the De object
		/// </summary>
		/// <returns>Status code for the post.  Returns -1 if there was an exception</returns>
		/// <param name="lat">Lat.</param>
		/// <param name="lon">Lon.</param>
		/// <param name="name">Name.</param>
		/// <param name="agency">Agency.</param>
		/// <param name="postURL">Post URL.</param>
		/// <param name="resourceType">Resource type.</param>
		/// <param name="eventDetails">Event details.</param>
		public static async Task sendUpdate(double lat, double lon, string name, string agency, string postURL, string resourceType, List<EventDetails> eventDetails)
		{
			try
			{
				// Creating DE
				DEv1_0 de = new DEv1_0();
				de.ContentObjects = new List<EMS.EDXL.DE.ContentObject>();

				de.DistributionID = name;
				de.DateTimeSent = DateTime.Now;
				de.DistributionStatus = StatusValue.Test;
				de.DistributionType = TypeValue.Update;
				de.CombinedConfidentiality = "Unclassified";

				// Creating Event
				Event newEvent = new Event();
				newEvent.EventTypeDescriptor = new EventTypeDescriptor();
				newEvent.EventLocation = new EventLocation();
				newEvent.EventLocation.LocationCylinder = new LocationCylinder();
				newEvent.EventLocation.LocationCylinder.LocationPoint = new LocationPoint();
				newEvent.EventLocation.LocationCylinder.LocationPoint.Point = new Point();

				newEvent.EventID = de.DistributionID;
				de.SenderID = name + "." + agency + "@watchtower";
				newEvent.EventTypeDescriptor.EventTypeCode = resourceType;
				newEvent.Details = eventDetails;

				newEvent.EventMessageDateTime = DateTime.Now;
				newEvent.EventValidityDateTimeRange.StartDate = DateTime.Now;
				newEvent.EventValidityDateTimeRange.EndDate = DateTime.Now.AddMinutes(30);

				newEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lat = lat;
				newEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lon = lon;

				Debug.WriteLine(newEvent.ToString());

				// Serializing Event to XML
				XElement xml = XElement.Parse(newEvent.ToString());

				// Adding the Event to DE as a content object
				EMS.EDXL.DE.ContentObject co = new EMS.EDXL.DE.ContentObject();
				co.XMLContent = new XMLContentType();
				co.XMLContent.AddEmbeddedXML(xml);
				de.ContentObjects.Add(co);

				await sendMessage(de, postURL);

			}
			catch (Exception e)
			{
				// log
			}
		}



	    /// <summary>
	    /// Sends the location.
	    /// </summary>
	    /// <returns>The location.</returns>
	    /// <param name="lat">Lat.</param>
	    /// <param name="lon">Lon.</param>
	    /// <param name="url">URL.</param>
	    /// <param name="name">Name.</param>
	    /// <param name="agency">Agency.</param>
	    /// <param name="resourceType">Resource type.</param>
	    /// <param name="eventDetails">Event details.</param>
		public static async Task SendLocation(double lat, double lon, string url, string name, string agency, string resourceType, List<EventDetails> eventDetails)
		{
			DEv1_0 de = new DEv1_0();
			Event newEvent = new Event();
			ResourceDetail resource = new ResourceDetail();
			resource.Status = new ResourceStatus();
			//resource.Status.SecondaryStatus = new List<AltStatus>();
			resource.setPrimaryStatus(ResourcePrimaryStatusCodeList.Available);
			resource.OwningOrg = new ResourceOrganization();
			resource.OwningOrg.ResourceID = name;
			// resource.AddSecondaryStatusText("Test", "001");
			newEvent.EventTypeDescriptor = new EventTypeDescriptor();
			newEvent.EventTypeDescriptor.EventTypeCode = resourceType;
			//EventTypeCodeList.ATOM_GRDTRK_EQT_GRDVEH_CVLVEH_EM_EMS_AMBULANCE.ToString();
			de.SenderID = name + "." + agency + "@watchtower";
			de.DistributionID = name;
			de.DateTimeSent = DateTime.Now;
			de.DistributionStatus = StatusValue.Test;
			de.DistributionType = TypeValue.Update;
			de.CombinedConfidentiality = "Unclassified";

			// add resource details at beginning
			eventDetails.Insert(0, resource);
			newEvent.Details = eventDetails;
			newEvent.EventID = de.DistributionID;
			newEvent.EventMessageDateTime = DateTime.Now;
			newEvent.EventValidityDateTimeRange.StartDate = DateTime.Now;
			newEvent.EventValidityDateTimeRange.EndDate = DateTime.Now.AddMinutes(30);

			newEvent.EventLocation = new EventLocation();
			newEvent.EventLocation.LocationCylinder = new LocationCylinder();
			newEvent.EventLocation.LocationCylinder.LocationPoint = new LocationPoint();
			newEvent.EventLocation.LocationCylinder.LocationPoint.Point = new Point();
			newEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lat = lat;
			newEvent.EventLocation.LocationCylinder.LocationPoint.Point.Lon = lon;


			de.ContentObjects = new List<EMS.EDXL.DE.ContentObject>();

			//Debug.WriteLine(newEvent.ToString());

			XElement xml = XElement.Parse(newEvent.ToString());
			EMS.EDXL.DE.ContentObject co = new EMS.EDXL.DE.ContentObject();
			co.XMLContent = new XMLContentType();
			co.XMLContent.AddEmbeddedXML(xml);
			de.ContentObjects.Add(co);

			await sendMessage(de, url);
		}


		/// <summary>
		/// Sends the given de message
		/// </summary>
		/// <param name="de">De.</param>
		/// <param name="url">URL.</param>
		public static async Task sendMessage(DEv1_0 de, string url)
		{
			try
			{
				
				XmlSerializer x = new XmlSerializer(de.GetType());
				XmlWriterSettings xsettings = new XmlWriterSettings();
				xsettings.Indent = true;
				xsettings.OmitXmlDeclaration = true;
				string str;

				using (var stream = new StringWriter())
				using (var writer = XmlWriter.Create(stream, xsettings))
				{
					x.Serialize(writer, de);
					str = stream.ToString();
				}
				byte[] bytes = Encoding.UTF8.GetBytes(str);
				System.IO.MemoryStream ms = new MemoryStream(bytes);
				HttpContent stringContent = new StreamContent(ms);
				stringContent.Headers.Add("charset", "utf-8");
				stringContent.Headers.Add("Content-type", "application/xml");
				HttpClient client = new HttpClient();


				HttpResponseMessage response = await client.PostAsync(
				  url, stringContent);

				Debug.WriteLine($"\n\n\nresponse status code is {response.StatusCode}\n\n\n");
			}
			catch (Exception ex)
			{
				//Message failed to sen
				Debug.WriteLine("failed to send message");
				Debug.WriteLine($"{ex.Message}");
			}
		}

		public static async Task<String> getMessage(string baseurl, string deviceID)
		{
			// by default this is https://s3.amazonaws.com/pscloud-watchtower/
			string fullurl = baseurl + deviceID + ".json";
			HttpClient client = new HttpClient();
			string data = await client.GetStringAsync(fullurl);
			return data;
		}

		/// <summary>
		/// Gets the data JSON for the specified device using the URL.
		/// 
		/// Returns an empty string if other than 200 response is received.
		/// </summary>
		/// <returns>The message with response.</returns>
		/// <param name="baseurl">Baseurl.</param>
		/// <param name="deviceID">Device identifier.</param>
		public static async Task<String> getMessageWithResponse(string baseurl, string deviceID)
		{
			// by default this is https://s3.amazonaws.com/pscloud-watchtower/
			string fullurl = baseurl + deviceID + ".json";

			HttpClient client = new HttpClient();

			HttpResponseMessage response = await client.GetAsync(fullurl);

			string sResponseData = "";

			if (response.StatusCode == System.Net.HttpStatusCode.OK)
				sResponseData = response.Content.ReadAsStringAsync().Result;

			return sResponseData;
		}

		/// <summary>
		/// Gets the hexoskin data.  Note this is a synchronous call.
		/// </summary>
		/// <returns>The hexoskin data.</returns>
		/// <param name="baseurl">Baseurl.</param>
		/// <param name="deviceID">Device identifier.</param>
		public static string getHexoskinDataSynchronous(string baseurl, string deviceID)
		{
						// by default this is https://s3.amazonaws.com/pscloud-watchtower/
			string fullurl = baseurl + deviceID + ".json";

			HttpClient client = new HttpClient();

			var response = client.GetAsync(fullurl).Result;

			string sResponseData = "";

			if (response.IsSuccessStatusCode)
			{
				var responseContent = response.Content;

				sResponseData = responseContent.ReadAsStringAsync().Result;
			}

			return sResponseData;
		}


	} // end class
} // end namespace
