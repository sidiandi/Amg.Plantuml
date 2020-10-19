using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Amg.EditPlantuml
{
    class Parser
    {
        public static string? GetTitle(string puml)
        {
            var m = Regex.Match(puml, @"^title\s+([^\n]+)$", RegexOptions.Multiline | RegexOptions.Singleline);
            return m.Success
                ? m.Groups[1].Value.Trim()
                : null;
        }
    }
}
