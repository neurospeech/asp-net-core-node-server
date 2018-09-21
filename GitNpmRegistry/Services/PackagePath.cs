using System;
using System.Linq;

namespace GitNpmRegistry
{
    public class PackagePath
    {

        
        readonly UIProxyConfig config;

        public UIProxyConfig.ProxyConfig PackageConfig { get; }
        public string Package { get; }
        public string Tag { get; }

        public string Version => Tag.StartsWith("v") ? Tag.Substring(1) : Tag;

        // public string TarFile => $"{config.CachePath}\\git-npm\\{Package}\\tars\\t{Tag}.tar.gz";

        public string TarFile => $"{TagFolder}\\{Package}-{Version}.tgz";

        public string GitFolder => $"{config.CachePath}\\git-npm\\{Package}\\git";

        public string TagFolder => $"{config.CachePath}\\git-npm\\{Package}\\tag\\{Tag}";

        public string CachePath => config.CachePath;

        public PackagePath(UIProxyConfig config, string package)
        {
            this.config = config;
            var tokens = package.Split('@');
            if (tokens.Length == 1)
            {
                throw new HttpStatusException(402, "tag is missing");
            }

            string version = tokens[1];

            this.PackageConfig = config.Get(package);
            if (PackageConfig == null)
            {
                throw new HttpStatusException(404, $"No package found {package}");
            }

            this.Package = tokens[0];

            this.Tag = version;
            if (!this.Tag.StartsWith("v"))
            {
                this.Tag = "v" + this.Tag;
            }

        }

    }
}
