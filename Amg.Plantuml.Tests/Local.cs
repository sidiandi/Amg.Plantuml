using Amg.Extensions;
using Amg.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    AssertIsValidPng(outputFile);
                }
            }
            Console.WriteLine(sw.Elapsed);
        }

        static void AssertIsValidPng(string outputFile)
        {
            Assert.That(new FileInfo(outputFile).Length > 0);
            using (var image = System.Drawing.Image.FromFile(outputFile))
            {
            }
        }

        [Test]
        public async Task ParallelInput()
        {
            var plantumlMarkup = @"@startuml
Alice -> Bob: Authentication Request
Bob --> Alice: Authentication Response

Alice -> Bob: Another authentication Request
Alice <-- Bob: another authentication Response
@enduml
";

            var outFile = TestDir.Combine("out.png");
            using (var plantuml = Plantuml.Local())
            {
                var results = Enumerable.Range(0, 20).AsParallel()
                    .Select(_ =>
                    {
                        plantuml.Convert(plantumlMarkup, outFile);
                        return true;
                    })
                    .ToList();
                Assert.That(results.All(_ => _));
            };
        }
    }
}
