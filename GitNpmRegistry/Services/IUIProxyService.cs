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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        Task<FileContentResult> GetAsync(string package, string path, string version = null);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        UIProxyConfig.ProxyConfig GetConfig(string package);


        Task<JSAppModel> GetPackageConfigAsync(string package);

    }

    /// <summary>
    /// 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class UIProxyServiceExtension {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void AddUIProxyService(this IServiceCollection services, UIProxyConfig config) {
            services.AddSingleton<IUIProxyService, UIProxyService>( sp => 
                new UIProxyService(
                    sp.GetRequiredService<IMemoryCache>(),
                    sp.GetRequiredService<AppCache<JSAppModel>>(),
                    config, 
                    sp.GetRequiredService<IHttpClientProvider>()));
        }

        /// <summary>
        /// 
        /// </summary>
        public class UIProxyService : IUIProxyService
        {
            private UIProxyConfig config;
            private IMemoryCache cache;
            private IHttpClientProvider httpClientProvider;

            private string CDN;
            readonly AppCache<JSAppModel> jsAppModelCache;

            /// <summary>
            /// 
            /// </summary>
            public DirectoryInfo TempPath { get; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="cache"></param>
            /// <param name="configuration"></param>
            /// <param name="httpClientProvider"></param>
            public UIProxyService(
                IMemoryCache cache,
                AppCache<JSAppModel> jsAppModelCache,
                UIProxyConfig configuration, 
                IHttpClientProvider httpClientProvider)
            {
                this.jsAppModelCache = jsAppModelCache;
                string cacheName = "HybridJet-UI-Proxy-16";
                this.httpClientProvider = httpClientProvider;
                this.config = configuration;

                this.CDN = configuration.CDN;

                if (string.IsNullOrWhiteSpace(CDN)){
                    CDN = null;
                }

                if(!string.IsNullOrWhiteSpace(configuration.CachePath))
                {
                    TempPath = new DirectoryInfo($"d:\\temp\\{cacheName}");
                    if (!TempPath.Exists)
                    {
                        TempPath.Create();
                    }

                }
                else
                {
                    TempPath = new DirectoryInfo(System.IO.Path.GetTempPath() + Path.DirectorySeparatorChar + cacheName);
                    if (!TempPath.Exists)
                    {
                        TempPath.Create();
                    }
                }

                this.cache = cache;

            }



            /// <summary>
            /// 
            /// </summary>
            /// <param name="package"></param>
            /// <param name="path"></param>
            /// <param name="version"></param>
            /// <returns></returns>
            public async Task<FileContentResult> GetAsync(string package, string path, string version = null)
            {
                if (package.Contains("@")) {
                    var tokens = package.Split("@");
                    package = tokens[0];
                    if (version == null) {
                        version = tokens[1];
                    }
                }
                var c = config.Get(package);
                var source = c.Source;

                FileInfo file;
                string type;

                if (version == null) {
                    version = c.Version;
                }

                if (source.Equals("git", StringComparison.OrdinalIgnoreCase))
                {
                    type = MimeTypes.GetMimeType(path);
                    file = await GetAsync($"{source}/{package}@{version}/{path}", s => DownloadGitAsync(c, path, s, version));
                }
                else
                {
                    file = await GetAsync($"{source}/{package}@{version}/{path}", s => DownloadNpmAsync($"{package}@{version}", path, s));
                    type = MimeTypes.GetMimeType(file.FullName);

                    if (type.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                    {

                        // we do not want to serve HTML from NPM as anyone can 
                        // inject html
                        type = "text/plain";
                    }

                }

                return await cache.GetOrCreateAsync($"ui-proxy:{package}@{version}/{path}", async ci =>
                {
                    try
                    {
                        ci.SetSlidingExpiration(TimeSpan.FromMinutes(60));
                        return new FileContentResult(await File.ReadAllBytesAsync(file.FullName), type);
                    }
                    catch {
                        ci.SetAbsoluteExpiration(TimeSpan.FromSeconds(-69));
                        throw;
                    }
                });
            }

            private async Task<FileInfo> GetAsync(string path, Func<Stream, Task> original)
            {
                var file = new FileInfo(TempPath.FullName + Path.DirectorySeparatorChar + path);
                if (!file.Exists)
                {
                    try
                    {
                        if (!file.Directory.Exists)
                        {
                            file.Directory.Create();
                        }
                        using (var os = System.IO.File.OpenWrite(file.FullName))
                        {
                            await original(os);
                        }
                    }
                    catch
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch {
                        }
                        throw;
                    }
                }

                return file;
            }

            private async Task DownloadNpmAsync(string package, string path, Stream s)
            {
                try
                {
                    var client = httpClientProvider.HttpClient;
                    using (var ss = await client.GetStreamAsync($"https://cdn.jsdelivr.net/npm/{package}/{path}"))
                    {
                        await ss.CopyToAsync(s);
                    }
                }
                catch (Exception ex) {
                    throw new InvalidOperationException($"Failed to download {package}/{path}", ex);
                }
            }

            private async Task DownloadGitAsync(UIProxyConfig.ProxyConfig c, string path, Stream s, string version)
            {
                var client = httpClientProvider.HttpClient;
                version = version ?? c.Version;
                if (!version.StartsWith("v"))
                {
                    version = "v" + version;
                }

                var url = $"https://pdf-img.800casting.com/git/show";
                FormUrlEncodedContent content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]{
                    new KeyValuePair<string, string>("id",c.Package),
                    new KeyValuePair<string, string>("url",c.Url),
                    new KeyValuePair<string, string>("username",c.Username),
                    new KeyValuePair<string, string>("password",c.Password),
                    new KeyValuePair<string, string>("Path",path),
                    new KeyValuePair<string, string>("Tag",version)
                    });

                var isHtml = MimeKit.MimeTypes.GetMimeType(path).EqualsIgnoreCase("text/html");

                using (var response = await client.PostAsync(url, content))
                {
                    // response.EnsureSuccessStatusCode();

                    if (!response.IsSuccessStatusCode) {
                        var result = await response.Content.ReadAsStringAsync();
                        throw new HttpStatusException((int)response.StatusCode, result);
                    }

                    if (!isHtml)
                    {
                        using (var ss = await response.Content.ReadAsStreamAsync())
                        {
                            await ss.CopyToAsync(s);
                        }
                        return;
                    }

                    var text = await response.Content.ReadAsStringAsync();

                    var package = await GetAsync(c.Package, "package.json", version);

                    var obj = JObject.Parse(System.Text.Encoding.UTF8.GetString(package.FileContents)) as JObject;

                    var deps = obj.GetValue("dependencies") as JObject;

                    var localDeps = deps?.ToDictionary<string>() ?? new Dictionary<string, string>();

                    //foreach (var n in config.NPM) {
                    //    localDeps[n.Package] = n.Version;
                    //}

                    // localDeps[c.Package] = c.Version;

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


                        text = text.Replace($"/ui/{pn}/", $"/ui/{pn}@{v}/", StringComparison.OrdinalIgnoreCase);

                        if (CDN == null)
                            continue;

                        var pc = config.Get(pn, false);
                        if (pc == null)
                            continue;
                        if (pc.Source.EqualsIgnoreCase("git"))
                        {
                            text = text.Replace($"\"/ui/{pn}@{v}", $"\"//{CDN}/ui/{pn}@{v}");
                            text = text.Replace($"&quot;/ui/{pn}@{v}", $"&quot;//{CDN}/ui/{pn}@{v}");
                        }
                        else
                        {
                            text = text.Replace($"\"/ui/{pn}@{v}", $"\"//cdn.jsdelivr.net/npm/{pn}@{v}");
                            text = text.Replace($"&quot;/ui/{pn}@{v}", $"&quot;//cdn.jsdelivr.net/npm/{pn}@{v}");
                            
                        }
                    }

                    var bytes = System.Text.Encoding.UTF8.GetBytes(text);

                    await s.WriteAsync(bytes, 0, bytes.Length);
                }


            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="package"></param>
            /// <returns></returns>
            public UIProxyConfig.ProxyConfig GetConfig(string package)
            {
                return config.Get(package);
            }

            public async Task<JSAppModel> GetPackageConfigAsync(string package)
            {
                var cachedModel = await jsAppModelCache.GetOrCreateLargeTTLAsync(package, async entry => {

                    JSAppModel model = new JSAppModel();

                    var packageInfo = await this.GetAsync(package, "package.json");

                    var currentPackage = config.Get(package);

                    model.Package = currentPackage;

                    string packageInfoText = System.Text.Encoding.UTF8.GetString(packageInfo.FileContents);


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
}
