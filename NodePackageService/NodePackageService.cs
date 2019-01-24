﻿using Microsoft.AspNetCore.Builder;
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
    public class NodePackageService
    {

        readonly IServiceProvider services;
        readonly IMemoryCache cache;
        readonly IEnumerable<PackagePath> privatePackages;
        public NodePackageServiceOptions Options { get; }

        public NodePackageService(
            IServiceProvider services,
            NodePackageServiceOptions options)
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

                var options = this.Options.NodeServicesOptions ?? new NodeServicesOptions(services)
                {
                    ProjectPath = pp.TagFolder,
                    NodeInstanceOutputLogger = services.GetService<ILogger<NodePackageService>>()
                };

                if (this.Options.EnvironmentVariables != null)
                {
                    if (options.EnvironmentVariables == null)
                    {
                        options.EnvironmentVariables = new Dictionary<string, string>();
                    }
                    foreach (var item in this.Options.EnvironmentVariables)
                    {
                        options.EnvironmentVariables[item.Key] = item.Value;
                    }
                }

                var s = NodeServicesFactory.CreateNodeServices(options);

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
