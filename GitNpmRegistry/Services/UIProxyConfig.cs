using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{

    /// <summary>
    /// 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class UIProxyConfig
    {

        public string CDN { get; set; }

        public string CachePath { get; set; }

        private Dictionary<string, ProxyConfig> configs;

        /// <summary>
        /// 
        /// </summary>
        public ProxyConfig[] Git { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public ProxyConfig[] NPM { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="throwIfNotFound"></param>
        /// <returns></returns>
        public ProxyConfig Get(string package, bool throwIfNotFound = true) {
            if (package.Contains("@")) {
                package = package.Split("@")[0];
            }
            var cs = configs ?? (configs = SetupConfigs());
            string t = package.ToLower();
            if (cs.TryGetValue(t, out ProxyConfig c)){
                return c;
            };
            if (throwIfNotFound)
                throw new KeyNotFoundException($"{t} not found in git/npm package list");
            return null;
        }

        private Dictionary<string,ProxyConfig> SetupConfigs()
        {
            var cd = new Dictionary<string, ProxyConfig>();
            if (Git != null)
            {
                foreach (var g in Git)
                {
                    cd[g.Package.ToLower()] = g;
                    if (!g.Version.StartsWith("v"))
                    {
                        g.Version = "v" + g.Version;
                    }
                    g.Source = "git";
                }
            }
            if (NPM != null)
            {
                foreach (var g in NPM)
                {
                    cd[g.Package.ToLower()] = g;
                    g.Source = "npm";
                }
            }
            return cd;
        }


        /// <summary>
        /// 
        /// </summary>
        public class ProxyConfig {

            /// <summary>
            /// 
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Package { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Url { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Source { get; set; }
        }

    }

    
}
