using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NodeServer.Tasks
{
    public class TarGZExtractTask : IDisposable
    {

        PackagePath packagePath;
        readonly string tempRoot;
        public TarGZExtractTask(PackagePath pp, string tempRoot)
        {
            this.tempRoot = tempRoot;
            this.packagePath = pp;

        }

        public async Task RunAsync()
        {
            try
            {
                string url = this.packagePath.PrivateNpmUrl;

                using (var client = new HttpClient())
                {
                    using (var stream = await client.GetStreamAsync(url))
                    {
                        using (var ungzStream = new GZipInputStream(stream))
                        {
                            using (var tar = TarArchive.CreateInputTarArchive(ungzStream))
                            {
                                // tar.ExtractContents(packagePath.TagFolder);

                                var temp = Directory.CreateDirectory(tempRoot + "\\tmp\\" + Guid.NewGuid().ToString());

                                tar.ExtractContents(temp.FullName);

                                Directory.Move(temp.FullName + "\\package", packagePath.TagFolder);

                                temp.Delete(true);

                            }
                        }

                    }
                }
            }
            catch
            {
                Directory.Delete(packagePath.TagFolder, true);
                throw;
            }
        }

        public void Dispose()
        {

        }
    }
}
