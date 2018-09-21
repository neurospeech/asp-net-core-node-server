using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Text;

namespace GitNpmRegistry
{
    public struct BuildResult {
        public bool Success;
        public string Log;
        public string Error;
    }

    public class BuildTask
    {

        readonly IHttpContextAccessor contextAccessor;
        readonly string path;
        readonly string package;
        readonly string version;
        readonly UIProxyConfig config;
        public BuildTask(
            UIProxyConfig config,
            IHttpContextAccessor contextAccessor,
            string package,
            string version,
            string path)
        {
            this.config = config;
            this.version = version;
            this.package = package;
            this.contextAccessor = contextAccessor;
            this.path = path;
        } 

        async Task UpdateDependencies()
        {
            string packageInfo = $"{path}\\package.json";

            string fileContents = await File.ReadAllTextAsync(packageInfo);

            var json = JObject.Parse(fileContents);

            var deps = json.GetValue("dependencies") as JObject;

            var localDeps = deps?.ToDictionary<string>();

            if (localDeps == null)
                return;

            foreach (var p in localDeps.ToList())
            {
                if (p.Value.StartsWithIgnoreCase("git+https://"))
                {

                    var req = contextAccessor.HttpContext.Request;
                    var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? 80);

                    var v = p.Value;

                    if (v.Contains("@"))
                    {
                        v = v.Split('@')[1];
                    }
                    else {
                        v = v.Split('#')[1];
                    }

                    uriBuilder.Path = $"npm/tar/{p.Key}@{v}/{p.Key}.tgz";

                    deps[p.Key].Replace(JToken.FromObject(uriBuilder.ToString()));
                }
            }

            await File.WriteAllTextAsync(packageInfo, json.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        async Task DisableWatch()
        {
            string packageInfo = $"{path}\\tsconfig.json";

            string fileContents = await File.ReadAllTextAsync(packageInfo);

            var json = JObject.Parse(fileContents);

            var co = json["compilerOptions"];

            co["watch"]?.Replace(JToken.FromObject(false));

            await File.WriteAllTextAsync(packageInfo, json.ToString(Newtonsoft.Json.Formatting.Indented));

        }


        public async Task Build(string command, TextWriter logger, CancellationToken token) {

            using (var batch = new TemporaryFile("bat", config.CachePath + "\\bat"))
            {

                await batch.AppendLines(command);

                var processTask = new ProcessTask(batch.File.FullName, this.path, token);

                var status = await processTask.RunAsync();

                if (status != 0) {
                    throw new InvalidOperationException(processTask.Error + "\r\n" + processTask.Log);
                }
            }
        }

        public async Task<string> RunAsync() {

            await UpdateDependencies();

            await DisableWatch();

            System.Threading.CancellationTokenSource ct = new System.Threading.CancellationTokenSource();


            DirectoryDelete(this.path + "\\node_modules");
            DirectoryDelete(this.path + "\\dist");

            using (StringWriter writer = new StringWriter())
            {

                await Build($"npm install", writer, ct.Token);
                await Build($"tsc", writer, ct.Token);
                await Build($"npm pack", writer, ct.Token);

                return writer.ToString();
            }

        }

        void DirectoryDelete(string directoryName)
        {
            if (Directory.Exists(directoryName)) {
                Directory.Delete(directoryName, true);
            }    
        }


    }
}
