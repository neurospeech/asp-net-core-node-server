using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public interface IGitService
    {
        Task BuildTag(UIProxyConfig.ProxyConfig config, string tag);
    }

    public class GitService : IGitService
    {
        readonly UIProxyConfig config;
        public GitService(UIProxyConfig config)
        {
            this.config = config;
        }

        public Task BuildTag(UIProxyConfig.ProxyConfig up, string tag)
        {

            string path = $"{config.CachePath}\\{up.Package}@{tag}";

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            throw new NotImplementedException();
        }
    }
}
