using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private static CloudTable errorTable;
        private static string errorTableName = "performanceInformation";

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
            StorageManager.errorTable = tableClient.GetTableReference(StorageManager.errorTableName);
            StorageManager.errorTable.CreateIfNotExists();
        }

        public List<string> GetTenErrors()
        {
            List<string> lastTenLinks = new List<string>();
            TableQuery<PerformanceEntity> tableQuery = new TableQuery<PerformanceEntity>().Take(10);

            var links = StorageManager.errorTable.ExecuteQuery(tableQuery).ToList();

            foreach (URLentity link in links)
            {
                if (lastTenLinks.Count >= 10)
                    break;
                lastTenLinks.Add(link.PageTitle + " | " + link.URL);
            }
            return lastTenLinks;
        }

        public int? TotalCrawled()
        {
            StorageManager.allurlQueue.FetchAttributes();
            int? cachedMessageCount = allurlQueue.ApproximateMessageCount;
            return cachedMessageCount;
        }


        public void InsertPerformance(string link)
        {
            CloudQueueMessage message = new CloudQueueMessage(link);
            StorageManager.allurlQueue.AddMessage(message);
        }

        public CloudQueueMessage GetNextURL()
        {
            CloudQueueMessage message = StorageManager.urlQueue.GetMessage(TimeSpan.FromMinutes(5));
            StorageManager.urlQueue.DeleteMessage(message);
            return message;
        }

        public CloudQueueMessage GetNextXML()
        {
            CloudQueueMessage message = StorageManager.xmlQueue.GetMessage(TimeSpan.FromMinutes(5));
            StorageManager.xmlQueue.DeleteMessage(message);
            return message;
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
            StorageManager.allurlQueue.Clear();
            StorageManager.urlTable.DeleteIfExists();
            StorageManager.errorTable.DeleteIfExists();
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
        public void AddLinkToTableStorage(string link, string title, DateTime date)
        {
            this.InsertPerformance(link);
            string hashedLink = Hash(link);
            URLentity l = new URLentity(hashedLink)
            {
                PageTitle = title,
                Date = date,
                URL = link
            };
            TableOperation insertOperation = TableOperation.Insert(l);
            StorageManager.urlTable.Execute(insertOperation);
        }

        // If no date found
        public void AddLinkToTableStorage(string link, string title)
        {
            this.AddLinkToTableStorage(link, title, new DateTime(2018, 2, 22));
        }

        public void AddErrorToTable(string link, string title)
        {
            this.InsertPerformance(link);
            string hashedLink = Hash(link);
            URLentity l = new URLentity(hashedLink)
            {
                PageTitle = title,
                Date = new DateTime(2018, 2, 21),
                URL = link
            };
            TableOperation insertOperation = TableOperation.Insert(l);
            StorageManager.errorTable.Execute(insertOperation);
        }

        // Referenced Discussion Post: https://canvas.uw.edu/courses/1128815/discussion_topics/4177096
        public List<string> GetLastTenPageTitle()
        {
            List<string> recentLinks = new List<string>();

            string invertedTicks = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            string fiveInvertedTicks = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks + 900000000);


            TableQuery<URLentity> rangeQuery = new TableQuery<URLentity>().Where(
                TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, fiveInvertedTicks),
            TableOperators.And,
            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, invertedTicks)));

            foreach (URLentity link in StorageManager.urlTable.ExecuteQuery(rangeQuery))
            {
                if (recentLinks.Count >= 100)
                    break;
                recentLinks.Add(link.PageTitle + " | " + link.URL + " | " + link.Date);
            }
            recentLinks.Reverse();
            List<string> lastTenLinks = new List<string>();
            foreach (string l in recentLinks)
            {
                if (lastTenLinks.Count >= 10)
                    break;
                lastTenLinks.Add(l);
            }
            return lastTenLinks;
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
            StorageManagerr.startStopQueue.AddMessage(message);
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
}
