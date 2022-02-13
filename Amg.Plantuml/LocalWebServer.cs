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

namespace Amg.Plantuml;

public class LocalWebServerOptions
{
    public int Port = 16395;
    public string? PlantUmlJarFile;
    public string[] Options { get; set; } = new string[] { };
}

public class LocalWebServer : IDisposable, IPlantuml
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    const string localIp = "127.0.0.1";

    public static LocalWebServer Create(LocalWebServerOptions options) => new LocalWebServer(options);

    LocalWebServer(LocalWebServerOptions options)
    {
        this.options = options;
        this.port = options.Port;
    }

    int port;

    public async Task Convert(TextReader plantumlMarkup, Stream output)
    {
        await StartWebServer();

        var markup = await plantumlMarkup.ReadToEndAsync();
        var stopwatch = Stopwatch.StartNew();

        using var webClient = new WebClient();
        var encodedData = Encode(markup);
        byte[] picture;
        try
        {
            picture = await webClient.DownloadDataTaskAsync($"http://{server.server}/plantuml/png/{encodedData}");
        }
        catch (WebException ex)
        {
            if (ex.Response.ContentType == "image/png")
            {
                picture = await ex.Response.GetResponseStream().ReadToEndAsync();
            }
            else
            {
                throw;
            }
        }
        output.Write(picture, 0, picture.Length);
        Logger.Information("Converted {inputLength} characters, output {outputLength} bytes, {duration}", 
            markup.Length, 
            picture.Length, 
            stopwatch.Elapsed);
    }

    static string Encode(string plantumlMarkup)
    {
        // https://plantuml.com/de/text-encoding
        return "~h" + System.Text.Encoding.UTF8.GetBytes(plantumlMarkup).Hex();
    }

    async Task StartWebServer()
    {
        if (server is null || server.plantumlProcess.HasExited)
        {
            server = await GetPlantumlProcess(port++);
        }
    }

    class ServerProcess
    {
        public string server;
        public Process plantumlProcess;
    }

    ServerProcess? server;

    Local localPlantuml = Local.Create(new LocalSettings());

    async Task<ServerProcess> GetPlantumlProcess(int port)
    {
        var plantumlJar = await localPlantuml.GetPlantumlJarFile();

        var server = $"{localIp}:{port}";
        var startInfo = new ProcessStartInfo
        {
            FileName = "java.exe",
            Arguments = new string[]
            {
                    "-jar",
                    plantumlJar.Quote(),
                    $"-picoweb:{port}:{localIp}"
            }
            .Concat(options.Options).Join(" "),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var dot = await localPlantuml.GetGraphvizDotFile();
        startInfo.Environment["GRAPHVIZ_DOT"] = dot;

        var process = Process.Start(startInfo);

        _ = ReadLines(process.StandardOutput, _ => Logger.Information(_));
        _ = ReadLines(process.StandardError, _ => Logger.Error(_));
        
        return new ServerProcess { plantumlProcess = process, server = server };
    }

    static async Task ReadLines(TextReader r, Action<string> forEachLine)
    {
        while (true)
        {
            var line = await r.ReadLineAsync();
            if (line is null) break;
            forEachLine(line);
        }
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private readonly LocalWebServerOptions options;
    Process? plantumlProcess = null;

    public void Dispose()
    {
        StopServer();
    }

    void StopServer()
    {
        if (server is { })
        {
            server.plantumlProcess.Kill();
            server.plantumlProcess.WaitForExit();
            server = null;
        }
    }
}
