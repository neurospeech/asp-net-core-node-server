using System;
using System.Linq;

namespace NodeServer
{
    public class PackagePath
    {

        public string PrivateNpmUrl => $"{Options.NPMRegistry}{Package}/-/{ID}-{Version}.tgz";

        public readonly bool isPrivate;
        public readonly string Package;
        public readonly string Version;
        public readonly string Path;
        public readonly string TempRoot;

        // private readonly string npmUrlTemplate;

        public string ID => Package.Split("/").Last();

        public string Tag => $"v{Version}";

        public string TagFolder
                => $"{Options.TempFolder}\\git-npm\\{Package}\\tag\\{Tag}";
    
        public NodeServerOptions Options { get; }


        public PackagePath(
            NodeServerOptions options,
            (string, string, string) p,
            bool isPrivate)
        {
            this.Options = options;
            this.isPrivate = isPrivate;
            (this.Package, this.Version, this.Path) = p;
        }
    }
}
