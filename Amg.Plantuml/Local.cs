using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

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

        public async Task Convert(TextReader plantumlMarkup, Stream output)
        {
            var captureOutput = PlantumlProcess.StandardOutput.BaseStream.CopyUntilAsync(pipedelimitor + "\r\n", output);
            PlantumlProcess.StandardInput.Write(await plantumlMarkup.ReadToEndAsync());
            await captureOutput;
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
                    };

                    startInfo.Environment["GRAPHVIZ_DOT"] = this.GraphvizDotFile;

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
                plantumlProcess.WaitForExit();
            }
        }

        string PlantumlJarFile
        {
            get
            {
                if (settings.PlantUmlJarFile is { })
                {
                    return settings.PlantUmlJarFile;
                }

                return ExtDir.Combine("plantuml.jar");
            }
        }

        string GraphvizDotFile
        {
            get
            {
                if (settings.GraphvizDotFile is { })
                {
                    return settings.GraphvizDotFile;
                }

                return ExtDir.Combine(@"graphviz-2.38\release\bin\dot.exe");
            }
        }

        string ExtDir => Assembly.GetExecutingAssembly().Location.Parent().Combine("plantuml");
    }
}
