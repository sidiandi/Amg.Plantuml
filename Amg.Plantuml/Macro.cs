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

        public static IEnumerable<string> OpenIconicNames() => @"account-login
account-logout
action-redo
action-undo
align-center
align-left
align-right
aperture
arrow-bottom
arrow-circle-bottom
arrow-circle-left
arrow-circle-right
arrow-circle-top
arrow-left
arrow-right
arrow-thick-bottom
arrow-thick-left
arrow-thick-right
arrow-thick-top
arrow-top
audio
audio-spectrum
badge
ban
bar-chart
basket
battery-empty
battery-full
beaker
bell
bluetooth
bold
bolt
book
bookmark
box
briefcase
british-pound
browser
brush
bug
bullhorn
calculator
calendar
camera-slr
caret-bottom
caret-left
caret-right
caret-top
cart
chat
check
chevron-bottom
chevron-left
chevron-right
chevron-top
circle-check
circle-x
clipboard
clock
cloud
cloud-download
cloud-upload
cloudy
code
cog
collapse-down
collapse-left
collapse-right
collapse-up
command
comment-square
compass
contrast
copywriting
credit-card
crop
dashboard
data-transfer-download
data-transfer-upload
delete
dial
document
dollar
double-quote-sans-left
double-quote-sans-right
double-quote-serif-left
double-quote-serif-right
droplet
eject
elevator
ellipses
envelope-closed
envelope-open
euro
excerpt
expand-down
expand-left
expand-right
expand-up
external-link
eye
eyedropper
file
fire
flag
flash
folder
fork
fullscreen-enter
fullscreen-exit
globe
graph
grid-four-up
grid-three-up
grid-two-up
hard-drive
header
headphones
heart
home
image
inbox
infinity
info
italic
justify-center
justify-left
justify-right
key
laptop
layers
lightbulb
link-broken
link-intact
list
list-rich
location
lock-locked
lock-unlocked
loop
loop-circular
loop-square
magnifying-glass
map
map-marker
media-pause
media-play
media-record
media-skip-backward
media-skip-forward
media-step-backward
media-step-forward
media-stop
medical-cross
menu
microphone
minus
monitor
moon
move
musical-note
paperclip
pencil
people
person
phone
pie-chart
pin
play-circle
plus
power-standby
print
project
pulse
puzzle-piece
question-mark
rain
random
reload
resize-both
resize-height
resize-width
rss
rss-alt
script
share
share-boxed
shield
signal
signpost
sort-ascending
sort-descending
spreadsheet
star
sun
tablet
tag
tags
target
task
terminal
text
thumb-down
thumb-up
timer
transfer
trash
underline
vertical-align-bottom
vertical-align-center
vertical-align-top
video
volume-high
volume-low
volume-off
warning
wifi
wrench
x
yen
zoom-in
zoom-out".SplitLines();
    }
}
