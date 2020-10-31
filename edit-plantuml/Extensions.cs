using AngleSharp.Dom;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
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
    }
}
