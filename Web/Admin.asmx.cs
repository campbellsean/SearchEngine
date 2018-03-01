using ClassLibrary;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
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
    // [System.Web.Script.Services.ScriptService]
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
            // sm.AddToXMLQueue("http://www.cnn.com/robots.txt");
            // sm.AddToXMLQueue("http://www.bleacherreport.com/robots.txt");
            return "started";
        }
    }
}
