using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace Plantuml
{
    public class Cache : IPlantuml
    {
        private readonly IPlantuml plantuml;
        private readonly string cacheDirectory;

        public Cache(IPlantuml plantuml, string? cacheDirectory = null)
        {
            this.plantuml = plantuml;
            if (cacheDirectory is null)
            {
                cacheDirectory = typeof(Cache).GetProgramDataDirectory();
            }
            this.cacheDirectory = cacheDirectory;
        }

        public async Task Convert(TextReader plantumlMarkup, Stream output)
        {
            var markup = plantumlMarkup.ReadToEnd();
            var p = CachePath(markup);
            if (!p.IsFile())
            {
                using (var w = File.OpenWrite(p.EnsureParentDirectoryExists()))
                {
                    await plantuml.Convert(new StringReader(markup), w);
                }
            }

            using (var r = File.OpenRead(p))
            {
                await r.CopyToAsync(output);
            }
        }

        string CachePath(string markup) => cacheDirectory.Combine(markup.Md5Checksum());

        public void Dispose()
        {
            plantuml.Dispose();
        }
    }
}
