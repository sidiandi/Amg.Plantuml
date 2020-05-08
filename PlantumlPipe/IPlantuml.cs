using Amg.FileSystem;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Plantuml
{
    public interface IPlantuml : IDisposable
    {
        Task Convert(TextReader plantumlMarkup, Stream output);
    }

    public static class IPlantumlExtensions
    {
        public static async Task<string> Convert(this IPlantuml plantuml, string plantumlMarkup, string outputFile)
        {
            using (var output = File.OpenWrite(outputFile.EnsureParentDirectoryExists()))
            {
                await plantuml.Convert(new StringReader(plantumlMarkup), output);
                return outputFile;
            }
        }
    }
}