using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    class WebEntity : TableEntity
    {

        public WebEntity(string word, string url)
        {
            this.PartitionKey = word;
            this.RowKey = url;
        }

        public WebEntity() { }

        public DateTime Date { get; set; }

        public string PageTitle { get; set; }

        public string Link { get; set; }

    }
}
