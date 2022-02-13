using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Plantuml;

internal static class Extensions
{
    public static async Task<byte[]> ReadToEndAsync(this Stream s)
    {
        var m = new MemoryStream();
        var b = new byte[4096];
        while (true)
        {
            var bytesRead = await s.ReadAsync(b, 0, b.Length);
            if (bytesRead == 0)
            {
                break;
            }
            m.Write(b, 0, bytesRead);
        }
        return m.ToArray();
    }
}
