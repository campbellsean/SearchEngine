using ClassLibrary;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Services;

namespace Web
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
     [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static StorageManager sm = new StorageManager(storageAccount);
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        [WebMethod]
        public string StartCrawling()
        {
            sm.AddtoStartStopQueue("start");
            sm.AddToXMLQueue("http://www.bleacherreport.com/robots.txt");
            sm.AddToXMLQueue("http://www.cnn.com/robots.txt");
            return "started";
        }

        [WebMethod]
        public List<string> GetResults(string search)
        {
            return sm.GetSearchResults(search);
        }

        [WebMethod]
        public string StopCrawling()
        {
            sm.AddtoStartStopQueue("stop");
            Thread.Sleep(5000);
            return "stopped";
        }

        [WebMethod]
        public List<string> PerformanceUpdate()
        {
            return sm.GetLastTenPerformance();
        }

        [WebMethod]
        public float GetAvailableMBytes()
        {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }

        [WebMethod]
        public float GetCPU()
        {
            float cpuUsage = cpuCounter.NextValue();
            Thread.Sleep(50);
            cpuUsage = cpuCounter.NextValue();
            return cpuUsage;
        }

        [WebMethod]
        public string GetState()
        {
            if (sm.GetSizeOfXMLQueue() != 0)
            {
                return "Loading (still loading XML's)";

            }
            else if (sm.GetSizeOfURLQueue() != 0)
            {
                return "Crawling (URL's still in queue)";
            }
            return "Idle (No XML's or URL's in Queue, waiting for start message)";
        }

        [WebMethod]
        public string ClearIndex()
        {
            this.StopCrawling();
            Thread.Sleep(9000);
            sm.DeleteAll();
            Thread.Sleep(18000);
            sm = new StorageManager(Admin.storageAccount);
            return "Please Wait Up to Ten Minutes before recrawling";
        }
    }
}
