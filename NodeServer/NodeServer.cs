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

namespace NodeServer
{
    public class NodeServerOptions
    {
        public string TempFolder { get; set; } = "d:\\temp\\ns-npm";

        public string NPMRegistry { get; set; }

        public string[] PrivatePackages { get; set; }
    }

    public class NodeServer
    {

        readonly IServiceProvider services;
        readonly IMemoryCache cache;
        readonly IEnumerable<PackagePath> privatePackages;
        readonly string tempFolder;
        readonly string npmUrlTemplate;
        string registry;

        public NodeServer(
            IServiceProvider services,
            NodeServerOptions options)
        {
            this.tempFolder = options.TempFolder;
            var reg = options.NPMRegistry.TrimEnd('/') + "/";
            this.npmUrlTemplate =  reg + "{package}/-/{id}-{version}.tgz";
            this.registry = reg;
            this.privatePackages = options.PrivatePackages.Select( x => {
                return new PackagePath(x.ParseNPMPath(), true, tempFolder, this.npmUrlTemplate);
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

            return new PackagePath((package, version, path), existing != null, tempFolder, this.npmUrlTemplate);
        }

        public async Task DownloadAsync(PackagePath packagePath)
        {
            if (!Directory.Exists(packagePath.TagFolder))
            {
                using(var tgz = new TarGZExtractTask(packagePath, packagePath.TempRoot))
                {
                    await tgz.RunAsync();
                }
            }

        }

        public async Task InstallAsync(PackagePath packagePath)
        {
            await DownloadAsync(packagePath);

            if (!Directory.Exists($"{packagePath.TagFolder}\\node_modules")) {
                using (var batch = new TemporaryFile("bat", packagePath.TempRoot + "\\tmp\\bat"))
                {
                    System.Threading.CancellationTokenSource ct = new System.Threading.CancellationTokenSource();

                    await batch.AppendLines("npm install --registry " + this.registry);

                    var processTask = new ProcessTask(batch.File.FullName, packagePath.TagFolder , ct.Token);

                    var status = await processTask.RunAsync();

                    if (status != 0)
                    {
                        throw new InvalidOperationException(processTask.Error + "\r\n" + processTask.Log);
                    }
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
