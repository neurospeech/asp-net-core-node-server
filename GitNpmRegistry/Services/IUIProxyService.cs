using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUIProxyService
    {

        Task<JSAppModel> GetPackageConfigAsync(string package);

    }


    /// <summary>
    /// 
    /// </summary>
    public class UIProxyService : IUIProxyService
    {
        private UIProxyConfig config;
        private IMemoryCache cache;

        private string CDN;
        readonly AppCache<JSAppModel> jsAppModelCache;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="configuration"></param>
        /// <param name="httpClientProvider"></param>
        public UIProxyService(
            IMemoryCache cache,
            AppCache<JSAppModel> jsAppModelCache,
            UIProxyConfig configuration)
        {
            this.jsAppModelCache = jsAppModelCache;
            this.config = configuration;

            this.CDN = configuration.CDN;

            if (string.IsNullOrWhiteSpace(CDN)){
                CDN = null;
            }
            this.cache = cache;

        }

        public async Task<JSAppModel> GetPackageConfigAsync(string package)
        {
            var cachedModel = await jsAppModelCache.GetOrCreateLargeTTLAsync(package, async entry => {

                JSAppModel model = new JSAppModel();

                // var packageInfo = await this.GetAsync(package, "package.json");

                var currentPackage = config.Get(package);

                PackagePath pp = new PackagePath(config, package);

                string packageInfoPath = pp.TagFolder + "\\package.json";

                model.Package = currentPackage;

                string packageInfoText = await System.IO.File.ReadAllTextAsync(packageInfoPath);

                var obj = JObject.Parse(packageInfoText) as JObject;

                var deps = obj.GetValue("dependencies") as JObject;

                var localDeps = deps?.ToDictionary<string>() ?? new Dictionary<string, string>();

                localDeps[obj.GetValue("name").Value<string>()] = obj.GetValue("version").Value<string>();

                foreach (var p in localDeps)
                {
                    string pn = p.Key.ToLower();
                    string v = p.Value;
                    if (v.StartsWithIgnoreCase("file:"))
                    {
                        continue;
                    }
                    if (v.StartsWith("^"))
                    {
                        v = v.Substring(1);
                    }
                    else if (v.Contains("#v"))
                    {
                        v = v.Split("#v")[1];
                    }

                    string url = $"/ui/{pn}@{v}";

                    var pc = config.Get(pn, false);
                    if (pc == null)
                        continue;
                    if (pc.Source.EqualsIgnoreCase("git"))
                    {
                        if (!string.IsNullOrWhiteSpace(CDN))
                        {
                            url = $"//{CDN}/ui/{pn}@{v}";
                        }
                    }
                    else
                    {
                        url = $"//cdn.jsdelivr.net/npm/{pn}@{v}";
                    }


                    if (pn.EqualsIgnoreCase("web-atoms-amd-loader")) {
                        model.UMDScriptSrc = $"{url}/umd.js";
                    }

                    if (pn.EqualsIgnoreCase("reflect-metadata")) {
                        url = $"{url}/Reflect.js";
                    }
                    model.Modules[pn] = url;
                }

                return model;

            });

            return cachedModel.Clone();
        }
    }
}
