using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace Plantuml
{
    public class LocalSettings
    {
        public string? PlantUmlJarFile { get; set; }
        public string[] Options { get; set; } = new string[] { };
    }

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
                    plantumlProcess = Process.Start(new ProcessStartInfo
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
                        UseShellExecute = false
                    });

                    plantumlProcess.Start();
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

                return Assembly.GetExecutingAssembly().Location.Parent().Combine("plantuml.jar");
            }
        }
    }
}
