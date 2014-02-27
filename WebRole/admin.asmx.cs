using HtmlAgilityPack;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using WorkerRole;

namespace WebRole
{
    /// <summary>
    /// Summary description for admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    [ScriptService]
    public class admin : System.Web.Services.WebService
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue cmdQueue = queueClient.GetQueueReference("crawlerqueuecommands");
        private static CloudQueue urlQueue = queueClient.GetQueueReference("crawlerqueueurls");

        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable pageTable = tableClient.GetTableReference("crawlertablepageinfo");

        [WebMethod]
        public string GetUrlFromQueue()
        {
            if (urlQueue.PeekMessage() == null)
            {
                return "No Messages in URL Queue";
            }
            return urlQueue.PeekMessage().AsString;
        }

        [WebMethod]
        public void StartCrawler()
        {
            cmdQueue.CreateIfNotExists();
            cmdQueue.Clear();
            cmdQueue.AddMessage(new CloudQueueMessage("start"));
        }

        [WebMethod]
        public void StopCrawler()
        {
            cmdQueue.CreateIfNotExists();
            cmdQueue.Clear();
            cmdQueue.AddMessage(new CloudQueueMessage("stop"));
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string Ram()
        {
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            int ram = Convert.ToInt32(ramCounter.NextValue());

            return new JavaScriptSerializer().Serialize(ram);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string Cpu()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            return new JavaScriptSerializer().Serialize(cpuCounter.NextValue()+"%");
        }

        [WebMethod]
        public void ClearCmdQueue()
        {
            cmdQueue.Clear();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetCmd()
        {
            return new JavaScriptSerializer().Serialize(cmdQueue.PeekMessage().AsString);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetSizeOfUrlQueue()
        {
            urlQueue.FetchAttributesAsync();
            return new JavaScriptSerializer().Serialize(urlQueue.ApproximateMessageCount);
        }

        [WebMethod]
        public string DeleteUrlFromQueue()
        {
            if (urlQueue.GetMessage() == null)
            {
                return "No Messages in URL Queue";
            }
            CloudQueueMessage msg = urlQueue.GetMessage();
            urlQueue.DeleteMessage(msg);
            return msg.AsString;
        }

        [WebMethod]
        public void ClearUrlQueue()
        {
            urlQueue.Clear();
        }

        [WebMethod]
        public void DeletePageTable()
        {
            pageTable.Delete();
        }

        [WebMethod]
        public List<string> GetResultFromTable()
        {
            TableQuery<PageInfo> query = new TableQuery<PageInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "cnn"));
            List<string> results = new List<string>();
            
            foreach (PageInfo page in pageTable.ExecuteQuery(query))
            {
                results.Add(page.title + " " + HttpUtility.UrlDecode(page.url) + " " + page.date);
            }

            return results;
        }

        [WebMethod]
        public string ClearAll()
        {
            pageTable.DeleteIfExists();
            urlQueue.Clear();
            StopCrawler();
            return new JavaScriptSerializer().Serialize(true);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetPageTitle(string url)
        {
            TableQuery<PageInfo> query = new TableQuery<PageInfo>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, HttpUtility.UrlEncode(url)));

            string title = "";
            foreach (PageInfo pageinfo in pageTable.ExecuteQuery(query))
            {
                title = pageinfo.title;
            }

            return new JavaScriptSerializer().Serialize(title);
        }
    }
}
