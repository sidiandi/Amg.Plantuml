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

        string PlantumlJarFile
        {
            get
            {
                if (settings.PlantUmlJarFile is { })
                {
                    return settings.PlantUmlJarFile;
                }

                return new string[]
                {
                    ExtDir,
                    System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).Combine(@"chocolatey\lib\plantuml\tools"),

                }
                .Select(_ => _.Combine("plantuml.jar"))
                .First(_ => _.IsFile());
            }
        }

        string? GraphvizDotFile
        {
            get
            {
                if (settings.GraphvizDotFile is { })
                {
                    return settings.GraphvizDotFile;
                }
                return null;
            }
        }

        string ExtDir => Assembly.GetExecutingAssembly().Location.Parent().Combine("plantuml");
    }
}
