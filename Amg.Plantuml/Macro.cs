using Amg.Extensions;
using Amg.FileSystem;
using GitVersion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amg.Plantuml
{
    public class Macro
    {
        public Macro(string name, string args, string include)
        {
            Name = name;
            Args = args;
            Include = include;
        }

        public string Name { get; }
        public string Args { get; }
        public string Include { get; }
    }

    public class Sprite
    {
        public Sprite(string name, string include)
        {
            Name = name;
            Include = include;
        }

        public string Name { get; }
        public string Include { get; }
    }

    public class MacroReader
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Task<IEnumerable<Macro>> Read(string directory) => Task.Factory.StartNew(() =>
            (IEnumerable<Macro>) new Amg.FileSystem.Glob(directory).Include(@"**\*.puml")
                .EnumerateFiles()
                .SelectMany(_ => ReadFile(_, directory))
                .ToList());

        public static IEnumerable<Macro> ReadFile(string pumlFile, string includeRoot)
        {
            Logger.Information(pumlFile);
            var include = pumlFile.RelativeTo(includeRoot).SplitDirectories().Join("/");

            return Lines(pumlFile)
                .Select(_ => Regex.Match(_, @"^!define\s+(?<name>\w+)\s*(?<args>\([^)]+\))?"))
                .Where(_ => _.Success)
                .Select(_ => new Macro(_.Groups["name"].Value, _.Groups["args"].Value, include))
                .ToList();
        }

        static IEnumerable<string> Lines(string textFile)
        {
            using (var r = new StreamReader(textFile))
            {
                while (true)
                {
                    var line = r.ReadLine();
                    if (line is null) break;
                    yield return line;
                }
            }
        }

        public static async Task<IEnumerable<Macro>> Stdlib()
        {
            return await Read(@"C:\Users\griman6i\stdlib");
        }

        public static async Task<IEnumerable<Macro>> StdlibCached()
        {
            var json = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                .Combine("Amg.Plantuml")
                .Combine("stdlib.json");

            if (json.IsFile())
            {
                return (await Json.Read<IEnumerable<Macro>>(json)).ToList();
            }

            var macros = await Stdlib();
            await Json.Write(json.EnsureParentDirectoryExists(), macros);

            return macros;
        }
    }

    public class SpriteReader
    {
        public SpriteReader(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
        }

        public SpriteReader()
            : this(MethodBase.GetCurrentMethod().DeclaringType.GetProgramDataDirectory())
        {
        }

        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string cacheDirectory;

        public static Task<IEnumerable<Sprite>> Read(string directory) => Task.Factory.StartNew(() =>
            (IEnumerable<Sprite>)new Amg.FileSystem.Glob(directory).Include(@"**\*.puml")
                .EnumerateFiles()
                .SelectMany(_ => ReadFile(_, directory))
                .ToList());

        public static IEnumerable<Sprite> ReadFile(string pumlFile, string includeRoot)
        {
            Logger.Information(pumlFile);
            var include = pumlFile.RelativeTo(includeRoot).SplitDirectories().Join("/");

            return Lines(pumlFile)
                .Select(_ => Regex.Match(_, @"^sprite\s+\$(?<name>\w+)"))
                .Where(_ => _.Success)
                .Select(_ => new Sprite(_.Groups["name"].Value, include))
                .ToList();
        }

        static IEnumerable<string> Lines(string textFile)
        {
            using (var r = new StreamReader(textFile))
            {
                while (true)
                {
                    var line = r.ReadLine();
                    if (line is null) break;
                    yield return line;
                }
            }
        }

        public static async Task<string> ExtractStdlib(string workDir)
        {
            Logger.Information("Extract plantuml stdlib to {0}", workDir);
            var plantuml = Local.Create(new LocalSettings());
            var tool = await plantuml.GetTool();
            await tool.WithWorkingDirectory(workDir).Run("-extractstdlib");
            return workDir.Combine("stdlib");
        }

        public static async Task<IEnumerable<Sprite>> Stdlib()
        {
            var workDir = Amg.Util.PathExtensions.GetTempDirectory().EnsureDirectoryExists();
            try
            {
                var stdlibDir = await ExtractStdlib(workDir);
                return await Read(stdlibDir);
            }
            finally
            {
                await workDir.EnsureNotExists();
            }
        }

        public async Task<IEnumerable<Sprite>> StdlibCached()
        {
            var json = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                .Combine("Amg.Plantuml")
                .Combine("stdlib_sprites.json");

            if (json.IsFile())
            {
                return (await Json.Read<IEnumerable<Sprite>>(json)).ToList();
            }

            var macros = await Stdlib();
            await Json.Write(json.EnsureParentDirectoryExists(), macros);

            return macros;
        }
    }
}
