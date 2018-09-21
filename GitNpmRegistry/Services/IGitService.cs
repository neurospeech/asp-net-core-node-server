using LibGit2Sharp;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public interface IGitService
    {
        Task<bool> BuildTag(PackagePath pp);
    }

    public class GitService : IGitService
    {
        readonly UIProxyConfig config;
        readonly IHttpContextAccessor contextAccessor;

        public GitService(UIProxyConfig config, IHttpContextAccessor contextAccessor)
        {
            this.config = config;
            this.contextAccessor = contextAccessor;
        }

        private async Task Download(PackagePath pp) {

            var root = new DirectoryInfo(pp.TagFolder);

            if (root.Exists) {
                return;
            }

            if (!root.Parent.Exists) {
                root.Parent.Create();
            }

            string gitPath = pp.GitFolder;

            var model = pp.PackageConfig;

            var vtag = pp.Tag;

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


                    BuildTask task = new BuildTask(contextAccessor, model.Package, vtag, tempRoot.FullName);
                    await task.RunAsync();


                    tempRoot.MoveTo(root.FullName);

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

        public async Task<bool> BuildTag(PackagePath pp)
        {
            string root = pp.TagFolder;

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

            await Download(pp);

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
