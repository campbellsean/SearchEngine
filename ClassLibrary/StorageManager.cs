using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;
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

        private static int totalCrawled = 0;
        private static int totalIndex = 0;
        private static string[] lastTenCrawked = new string[10];
        private static string[] lastTenErrors = new string[10];


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

        public List<string> GetSearchResults(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            List<string> results = new List<string>();
            List<WebEntity> allWebEntities = new List<WebEntity>();

            string[] wordsInSearch = searchTerm.Split(' ');
            foreach (string word in wordsInSearch)
            {
                TableQuery<WebEntity> wordQueries = new TableQuery<WebEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word));
                var currentWordResults = StorageManager.urlTable.ExecuteQuery(wordQueries);
                foreach (WebEntity entity in currentWordResults)
                {
                    allWebEntities.Add(entity);
                }
            }

            // var sortedWebEntities = allWebEntities.OrderBy(c => c.RowKey.Count());

            var sortedWebEntities = allWebEntities.GroupBy(x => x.RowKey)
                  .OrderByDescending(g => g.Count())
                  .SelectMany(g => g).ToList()
                  .DistinctBy(p => p.RowKey).Take(10)
                  .OrderByDescending(p => p.Date);


            //.ThenBy(n => n.Date); //.ToList();

            foreach (WebEntity entity in sortedWebEntities)
            {
                results.Add("<a href=\"" + entity.Link + "\">" + entity.PageTitle + "</a><br />" + entity.Date + "<br /><br />");
            }
            return results;

            // for now we will try a simple query
            /*
            TableQuery<WebEntity> query = new TableQuery<WebEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, searchTerm));

            // for each word in the searchTerm
            // get execute query results
            var queryResults = StorageManager.urlTable.ExecuteQuery(query);
            
            foreach (WebEntity entity in queryResults)
            {
                results.Add("<a href=\"" + entity.Link + "\">" + entity.PageTitle + "</a><br />" + entity.Date + "<br /><br />");

                // results.Add(entity.PageTitle + " | " + entity.Link);
            }
            */

        }

        public List<string> GetLastTenPerformance()
        {
            List<string> lastTenLinks = new List<string>();
            TableQuery<PerformanceEntity> tableQuery = new TableQuery<PerformanceEntity>().Take(1);
            var links = StorageManager.performanceTable.ExecuteQuery(tableQuery).ToList();

            foreach (PerformanceEntity link in links)
            {
                if (lastTenLinks.Count <= 10)
                {
                    lastTenLinks.Add("Num Crawled: " + link.NumCrawled);
                    lastTenLinks.Add("Num Table Index" + link.NumIndex);
                    lastTenLinks.Add("Last Ten URL: " + link.LastTenCrawled);
                    lastTenLinks.Add("Last Ten Errors: " + link.LastTenErrors);
                }
            }
            return lastTenLinks;
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

        /*
        public void AddErrorToPerformance(string link, string title)
        {
            for (int i = 1; i < 9; i++)
            {
                lastTenErrors[i] = lastTenErrors[i - 1];
            }
            // shift down to clear 1
            lastTenErrors[0] = (title + " | " + link);

            AddPerformanceToTable(link);
            StorageManager.totalCrawled++;
        }
        */

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

            // edit last ten performance
            for (int i = 1; i < 9; i++)
            {
                lastTenCrawked[i] = lastTenCrawked[i - 1];
            }
            lastTenCrawked[0] = (title + " | " + link + " | " + date.ToString());

            // this.AddPerformanceToTable(link);


            if (word.Length > 0)
            {
                StorageManager.totalCrawled++;
                StorageManager.totalIndex++;
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

        // From : https://stackoverflow.com/questions/14859405/azure-table-storage-returns-400-bad-request
        private string ToAzureKeyString(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str
                .Where(c => c != '/'
                            && c != '\\'
                            && c != '#'
                            && c != '/'
                            && c != '?'
                            && c != ';'
                            // apostrophe
                            && !char.IsControl(c)))
                sb.Append(c);
            return sb.ToString();
        }

        /*
        public void AddPerformanceToTable(string link)
        {
            string hashedLink = this.Hash(link);

            string lastTenCrawl = string.Join(",", StorageManager.lastTenCrawked.ToArray());
            string lastTenError = string.Join(",", StorageManager.lastTenErrors.ToArray());

            PerformanceEntity l = new PerformanceEntity(hashedLink)
            {
                LastTenCrawled = lastTenCrawl,
                LastTenErrors = lastTenError,
                NumIndex = StorageManager.totalIndex,
                NumCrawled = StorageManager.totalCrawled
            };
            TableOperation insertOperation = TableOperation.Insert(l);
            StorageManager.performanceTable.Execute(insertOperation);
        }
        */

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

