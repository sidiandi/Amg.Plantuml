using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using Amg.Util;
using System.Runtime.CompilerServices;
using Amg.Build;

[assembly: InternalsVisibleTo("Amg.Plantuml.Tests")]

namespace Amg.Plantuml
{
    public class LocalSettings
    {
        public string? PlantUmlJarFile { get; set; }
        public string[] Options { get; set; } = new string[] { };
        public string? GraphvizDotFile { get; set; }
    }

    // https://plantuml.com/de/command-line
    public class Local : IDisposable, IPlantuml
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Local(LocalSettings settings)
        {
            this.settings = settings;
        }

        Local()
        {
        }

        public static Local Create(LocalSettings settings) => Amg.Build.Once.Create<Local>(settings);

        public async Task Convert(string plantumlMarkup, string outputFile)
        {
            using (var output = File.OpenWrite(outputFile.EnsureParentDirectoryExists()))
            {
                await Convert(new StringReader(plantumlMarkup), output);
            }
        }

        readonly SemaphoreSlim convertInProgress = new SemaphoreSlim(1, 1);

        public async Task Convert(TextReader plantumlMarkup, Stream output)
        {
            await convertInProgress.WaitAsync();
            try
            {
                var markup = await plantumlMarkup.ReadToEndAsync();
                Logger.Information("Convert {0} characters", markup.Length);
                var stopwatch = Stopwatch.StartNew();

                // ensure that end marker is present
                if (!Regex.IsMatch(markup, @"@end"))
                {
                    markup = markup + "\r\n@end";
                }

                var process = await GetPlantumlProcess();
                var captureOutput = process.StandardOutput.BaseStream.CopyUntilAsync(pipedelimitor + "\r\n", output);
                process.StandardInput.WriteLine(markup);
                await captureOutput;
                Logger.Information("Convert done: {0}", stopwatch.Elapsed);
            }
            finally
            {
                convertInProgress.Release();
            }
        }

        [Once]
        protected virtual async Task<Process> GetPlantumlProcess()
        {
            var plantumlJar = await GetPlantumlJarFile();

            var startInfo = new ProcessStartInfo
            {
                FileName = "java.exe",
                Arguments = new string[]
                {
                    "-jar",
                    plantumlJar.Quote(),
                    "-pipe", "-pipedelimitor", pipedelimitor
                }
                .Concat(settings.Options).Join(" "),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var dot = await GetGraphvizDotFile();
            startInfo.Environment["GRAPHVIZ_DOT"] = dot;

            return Process.Start(startInfo);
        }

        readonly string pipedelimitor = Guid.NewGuid().ToString();
        private readonly LocalSettings settings;

        public void Dispose()
        {
            var plantumlProcess = GetPlantumlProcess().Result;
            if (plantumlProcess is { })
            {
                plantumlProcess.StandardInput.Close();
                plantumlProcess.StandardOutput.ReadToEnd();
                plantumlProcess.WaitForExit();
            }
        }

        public async Task<ITool> GetTool()
        {
            var tool = Tools.Default.WithFileName("java.exe").WithArguments(
                "-jar", await GetPlantumlJarFile());

            var gvd = await GetGraphvizDotFile();
            if (gvd is { })
            {
                tool = tool.WithArguments("-graphvizdot", gvd);
            }
            return tool;
        }

        [Once]
        public virtual async Task<string> GetPlantumlJarFile()
        {
            if (settings.PlantUmlJarFile is { })
            {
                return settings.PlantUmlJarFile;
            }

            // last resort: try to download Plantuml
            return await DownloadPlantuml();
        }

        string PlantumlJarFileName => "plantuml.jar";

        [Once]
        internal async Task<string> DownloadPlantuml()
        {
            var plantumlJarFile = LibDir.Combine(PlantumlJarFileName);
            if (!plantumlJarFile.IsFile())
            {
                var wc = new WebClient();
                var url = "https://netcologne.dl.sourceforge.net/project/plantuml/plantuml.jar";
                var dest = plantumlJarFile.EnsureParentDirectoryExists();
                Logger.Information("Download {0} to {1}", url, dest);
                await wc.DownloadFileTaskAsync(url, dest);
            }
            return plantumlJarFile;
        }

        static string? GetProgramFile(string pathGlob)
        {
            return System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Glob()
                .Concat(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).Glob(@"Graphviz*\bin\dot.exe"))
                .FirstOrDefault();
        }

        [Once]
        public virtual async Task<string> GetGraphvizDotFile()
        {
            if (settings.GraphvizDotFile is { })
            {
                return settings.GraphvizDotFile;
            }

            var dotExe = GetProgramFile(@"Graphviz*\bin\dot.exe");

            if (dotExe is { })
            {
                return dotExe;
            }

            return await DownloadGraphviz();
        }

        [Once]
        internal virtual async Task<string> DownloadGraphviz()
        {
            var graphvizName = "graphviz-2.38";
            var graphvizDir = LibDir.Combine(graphvizName);
            var dotExe = graphvizDir.Combine("release", "bin", "dot.exe");
            if (!dotExe.IsFile())
            {
                var wc = new WebClient();
                await wc.DownloadAndExtract("https://graphviz.gitlab.io/_pages/Download/windows/graphviz-2.38.zip", graphvizDir);

                if (!dotExe.IsFile())
                {
                    throw new InvalidOperationException("Cannot download graphviz");
                }
            }

            return dotExe;
        }

        string LibDir => typeof(Local).GetProgramDataDirectory().Combine("lib");
    }
}
