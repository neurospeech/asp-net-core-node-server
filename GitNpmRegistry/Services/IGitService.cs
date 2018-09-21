using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public interface IGitService
    {
        Task<bool> BuildTag(UIProxyConfig.ProxyConfig config, string tag);
    }

    public class GitService : IGitService
    {
        readonly UIProxyConfig config;
        public GitService(UIProxyConfig config)
        {
            this.config = config;
        }

        private async Task Download(UIProxyConfig.ProxyConfig model, string vtag) {

            string root = $"{config.CachePath}/{model.Package}@{vtag}";

            if (Directory.Exists(root)) {
                return;
            }

            string gitPath = $"{config.CachePath}/{model.Package}/git";

            LibGit2Sharp.Handlers.CredentialsHandler credentialProvider = (url, usernameFromUrl, types)
                           => new UsernamePasswordCredentials
                           {
                               Username = model.Username,
                               Password = string.IsNullOrWhiteSpace(model.Password) ? "" : model.Password
                           };
            DirectoryInfo dir = new DirectoryInfo(gitPath);
            if (!dir.Exists)
            {
                dir.Create();

                CloneOptions clone = new CloneOptions();
                clone.CredentialsProvider = credentialProvider;

                clone.Checkout = false;
                clone.IsBare = false;

                Repository.Clone(model.Url, dir.FullName, clone);
            }


            using (var repo = new Repository(dir.FullName))
            {

                if (!repo.Tags.Any(x => x.FriendlyName == vtag))
                {

                    foreach (Remote remote in repo.Network.Remotes)
                    {
                        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        Commands.Fetch(repo, remote.Name, refSpecs, new FetchOptions
                        {
                            CredentialsProvider = credentialProvider,
                            TagFetchMode = TagFetchMode.All
                        }, "");
                    }
                }

                var tag = repo.Tags.FirstOrDefault(x => x.FriendlyName == vtag);
                if (tag == null)
                    throw new HttpStatusException(404, "Tag not found");

                var commit = repo.Lookup<Commit>(tag.Target.Id);

                var tempRoot = new DirectoryInfo( $"{config.CachePath}\\{Guid.NewGuid().ToString()}");

                try
                {

                    await ExtractTree(commit.Tree, tempRoot);

                    // do build here....

                    BuildTask task = new BuildTask();
                    await task.RunAsync(tempRoot.FullName);

                    tempRoot.MoveTo(root);

                } catch {
                    // tempRoot.Delete();
                    throw;
                } 

            }

        }
        async Task ExtractTree(Tree tree, DirectoryInfo tempRoot)
        {
            foreach (var treeEntry in tree)
            {
                var localFile = new FileInfo($"{tempRoot.FullName}\\{treeEntry.Path}");

                if (!(treeEntry.Target is Blob blob))
                {
                    if (treeEntry.Target is Tree t)
                    {
                        await ExtractTree(t, tempRoot);
                    }
                    continue;
                }
                // var blob = (Blob)treeEntry.Target;

                if (!localFile.Directory.Exists)
                {
                    localFile.Directory.Create();
                }
                using (var fs = System.IO.File.OpenWrite(localFile.FullName))
                {
                    using (var s = blob.GetContentStream())
                    {
                        await s.CopyToAsync(fs);
                    }
                }
            }

        }

        private Dictionary<string, BuildInfo> Locks = new Dictionary<string, BuildInfo>();

        public async Task<bool> BuildTag(UIProxyConfig.ProxyConfig up, string tag)
        {
            string root = $"{config.CachePath}\\{up.Package}@{tag}";

            if (Directory.Exists(root)) {
                return true;
            }

            BuildInfo lockObject = null;

            lock (this) {
                if (!Locks.TryGetValue(root, out lockObject)) {
                    lockObject = new BuildInfo();
                    Locks[root] = lockObject;
                }
            }

            if (lockObject.IsBuilding)
                return false;

            await Download(up, tag);

            lockObject.IsBuilding = false;

            lock (this) {
                Locks.Remove(root);
            }

            return true;
        }
    }

    public class BuildInfo {

        private bool _IsBuilding;
        public bool IsBuilding
        {
            get
            {
                lock (this)
                {
                    return this._IsBuilding;
                }
            }
            set
            {
                lock (this)
                {
                    this._IsBuilding = value;
                }
            }
        }

    }
}
