using Amg.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Plantuml.Tests
{
    [TestFixture]
    public class MacroTests
    {
        [Test]
        public async Task ReadStdLib()
        {
            var macros = await MacroReader.Stdlib();
            Console.WriteLine(macros.Count());
            Console.WriteLine(macros.Select(_ => new { _.Name, _.Include }).Join());
            await Json.Write(@"C:\temp\stdlib.json", macros);
        }

        [Test]
        public async Task ReadStdLibCache()
        {
            var macros = await MacroReader.StdlibCached();
            Console.WriteLine(macros.Count());
        }
    }
}
