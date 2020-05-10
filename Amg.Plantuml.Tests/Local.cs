using Amg.Extensions;
using Amg.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Plantuml
{
    [TestFixture]
    public sealed class LocalTests
    {
        string TestDir = typeof(LocalTests).GetProgramDataDirectory();

        [Test]
        public async Task Test()
        {
            var count = 10;

            var plantumlMarkup = @"@startuml
Alice -> Bob: Authentication Request
Bob --> Alice: Authentication Response

Alice -> Bob: Another authentication Request
Alice <-- Bob: another authentication Response
@enduml
";

            var sw = Stopwatch.StartNew();
            using (var plantuml = Plantuml.Local(new LocalSettings
            {
                Options = new[] { "-v" }
            }))
            {
                for (int i = 0; i < count; ++i)
                {
                    var outputFile = TestDir.Combine($@"Alice-{i}.png");
                    await plantuml.Convert(plantumlMarkup, outputFile);
                    Assert.That(new FileInfo(outputFile).Length > 0);
                    using (var image = System.Drawing.Image.FromFile(outputFile))
                    {
                    }
                }
            }
            Console.WriteLine(sw.Elapsed);
        }
    }
}
