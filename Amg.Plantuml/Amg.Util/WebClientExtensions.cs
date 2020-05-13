using Amg.FileSystem;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Util
{
    public static class WebClientExtensions
    {
        public static async Task<string> DownloadAndExtract(this WebClient webClient, string url, string outputDir)
        {
            var archiveFile = typeof(WebClientExtensions).GetTempDirectory().Combine(new Uri(url).LocalPath.FileName());
            await webClient.DownloadFileTaskAsync(url, archiveFile);
            await Task.Factory.StartNew(() =>
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(archiveFile, outputDir.EnsureDirectoryExists());
            }, TaskCreationOptions.LongRunning);
            archiveFile.EnsureFileNotExists();
            return outputDir;
        }
    }
}
