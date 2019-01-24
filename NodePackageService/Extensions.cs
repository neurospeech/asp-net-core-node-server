using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NeuroSpeech
{
    public static class StringExtensions
    {

        public static (string, string, string) ParseNPMPath(this string input)
        {
            string package = "";
            string version = "";
            string path = "";
            if (input.StartsWith("@"))
            {
                var (scope, packagePath) = input.ExtractTill("/");

                (package, path) = packagePath.ExtractTill("/");
                (package, version) = package.ExtractTill("@");
                package = scope + "/" + package;
            }
            else
            {
                (package, path) = input.ExtractTill("/");
                (package, version) = package.ExtractTill("@");
            }

            if(version.StartsWith("v"))
            {
                version = version.Substring(1);
            }

            return (package, version, path);
        }

        public static (string, string) ExtractTill(this string input, string separator)
        {
            int index = input.IndexOf(separator);
            if (index == -1)
            {
                return (input, "");
            }
            return (input.Substring(0, index), input.Substring(index + 1));
        }

        public static string SubstringTill(this string input, string separator)
        {
            bool scoped = false;
            if (input.StartsWith("@"))
            {
                scoped = true;
                input = input.Substring(1);
            }
            input = input.Split(separator)[0];
            if (scoped)
            {
                input = "@" + input;
            }
            return input;
        }

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


        public static bool EqualsIgnoreCase(this string source, string test)
        {
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
