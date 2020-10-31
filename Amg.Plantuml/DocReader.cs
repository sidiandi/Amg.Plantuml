using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Amg.Extensions;

namespace Amg.Plantuml
{
    class DocReader
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task GetDoc()
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 20,
            };
            var crawler = new PoliteWebCrawler(config);

            crawler.PageCrawlCompleted += Crawler_PageCrawlCompleted;

            var crawlResult = await crawler.CrawlAsync(new Uri("https://plantuml.com/"));
        }

        private void Crawler_PageCrawlCompleted(object? sender, PageCrawlCompletedArgs e)
        {
            Logger.Information("{0}", e.CrawledPage.Uri);

            foreach (var m in GetExamples(e.CrawledPage.Content.Text))
            {
                Console.WriteLine(m);
            }
        }

        public static IEnumerable<string> GetExamples(string html)
        {
            var headers = Regex.Matches(html, @"\<h\d\>(.*)\<\/h\d\>");
            Console.WriteLine(headers.Cast<Match>().Select(_ => _.Groups[1].Value).Join());
            
            var examples = Regex.Matches(html, @"Edit online\<\/button\>\<code\>\<pre\>(.*)\<\/pre\>", RegexOptions.Singleline);

            return examples.Cast<Match>()
                .Select(_ => _.Groups[1].Value);
        }
    }
}
