using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    class PerformanceEntity : TableEntity
    {
        public PerformanceEntity(string url)
        {
            string invertedTicks = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.PartitionKey = invertedTicks;
            this.RowKey = url;
        }

        public PerformanceEntity() { }

        public string LastTenCrawled { get; set; }

        public string LastTenErrors { get; set; }

        public int NumCrawled { get; set; }

        public int NumIndex { get; set; }
    }
}
