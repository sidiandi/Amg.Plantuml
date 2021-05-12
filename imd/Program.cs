using Amg.FileSystem;
using Amg.GetOpt;
using Amg.Plantuml;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace imd
{
    public class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static int Main(string[] args) => Amg.Build.Runner.Run(args);

        string PngExtension => ".png";
        string MdExtension => ".md";
        string ImdExtension => ".i" + MdExtension;

        FileSystemWatcher[] CreateWatchers(string? Directory)
        {
            if (Directory is { })
            {
                var watcher = new FileSystemWatcher();
                watcher.Path = Directory.Absolute();
                watcher.IncludeSubdirectories = true;
                watcher.Filter = "*" + ImdExtension;
                watcher.Changed += Watcher_Changed;
                watcher.Created += Watcher_Changed;
                watcher.Deleted += Watcher_Changed;
                watcher.Renamed += Watcher_Changed;
                watcher.EnableRaisingEvents = true;
                return new[] { watcher };
            }
            else
            {
                return DriveInfo.GetDrives().Select(driveInfo =>
                {
                    var watcher = new FileSystemWatcher();
                    watcher.Path = driveInfo.RootDirectory.FullName;
                    watcher.IncludeSubdirectories = true;
                    watcher.Filter = "*" + ImdExtension;
                    watcher.Changed += Watcher_Changed;
                    watcher.Created += Watcher_Changed;
                    watcher.Deleted += Watcher_Changed;
                    watcher.Renamed += Watcher_Changed;
                    watcher.EnableRaisingEvents = true;
                    return watcher;
                }).ToArray();
            }
        }

        void DisposeWatchers(FileSystemWatcher[] watchers)
        {
            foreach (var watcher in watchers)
            {
                watcher.Changed -= Watcher_Changed;
                watcher.Created -= Watcher_Changed;
                watcher.Deleted -= Watcher_Changed;
                watcher.Renamed -= Watcher_Changed;
                watcher.Dispose();
            }
        }

        [Description("Watch directory and update .i.md files"), Default]
        public async Task Watch()
        {
            var watchers = CreateWatchers(this.Directory);
            await Console.In.ReadLineAsync();
            DisposeWatchers(watchers);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(ImdExtension, StringComparison.OrdinalIgnoreCase))
            {
                ProcessInlineMarkdown(e.FullPath);
            }
        }

        readonly IPlantuml plantUml = Amg.Plantuml.Plantuml.Cached(Plantuml.Local());

        static bool ProcessBlock(string blockName, string firstLine, TextReader text, out string? block)
        {
            if (firstLine.Equals("@start" + blockName))
            {
                block = ReadUntil(text, "@end" + blockName);
                return true;
            }
            else
            {
                block = null;
                return false;
            }
        }

        public async Task ProcessInlineMarkdown(string imdFile)
        {
            imdFile = imdFile.Absolute();
            var name = imdFile.FileName().Substring(0, imdFile.FileName().Length - ImdExtension.Length);
            var imageDir = imdFile.Parent().Combine(name + ".img");
            var mdFile = imdFile.Parent().Combine(name + MdExtension);
            int imageIndex = 0;

            using (var r = new StreamReader(imdFile))
            using (var w = new StreamWriter(mdFile))
            {
                while (true)
                {
                    var line = await r.ReadLineAsync();
                    if (line is null)
                    {
                        break;
                    }
                    
                    if (ProcessBlock("uml", line, r, out var plantumlText))
                    {
                        var pngFile = imageDir.Combine($"{imageIndex}{PngExtension}");
                        await plantUml.Convert(plantumlText, pngFile.EnsureParentDirectoryExists());
                        var relativePngFile = $"./{name}.img/{imageIndex}{PngExtension}";
                        w.WriteLine($"![diagram {imageIndex}]({relativePngFile})");
                        ++imageIndex;
                        continue;
                    }

                    if (ProcessBlock("posh", line, r, out var block))
                    {
                        // todo - support powershell
                        w.WriteLine("posh!");
                        continue;
                    }

                    w.WriteLine(line);
                }
            }
        }

        static string ReadUntil(TextReader r, string endLine)
        {
            var w = new StringWriter();
            while (true)
            {
                var line = r.ReadLine();
                if (line is null)
                {
                    break;
                }
                if (line.Equals(endLine))
                {
                    break;
                }
                w.WriteLine(line);
            }
            return w.ToString();
        }

        [Description("Directory to watch. Default: all local drives.")]
        public string? Directory { get; set; }

        [Description("Compiles a single .i.md file")]
        public async Task Compile(string imdFile)
        {
            await ProcessInlineMarkdown(imdFile);
        }
    }
}
