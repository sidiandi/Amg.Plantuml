using Amg.Extensions;
using Amg.FileSystem;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace Amg.EditPlantuml
{
    static class Util
    {
        public static string Filename(DateTime time, string title)
        {
            return $"{time.ToFileName()}-{ToFilename(title)}";
        }

        public static string ToFilename(string title)
        {
            return Regex.Split(title, @"\s+")
                .Select(_ => WebUtility.UrlEncode(_))
                .Join("-");
        }

        public static ISegment? Find(this TextDocument document, string searchString)
        {
            var i = document.Text.IndexOf(searchString);
            return i < 0
                ? null
                : new TextSegment { StartOffset = i, Length = searchString.Length };
        }
    }
}
