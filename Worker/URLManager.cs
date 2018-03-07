using ClassLibrary;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Worker
{
    class URLManager
    {
        private static HashSet<string> seenLinks = new HashSet<string>();
        private static HashSet<string> seenXMLs = new HashSet<string>();
        private static List<string> notAllowed = new List<string>();
        private StorageManager sm;
        
        public URLManager(StorageManager s)
        {
            sm = s;
        }

        public URLManager() { }

        public void ParseXML(string xml)
        {
            List<string> addToURLQueue = new List<string>();
            List<string> addToXMLQueue = new List<string>();

            if (xml.Contains("robots.txt"))
            {
                this.parseRobots(xml);
            }
            else if (xml.Contains("xml") && !URLManager.seenXMLs.Contains(xml))
            {
                URLManager.seenXMLs.Add(xml);

                WebResponse webResponse = WebRequest.Create(xml).GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                String response = sr.ReadToEnd().Trim();

                // Encode the XML string in a UTF-8 byte array
                byte[] encodedString = Encoding.UTF8.GetBytes(response);

                // Put the byte array into a stream and rewind it to the beginning
                MemoryStream ms = new MemoryStream(encodedString);
                ms.Flush();
                ms.Position = 0;

                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(ms);

                var locationNodes = xdoc.GetElementsByTagName("loc");

                for (int i = 0; i < locationNodes.Count; i++)
                {
                    string xmlToAdd = locationNodes[i].InnerXml;

                    if (xmlToAdd.Contains("xml"))
                    {
                        // BleacherReport -> Ignore non nba related links
                        if (xml.Contains("bleacherreport"))
                        {
                            if (xml.Contains("nba"))
                            {
                                addToXMLQueue.Add(xmlToAdd);
                            }
                        }
                        else if (ValidXML(xmlToAdd))
                        {
                            addToXMLQueue.Add(xmlToAdd);
                        }
                    }
                    else if (!URLManager.seenLinks.Contains(xmlToAdd))
                    {
                        addToURLQueue.Add(xmlToAdd);
                    }
                }
                this.AddToXMLQueue(addToXMLQueue);
                this.AddToURLQueue(addToURLQueue);
            }
        }

        public void ParseHTML(string link)
        {
            //HtmlWeb web = new HtmlWeb();
            var web = new HtmlWeb()
            {
                PreRequest = request =>
                {
                    // Make any changes to the request object that will be used.
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    return true;
                }
            };

            WebClient w = new WebClient();
            WebException except = new WebException();

            try
            {
                string s = w.DownloadString(link);



                var htmlDoc = web.Load(link);
                var node = htmlDoc.DocumentNode.Descendants("title").FirstOrDefault();

                string title = "No Title";
                if (node != null)
                {
                    title = node.InnerText;
                }

                if (title.Equals("Error"))
                {
                    sm.AddErrorToPerformance(link, title);
                }
                else
                {
                    // Get Date if possible
                    HtmlNode mdnode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='pubdate']");
                    if (mdnode != null)
                    {
                        HtmlAttribute desc;
                        desc = mdnode.Attributes["content"];
                        string date = desc.Value;
                        string[] wordsInTitle = title.Split(' ');

                        if (date.Length > 8)
                        {
                            DateTime d = this.StringToDateTime(date);
                            // Go word by word and insert into the table
                            for (int i = 0; i < wordsInTitle.Length; i++)
                            {
                                // To LowerCase
                                string toAdd = wordsInTitle[i].ToLower();
                                sm.AddLinkToTableStorage(link, toAdd, title, d);
                            }
                        }
                        else
                        {
                            // Go word by word and insert into the table
                            for (int i = 0; i < wordsInTitle.Length; i++)
                            {
                                // ToLowerCase
                                string toAdd = wordsInTitle[i].ToLower();
                                sm.AddLinkToTableStorage(link, toAdd, title);
                            }
                        }
                    }

                    sm.AddLinkToPerformance(link, title);
                    // Extracting Links:

                    List<string> linksOnPage = this.ExtractAllAHrefTags(htmlDoc);
                    List<string> addToURLQueue = this.ExamineLinksOnPage(linksOnPage);
                    this.AddToURLQueue(addToURLQueue);
                }
            }
            catch (WebException e)
            {
                except = e;
                sm.AddErrorToPerformance(e.ToString(), "Catched Error");
            }
        }

        private List<string> ExamineLinksOnPage(List<string> linksOnPage)
        {
            List<string> acceptableLinks = new List<string>();
            // Remove other links or add, break into seperate private method
            foreach (string v in linksOnPage)
            {
                string urlToAdd = "";
                // make check for Bleacher and CNN
                if (v.Contains(".com"))
                {
                    if (v.Contains("cnn.com") || v.Contains("bleacherreport.com"))
                    {
                        urlToAdd = v;
                    }
                }
                else if (v.StartsWith("/"))
                {
                    urlToAdd = "https://www.cnn.com" + v;
                }
                // Checking to see if contained in hashset
                // Adds if not contained
                if (!URLManager.seenLinks.Contains(urlToAdd) && !URLManager.notAllowed.Contains(urlToAdd) && !urlToAdd.Contains("gzip"))
                {
                    URLManager.seenLinks.Add(urlToAdd);
                    acceptableLinks.Add(urlToAdd);
                }
            }
            return acceptableLinks;
        }

        private List<string> ExtractAllAHrefTags(HtmlDocument htmlSnippet)
        {
            List<string> hrefTags = new List<string>();
            HtmlNodeCollection nc = htmlSnippet.DocumentNode.SelectNodes("//a[@href]");

            if (nc != null)
            {
                foreach (HtmlNode link in nc)
                {
                    HtmlAttribute att = link.Attributes["href"];
                    // If it hasn't been added to the queue and is allowed to be crawled
                    if (att != null && att.Value != null)
                    {
                        hrefTags.Add(att.Value);
                    }
                }
            }
            return hrefTags;
        }

        private DateTime StringToDateTime(string date)
        {
            int year = Convert.ToInt32(date.Substring(0, 4));
            int month = Convert.ToInt32(date.Substring(5, 2));
            int day = Convert.ToInt32(date.Substring(8, 2));
            DateTime d = new DateTime(year, month, day);
            return d;
        }

        private bool ValidXML(string xml)
        {
            // Doing two different regex date checks
            Regex rgx = new Regex(@"\d{4}-\d{2}");
            Match mat = rgx.Match(xml);
            if (!mat.Success)
            {
                rgx = new Regex(@"\d{4}/\d{2}");
                mat = rgx.Match(xml);
            }
            // Do another regex check with "/" match
            if (mat.Success)
            {
                string date = mat.ToString();
                int year = Convert.ToInt32(date.Substring(0, 4));
                int month = Convert.ToInt32(date.Substring(5, 2));
                DateTime d = new DateTime(year, month, 1);

                DateTime checkDate = new DateTime(2018, 1, 1);
                int result = DateTime.Compare(d, checkDate);
                if (result == -1)
                {
                    return false;
                }
            }
            else
            {
                rgx = new Regex(@"\d{4}");
                mat = rgx.Match(xml);
                if (mat.Success && Convert.ToInt32(mat.ToString()) < 2017)
                {
                    return false;
                }
            }
            return true;
        }

        private void parseRobots(string robots)
        {
            List<string> disallows = new List<string>();
            List<string> addToXMLQueue = new List<string>();

            string contents;
            using (var wc = new System.Net.WebClient())
                contents = wc.DownloadString(robots);

            bool disallow = false;
            string[] lines = contents.Split(
                new[] { " ", "\n" },
                StringSplitOptions.None
                );
            foreach (string word in lines)
            {
                if (word.Contains("Disallow"))
                {
                    disallow = true;
                }
                else if (disallow && word.Length > 0)
                {
                    // add to disallow list
                    disallows.Add(word);
                }
                else if (word.Contains(".xml"))
                {
                    if (word.Contains("bleacherreport"))
                    {
                        if (word.Contains("nba") || word.Contains("article"))
                        {
                            addToXMLQueue.Add(word);
                        }
                    }
                    else
                    {
                        addToXMLQueue.Add(word);
                    }
                }
            }

            URLManager.notAllowed = disallows;

            this.AddToXMLQueue(addToXMLQueue);
            // Still need to add to XML Queue
        }

        private void AddToXMLQueue(List<string> xmls)
        {
            foreach (string s in xmls)
            {
                sm.AddToXMLQueue(s);
            }
        }

        private void AddToURLQueue(List<string> urls)
        {
            foreach (string s in urls)
            {
                sm.AddToURLQueue(s);
            }
        }
    }
}
