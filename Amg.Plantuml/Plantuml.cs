using System;
using System.Collections.Generic;
using System.Text;

namespace Amg.Plantuml
{
    public class Plantuml
    {
        public static IPlantuml Local(LocalSettings? settings = null) => new Local(settings is null ? new LocalSettings() : settings);

        public static IPlantuml Cached(IPlantuml plantuml, string? cacheDirectory = null)
        {
            return new Cache(plantuml, cacheDirectory);
        }
    }
}
