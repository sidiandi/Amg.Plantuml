using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Amg.Util
{
    public static class PathExtensions
    {
        /// <summary>
        /// Gets a temp directory for type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTempDirectory(this System.Type type)
        {
            var assembly = type.Assembly;
            return System.IO.Path.GetTempPath().Combine(new[]
            {
                assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Map(_ => _.Company),
                assembly.GetCustomAttribute<AssemblyProductAttribute>().Map(_ => _.Product),
                type.Name
            }.Where(_ => !String.IsNullOrEmpty(_))
            .NotNull()
            .ToArray())
            .EnsureDirectoryExists();
        }

        public static string GetTempDirectory()
        {
            return System.IO.Path.GetTempPath().Combine(System.IO.Path.GetRandomFileName());
        }
    }
}
