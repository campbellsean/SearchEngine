using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("Worker is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Worker has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Worker is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Worker has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
               ConfigurationManager.AppSettings["StorageConnectionString"]);
            StorageManager sm = new StorageManager(storageAccount);
            URLManager manager = new URLManager(sm);

            while (!cancellationToken.IsCancellationRequested)
            {

                if (sm.GetSizeOfStartStopQueue() != 0)
                {
                    string message = sm.GetStartStopMessage().AsString;

                    if (message.Equals("start"))
                    {
                        while (sm.GetSizeOfStartStopQueue() == 0)
                        {
                            while (sm.GetSizeOfXMLQueue() != 0 && sm.GetSizeOfStartStopQueue() == 0)
                            {
                                Thread.Sleep(50);
                                CloudQueueMessage queueMessage = sm.GetNextXML();
                                manager.ParseXML(queueMessage.AsString);
                                // Delete after processing
                                sm.DeleteXMLMessage(queueMessage);
                            }

                            while (sm.GetSizeOfURLQueue() > 20 && sm.GetSizeOfStartStopQueue() == 0) // && sm.GetSizeOfXMLQueue() == 0)
                            {
                                // Increments of 20 to reduce time wasted calling size of queue
                                for (int i = 0; i < 20; i++)
                                {
                                    Thread.Sleep(50);
                                    CloudQueueMessage queueMessage = sm.GetNextURL();
                                    manager.ParseHTML(queueMessage.AsString);
                                    // Delete after processing
                                    sm.DeleteURLMessage(queueMessage);
                                }
                                Thread.Sleep(50);
                            }

                        }
                    }

                    if (message.Equals("stop"))
                    {
                        Thread.Sleep(5000);
                        while (sm.GetSizeOfStartStopQueue() == 0)
                        {
                            Thread.Sleep(5000);
                        }
                    }
                }
                Thread.Sleep(50);
                await Task.Delay(1000);
            }
        }
    }
}
