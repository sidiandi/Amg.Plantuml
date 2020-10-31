using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Plantuml.Tests
{
    [TestFixture]
    class DocReaderTests
    {
        [Test]
        public async Task RipDoc()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            var docReader = new DocReader();

            await docReader.GetDoc();
        }
    }
}
