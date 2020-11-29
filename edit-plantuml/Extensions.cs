using AngleSharp.Dom;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Amg.EditPlantuml
{
    public static class Extensions
    {
        public static async Task<IList<Y>> SelectAsync<X,Y>(this IEnumerable<X> x, Func<X,Task<Y>> f)
        {
            var result = new List<Y>();
            foreach (var i in x)
            {
                result.Add(await f(i));
            }
            return result;
        }

        public static void Append(this TextDocument document, string text)
        {
            document.Insert(document.TextLength, text);
        }

        public static async Task<byte[]> ReadToEndAsync(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
