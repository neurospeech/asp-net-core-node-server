using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public class TarGZTask
    {

        readonly FileInfo tarFileName;
        readonly string sourceDirectory;
        readonly string tempPath;
        public TarGZTask(string tempPath, string tarFileName, string sourceDirectory)
        {
            this.tempPath = $"{tempPath ?? Path.GetTempPath()}\\git-npm\\tars";
            this.sourceDirectory = sourceDirectory;
            this.tarFileName = new FileInfo( tarFileName);
        }

        public Task CreateAsync() {

            if (tarFileName.Exists) {
                return Task.CompletedTask;
            }

            var tmp = new FileInfo( $"{tempPath}\\{Guid.NewGuid().ToString()}.tar.gz");

            if (!tmp.Directory.Exists)
                tmp.Directory.Create();

            CreateTarGZ(tmp.FullName, sourceDirectory);

            if (!tarFileName.Directory.Exists)
                tarFileName.Directory.Create();
            tmp.MoveTo(tarFileName.FullName);

            return Task.CompletedTask;
        }

        private void CreateTarGZ(string tgzFilename, string sourceDirectory)
        {
            Stream outStream = File.Create(tgzFilename);
            Stream gzoStream = new GZipOutputStream(outStream);
            TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

            // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
            // and must not end with a slash, otherwise cuts off first char of filename
            // This is scheduled for fix in next release
            tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
            if (tarArchive.RootPath.EndsWith("/"))
                tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

            AddDirectoryFilesToTar(tarArchive, sourceDirectory, true);

            tarArchive.Close();
        }

        private void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
        {

            if (sourceDirectory.EndsWith("/node_modules") || sourceDirectory.EndsWith("\\node_modules"))
                return;

            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToTar(tarArchive, directory, recurse);
            }
        }
    }
}
