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

namespace Plantuml
{
    [TestFixture]
    public sealed class PlantumlTests
    {
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
            }))
            {
                for (int i = 0; i < count; ++i)
                {
                    await plantuml.Convert(plantumlMarkup, $@"Alice-{i}.png");
                }
            }
            Console.WriteLine(sw.Elapsed);
        }
    }
}
