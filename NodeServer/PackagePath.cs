using System;
using System.Linq;

namespace NodeServer
{
    public class PackagePath
    {

        public string PrivateNpmUrl => npmUrlTemplate
                            .Replace("{package}", Package)
                            .Replace("{id}", this.ID)
                            .Replace("{version}", Version);

        public readonly bool isPrivate;
        public readonly string Package;
        public readonly string Version;
        public readonly string Path;
        public readonly string TempRoot;

        private readonly string npmUrlTemplate;

        public string ID => Package.Split("/").Last();

        public string Tag => $"v{Version}";

        public string TagFolder => $"{TempRoot}\\git-npm\\{Package}\\tag\\{Tag}";


        public PackagePath(
            (string, string, string) p,
            bool isPrivate,
            string tempRoot,
            string npmUrlTemplate)
        {
            this.TempRoot = tempRoot;
            this.npmUrlTemplate = npmUrlTemplate;
            this.isPrivate = isPrivate;
            (this.Package, this.Version, this.Path) = p;
        }
    }
}
