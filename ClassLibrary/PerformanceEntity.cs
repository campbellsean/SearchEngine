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
            // to order by insert
            this.PartitionKey = invertedTicks;
            this.RowKey = url;
        }

        public PerformanceEntity() { }

        public List<string> LastTenCrawled { get; set; }

        public List<string> LastTenErrors { get; set; }
    }
}
