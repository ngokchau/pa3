using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WorkerRole
{
    class PageInfo : TableEntity
    {
        public string url { get; set; }
        public string title { get; set; }
        public string date { get; set; }

        public PageInfo(string url, string title, string date)
        {
            this.PartitionKey = "cnn";
            this.RowKey = url;

            this.url = url;
            this.title = title;
            this.date = date;
        }

        public PageInfo() { }
    }
}
