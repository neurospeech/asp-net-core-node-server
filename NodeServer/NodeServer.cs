using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NodeServer.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace NodeServer
{
    public class NodeServerOptions
    {
        /// <summary>
        /// Folder where node packages will be downloaded
        /// </summary>
        public string TempFolder { get; set; } = "d:\\temp\\ns-npm";


        /// <summary>
        /// NPM Registry used to download packages
        /// </summary>
        public string NPMRegistry { get; set; }


        /// <summary>
        /// White list of packages to execute
        /// </summary>
        public string[] PrivatePackages { get; set; }

        /// <summary>
        /// Time to live, after which NodeServer will dispose
        /// </summary>
        public TimeSpan TTL { get; set; } = TimeSpan.FromHours(1);
    }

    public class NodeServer
    {

        readonly IServiceProvider services;
        readonly IMemoryCache cache;
        readonly IEnumerable<PackagePath> privatePackages;
        public NodeServerOptions Options { get; }

        public NodeServer(
            IServiceProvider services,
            NodeServerOptions options)
        {
            var reg = options.NPMRegistry.TrimEnd('/') + "/";
            this.Options = options;
            this.Options.NPMRegistry = reg;
            this.privatePackages = options.PrivatePackages.Select( x => {
                return new PackagePath(options, x.ParseNPMPath(), true);
            } );
            this.cache = services.GetService<IMemoryCache>();
            this.services = services;            
        }

        public PackagePath ParsePath(string sp)
        {
            var (package, version, path) = sp.ParseNPMPath();

            var existing = this.privatePackages.FirstOrDefault(x => x.Package == package);

            // replace version... if it is empty
            if(string.IsNullOrWhiteSpace(version))
            {
                if (existing != null)
                {
                    version = existing.Version;
                }
            }

            return new PackagePath(this.Options, (package, version, path), existing != null);
        }

        public async Task DownloadAsync(PackagePath packagePath)
        {
            if (!Directory.Exists(packagePath.TagFolder))
            {
                using(var tgz = new PackageInstallTask(packagePath))
                {
                    await tgz.RunAsync();
                }
            }

        }

        private Dictionary<string, bool> loading = new Dictionary<string, bool>();

        public async Task InstallAsync(PackagePath packagePath)
        {

            bool wait = false;

            while (true) {
         
                lock(loading)
                {
                    if(loading.ContainsKey(packagePath.TagFolder))
                    {
                        wait = true;
                    } else
                    {
                        wait = false;
                        loading[packagePath.TagFolder] = true;
                        break;
                    }
                }
                if (wait)
                {
                    await Task.Delay(1000);
                }
            }

            try
            {

                if (!Directory.Exists(packagePath.TagFolder))
                {
                    await DownloadAsync(packagePath);
                }

            } finally {
                lock (loading)
                {
                    loading.Remove(packagePath.TagFolder);
                }
            }
        }

        public Task<NodePackage> GetInstalledPackageAsync(string path)
        {
            var pp = this.ParsePath(path);
            return cache.GetOrCreateAsync<NodePackage>(pp.Package + "@" + pp.Version, async entry => {

                entry.SlidingExpiration = TimeSpan.FromHours(1);

                await InstallAsync(pp);

                var s = NodeServicesFactory.CreateNodeServices(new NodeServicesOptions(services) {
                    ProjectPath = pp.TagFolder,
                    NodeInstanceOutputLogger = services.GetService<ILogger<NodeServer>>()
                });

                entry.RegisterPostEvictionCallback((x1, x2, x3, x4) => {
                    try
                    {
                        s.Dispose();
                    } catch(Exception ex)
                    {
                        Trace.WriteLine(ex);
                    }
                });

                return new NodePackage {
                    Path = pp,
                    NodeServices = s
                };

            });

        }



    }

    public class NodePackage
    {
        public PackagePath Path;
        public INodeServices NodeServices;
    }
}
