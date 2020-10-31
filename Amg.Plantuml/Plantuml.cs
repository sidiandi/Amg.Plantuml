using Amg.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Amg.Plantuml
{
    public class Plantuml
    {
        public static IPlantuml Local(LocalSettings? settings = null) => Amg.Plantuml.Local.Create(settings is null ? new LocalSettings() : settings);

        public static IPlantuml Cached(IPlantuml plantuml, string? cacheDirectory = null)
        {
            return new Cache(plantuml, cacheDirectory);
        }

        public static IEnumerable<string> GetSections(string plantumlSource)
        {
            var m = Regex.Matches(plantumlSource, @"^@end.*$", RegexOptions.Multiline)
                .Cast<Match>().ToList();
            var end = 0;
            if (m.Count > 0)
            {
                for (int i = 0; i< m.Count; ++i)
                {
                    var newEnd = m[i].Index + m[i].Length;
                    yield return plantumlSource.Substring(end,  newEnd - end);
                    end = newEnd;
                }
            }
            else
            {
                yield return plantumlSource + "\r\n@end\r\n";
            }
        }
    }
}
