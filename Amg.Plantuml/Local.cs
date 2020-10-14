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
    public sealed class Local : IDisposable, IPlantuml
    {
        public Local(LocalSettings settings)
        {
            this.settings = settings;
        }

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

                // ensure that end marker is present
                if (!Regex.IsMatch(markup, @"@end"))
                {
                    markup = markup + "\r\n@end";
                }

                var captureOutput = PlantumlProcess.StandardOutput.BaseStream.CopyUntilAsync(pipedelimitor + "\r\n", output);
                PlantumlProcess.StandardInput.WriteLine(markup);
                await captureOutput;
            }
            finally
            {
                convertInProgress.Release();
            }
        }

        Process PlantumlProcess
        {
            get
            {
                if (plantumlProcess is null)
                {
                    if (!PlantumlJarFile.IsFile())
                    {
                        throw new FileNotFoundException($"plantuml.jar not found at {PlantumlJarFile}.");
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "java.exe",
                        Arguments = new string[]
                        {
                            "-jar",
                            PlantumlJarFile.Quote(),
                            "-pipe", "-pipedelimitor", pipedelimitor
                        }
                        .Concat(settings.Options).Join(" "),
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    if (this.GraphvizDotFile is { })
                    {
                        startInfo.Environment["GRAPHVIZ_DOT"] = this.GraphvizDotFile;
                    }

                    plantumlProcess = Process.Start(startInfo);
                }

                return plantumlProcess;
            }
        }

        Process? plantumlProcess = null;
        readonly string pipedelimitor = Guid.NewGuid().ToString();
        private readonly LocalSettings settings;

        public void Dispose()
        {
            if (plantumlProcess is { })
            {
                plantumlProcess.StandardInput.Close();
                plantumlProcess.StandardOutput.ReadToEnd();
                plantumlProcess.WaitForExit();
            }
        }

        public ITool Tool
        {
            get
            {
                var tool = Tools.Default.WithFileName("java.exe").WithArguments(
                    "-jar", PlantumlJarFile);

                var gvd = GraphvizDotFile;
                if (gvd is { })
                {
                    tool = tool.WithArguments("-graphvizdot", gvd);
                }
                return tool;
            }
        }

        public string PlantumlJarFile
        {
            get
            {
                if (settings.PlantUmlJarFile is { })
                {
                    return settings.PlantUmlJarFile;
                }

                var d = new string[]
                {
                    LibDir,
                    System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).Combine(@"chocolatey\lib\plantuml\tools"),

                }
                .Select(_ => _.Combine(PlantumlJarFileName))
                .FirstOrDefault(_ => _.IsFile());

                if (d is { })
                {
                    return d;
                }

                // last resort: try to download Plantuml
                return DownloadPlantuml().Result;
            }
        }

        string PlantumlJarFileName = "plantuml.jar";

        internal async Task<string> DownloadPlantuml()
        {
            var plantumlJarFile = LibDir.Combine(PlantumlJarFileName).EnsureParentDirectoryExists();
            if (!plantumlJarFile.IsFile())
            {
                var wc = new WebClient();
                await wc.DownloadFileTaskAsync("https://netcologne.dl.sourceforge.net/project/plantuml/plantuml.jar", plantumlJarFile);
            }
            return plantumlJarFile;
        }

        public string? GraphvizDotFile
        {
            get
            {
                if (settings.GraphvizDotFile is { })
                {
                    return settings.GraphvizDotFile;
                }

                var dotExe = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Glob(@"Graphviz*\bin\dot.exe")
                    .Concat(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).Glob(@"Graphviz*\bin\dot.exe"))
                    .FirstOrDefault();

                if (dotExe is { })
                {
                    return dotExe;
                }

                return DownloadGraphviz().Result;
            }
        }

        internal async Task<string> DownloadGraphviz()
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
