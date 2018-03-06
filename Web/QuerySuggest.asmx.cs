using ClassLibrary;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

namespace Web
{
    /// <summary>
    /// Forms query suggestions.
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
     [System.Web.Script.Services.ScriptService]
    public class QuerySuggest : System.Web.Services.WebService
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static StorageManager sm = new StorageManager(storageAccount);

        private static ListTrie t = new ListTrie();
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        private static string fileLocation;

        public QuerySuggest()
        {

        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> Search(string name)
        {
            List<string> results = QuerySuggest.t.GetSearchResults(name);
            return results;
        }

        private float GetAvailableMBytes()
        {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }

        [WebMethod]
        public string DowloadWiki()
        {
            QuerySuggest.fileLocation = QuerySuggest.sm.GetTitlesContainer();
            return QuerySuggest.fileLocation;
        }

        [WebMethod]
        public string BuildTrieMemory()
        {
            string currentTitle = "";
            int i = 0;
            using (StreamReader sr = new StreamReader(QuerySuggest.fileLocation))
            {
                while (!sr.EndOfStream)
                {
                    if (i % 1000 == 0 && this.GetAvailableMBytes() < 100)
                    {
                        return i.ToString() + " " + sr.ReadLine();
                    }
                    i++;
                    currentTitle = sr.ReadLine();
                    t.Add(currentTitle);
                }
                return currentTitle + this.GetAvailableMBytes();
            }
        }
    }
}