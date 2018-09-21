using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public static class StringExtensions
    {

        public static string ToQuoted(this string input)
        {
            return $"\"{input}\"";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        public static bool StartsWithIgnoreCase(this string text, string test)
        {
            return text.StartsWith(test, StringComparison.OrdinalIgnoreCase);
        }


        public static bool EqualsIgnoreCase(this string source, string test) {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(test);
            return source.Equals(test, StringComparison.OrdinalIgnoreCase);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public static class JsonExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// As method simply converts key value pair in different form, no code coverage is needed.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="jObject"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        public static Dictionary<string, T> ToDictionary<T>(this JObject jObject)
        {
            var dict = new Dictionary<string, T>();

            foreach (var p in jObject.Properties())
            {
                dict[p.Name] = p.Value.ToObject<T>();
            }
            return dict;
        }
    }
}
