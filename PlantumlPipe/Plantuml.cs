using System;
using System.Collections.Generic;
using System.Text;

namespace Plantuml
{
    public class Plantuml
    {
        public static IPlantuml Local(LocalSettings settings) => new Local(settings);

        public static IPlantuml Cached(IPlantuml plantuml, string? cacheDirectory = null)
        {
            return new Cache(plantuml, cacheDirectory);
        }
    }
}
