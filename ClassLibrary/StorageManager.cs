using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class StorageManager
    {
        private static CloudQueue xmlQueue;
        private static string xmlQueueName = "xmlqueue";

        private static CloudQueue urlQueue;
        private static string urlQueueName = "urlqueue";

        private static CloudQueue startStopQueue;
        private static string startStopQueueName = "startstopqueue";

        private static CloudTable urlTable;
        private static string urlTableName = "urlInformation";

        private static CloudTable performanceTable;
        private static string performanceTableName = "performanceInformation";

        public StorageManager(CloudStorageAccount storageAccount)
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            StorageManager.xmlQueue = queueClient.GetQueueReference(StorageManager.xmlQueueName);
            StorageManager.xmlQueue.CreateIfNotExists();

            queueClient = storageAccount.CreateCloudQueueClient();
            StorageManager.urlQueue = queueClient.GetQueueReference(StorageManager.urlQueueName);
            StorageManager.urlQueue.CreateIfNotExists();

            queueClient = storageAccount.CreateCloudQueueClient();
            StorageManager.startStopQueue = queueClient.GetQueueReference(StorageManager.startStopQueueName);
            StorageManager.startStopQueue.CreateIfNotExists();

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            StorageManager.urlTable = tableClient.GetTableReference(StorageManager.urlTableName);
            StorageManager.urlTable.CreateIfNotExists();

            tableClient = storageAccount.CreateCloudTableClient();
            StorageManager.performanceTable = tableClient.GetTableReference(StorageManager.performanceTableName);
            StorageManager.performanceTable.CreateIfNotExists();
        }

        public void GetTenErrors()
        {
            List<string> lastTenLinks = new List<string>();
            TableQuery<PerformanceEntity> tableQuery = new TableQuery<PerformanceEntity>().Take(10);

            var links = StorageManager.performanceTable.ExecuteQuery(tableQuery).ToList();
            /*
            foreach (PerformanceEntity link in links)
            {
                if (lastTenLinks.Count >= 10)
                    break;
                lastTenLinks.Add(link.PageTitle + " | " + link.URL);
            }
            return lastTenLinks;
            */
            // return links;
        }


        public CloudQueueMessage GetNextURL()
        {
            CloudQueueMessage message = StorageManager.urlQueue.GetMessage(TimeSpan.FromMinutes(5));
            // StorageManager.urlQueue.DeleteMessage(message);
            return message;
        }

        public void DeleteURLMessage(CloudQueueMessage message)
        {
            StorageManager.urlQueue.DeleteMessage(message);
        }

        public CloudQueueMessage GetNextXML()
        {
            CloudQueueMessage message = StorageManager.xmlQueue.GetMessage(TimeSpan.FromMinutes(5));
            // StorageManager.xmlQueue.DeleteMessage(message);
            return message;
        }

        public void DeleteXMLMessage(CloudQueueMessage message)
        {
            StorageManager.xmlQueue.DeleteMessage(message);
        }

        public CloudQueueMessage GetStartStopMessage()
        {
            CloudQueueMessage message = StorageManager.startStopQueue.GetMessage(TimeSpan.FromMinutes(5));
            StorageManager.startStopQueue.DeleteMessage(message);
            return message;
        }

        public void DeleteAll()
        {
            StorageManager.startStopQueue.Clear();
            StorageManager.urlQueue.Clear();
            StorageManager.xmlQueue.Clear();
            StorageManager.urlTable.DeleteIfExists();
            StorageManager.performanceTable.DeleteIfExists();
        }

        public void AddToURLQueue(string messageToAdd)
        {
            // Checks for well formed URI
            if ((Uri.IsWellFormedUriString(messageToAdd, UriKind.Absolute)))
            {
                CloudQueueMessage message = new CloudQueueMessage(messageToAdd);
                StorageManager.urlQueue.AddMessage(message);
            }
        }

        public void AddToXMLQueue(string messageToAdd)
        {
            CloudQueueMessage message = new CloudQueueMessage(messageToAdd);
            StorageManager.xmlQueue.AddMessage(message);
        }

        // Adds link to Table Storage
        // I think this will need to take in multiple values
        public void AddLinkToTableStorage(string link, string word, string title, DateTime date)
        {
            if (word.Length > 0)
            {
                WebEntity w = new WebEntity(this.ToAzureKeyString(word), this.Hash(link))
                {
                    PageTitle = title,
                    Date = date,
                    Link = link
                };
                TableOperation insertOperation = TableOperation.Insert(w);
                try
                {
                    StorageManager.urlTable.Execute(insertOperation);
                }
                catch
                {
                    // except = e;
                    // log these errors in the error table
                }
            }
        }

        // If no date found then use 02/22/2018
        public void AddLinkToTableStorage(string link, string title, string word)
        {
            this.AddLinkToTableStorage(link, word, title, new DateTime(2018, 2, 22));
        }

        private string ToAzureKeyString(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str
                .Where(c => c != '/'
                            && c != '\\'
                            && c != '#'
                            && c != '/'
                            && c != '?'
                            && !char.IsControl(c)))
                sb.Append(c);
            return sb.ToString();
        }

        public void AddErrorToTable(string link, string title)
        {
            /*
             * to be determined
            string hashedLink = Hash(link);
            URLentity l = new URLentity(hashedLink)
            {
                PageTitle = title,
                Date = new DateTime(2018, 2, 21),
                URL = link
            };
            TableOperation insertOperation = TableOperation.Insert(l);
            StorageManager.performanceTable.Execute(insertOperation);
            */
        }

        public int? GetSizeOfURLQueue()
        {
            StorageManager.urlQueue.FetchAttributes();
            int? cachedMessageCount = urlQueue.ApproximateMessageCount;
            return cachedMessageCount;
        }

        public int? GetSizeOfXMLQueue()
        {
            StorageManager.xmlQueue.FetchAttributes();
            int? cachedMessageCount = xmlQueue.ApproximateMessageCount;
            return cachedMessageCount;
        }

        public int? GetSizeOfStartStopQueue()
        {
            StorageManager.startStopQueue.FetchAttributes();
            int? cachedMessageCount = startStopQueue.ApproximateMessageCount;
            return cachedMessageCount;
        }

        public string AddtoStartStopQueue(string messageToAdd)
        {
            CloudQueueMessage message = new CloudQueueMessage(messageToAdd);
            StorageManager.startStopQueue.AddMessage(message);
            return messageToAdd;
        }

        // Taken from Discussion Forum Post by: Owen Albert Flannigan
        private string Hash(string link)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(link);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }
    }
}

