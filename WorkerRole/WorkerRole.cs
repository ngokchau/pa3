using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using HtmlAgilityPack;
using System;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue cmdQueue = queueClient.GetQueueReference("crawlerqueuecommands");
        private static CloudQueue urlQueue = queueClient.GetQueueReference("crawlerqueueurls");
        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable pageTable = tableClient.GetTableReference("crawlertablepageinfo");

        private static string htmlRegex = @"^(http|https):\/\/[a-zA-Z0-9\-\.]+\.cnn\.com\/[a-zA-Z\d\/\.\-]+\/[a-zA-Z\d\-]+(\.cnn\.html|\.html|\.wtvr\.html|[a-zA-Z\d]+|\?[a-zA-Z\=a-zA-Z\&+\=a-zA-z0-9]+)$";
        private static string xmlRegex = @"^(http|https):\/\/[a-zA-Z0-9\-\.]+\.cnn\.com\/[a-zA-Z0-9\/\-]+(\.xml)$";

        private static HashSet<string> urlHashSet = new HashSet<string>();
        private static List<string> disallowedPaths = new List<string>();

        private static int counter = 1;

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole entry point called", "Information");

            urlQueue.CreateIfNotExists();
            pageTable.CreateIfNotExists();

            WebRequest request = WebRequest.Create("http://www.cnn.com/robots.txt");
            StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream());
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                // Load the initial url queue.
                if (Regex.Match(line.Substring(9), xmlRegex).Success && counter < 10000)
                {
                    //LoadHashSet(line.Substring(9));
                }

                // Get the disallowed paths.
                if (line.StartsWith("Disallow: "))
                {
                    disallowedPaths.Add(line.Substring(10));
                }
            }

            //LoadUrlQueue();

            while (true)
            {
                Thread.Sleep(500);
                Trace.TraceInformation("Working", "Information");
                CloudQueueMessage cmd = cmdQueue.PeekMessage();
                if (cmd == null || cmd.AsString == "stop")
                {
                    continue;
                }
                else if (cmd.AsString == "start")
                {
                    CloudQueueMessage url = urlQueue.PeekMessage();
                    InsertIntoTable(url.AsString);
                    if (url != null) {  
                        urlQueue.DeleteMessage(urlQueue.GetMessage());
                    }
                }

            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        private void InsertIntoTable(string url)
        {
            if (IsAllowedPath(url)) {

                try
                {
                    string htmlRegex = @"^(http|https):\/\/[a-zA-Z0-9\-\.]+\.cnn\.com\/([a-zA-Z\d\/\.\-]+|\.cnn\.html|\.html|\.wtvr\.html|[a-zA-Z\d]+\?[a-zA-Z\=a-zA-Z\&+\=a-zA-z0-9]+|)$";
                    WebRequest request = WebRequest.Create(url);
                    Uri uri = new Uri(url);

                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.Load(request.GetResponse().GetResponseStream());

                    // These stuff goes in the table.
                    string title = "";
                    if(htmlDoc.DocumentNode.SelectSingleNode("//head/title") != null) {
                        title = htmlDoc.DocumentNode.SelectSingleNode("//head/title").InnerText;
                    }
                    string date = "";
                    if (htmlDoc.DocumentNode.SelectSingleNode("//head/meta[@http-equiv='last-modified']") != null) { 
                        date = htmlDoc.DocumentNode.SelectSingleNode("//head/meta[@http-equiv='last-modified']").Attributes["content"].Value;
                    }
                    pageTable.CreateIfNotExists();
                    TableOperation insertOperation = TableOperation.InsertOrReplace(new PageInfo(HttpUtility.UrlEncode(url), title, date));
                    pageTable.Execute(insertOperation);

                    // Get all the links in the page.
                    HtmlNodeCollection links = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
                    if (links != null)
                    {
                        foreach (HtmlNode link in links)
                        {
                            string scheme = new Uri(uri, link.Attributes["href"].Value).Scheme.ToString();
                            string host = new Uri(uri, link.Attributes["href"].Value).Host.ToString();
                            string path = new Uri(uri, link.Attributes["href"].Value).PathAndQuery.ToString();

                            string s = scheme + "://" + host + path;

                            if (Regex.Match(s, htmlRegex).Success)
                            {
                                urlQueue.AddMessage(new CloudQueueMessage(s));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // throw e;
                }
            }
        }

        private bool IsAllowedPath(string url)
        {
            foreach (string path in disallowedPaths)
            {
                if (url.Contains(path))
                {
                    return false;
                }
            }
            return true;
        }

        private void LoadHashSet(string xml)
        {
            XmlTextReader xmlReader = new XmlTextReader(WebRequest.Create(xml).GetResponse().GetResponseStream());

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Text)
                { 
                    if (Regex.Match(xmlReader.Value, htmlRegex).Success)
                    {
                        urlHashSet.Add(xmlReader.Value);
                    }
                    else if (Regex.Match(xmlReader.Value, xmlRegex).Success)
                    {
                        LoadHashSet(xmlReader.Value);
                    }
                }
            }
        }

        private void LoadUrlQueue()
        {
            foreach (string url in urlHashSet)
            {
                urlQueue.AddMessage(new CloudQueueMessage(url));
            }
        }
    }
}
