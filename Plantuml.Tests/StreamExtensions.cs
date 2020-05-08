using Amg.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plantuml
{
    public class StreamExtensionsTests
    {
            [Test]
            public void TestCase()
            {
                var sep = "sep";
                var count = 10;
                var data = Encoding.ASCII.GetBytes(Enumerable.Range(0, count).Select(_ => _.ToString()).Join(sep));
                var input = new MemoryStream(data);

                for (int i=0; i< count;++i)
                {
                    var output = new MemoryStream();
                    input.CopyUntil(sep, output);
                    var text = Encoding.ASCII.GetString(output.GetBuffer());
                    Console.WriteLine(text);
                    Assert.That(int.Parse(text), Is.EqualTo(i));
                }
            }
    }
}
