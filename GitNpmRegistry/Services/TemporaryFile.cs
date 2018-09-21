﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public class TemporaryFile : IDisposable
    {
        public FileInfo File { get; set; }

        public TemporaryFile(string ext, string tempRoot = null)
        {
            tempRoot = tempRoot ?? Path.GetTempPath();
            File = new FileInfo($"{tempRoot}\\tmp-bat-{Guid.NewGuid().ToString()}.{ext ?? "bat"}");
            // if(File.Directory.Exists)
        }

        public async Task AppendLines(params string[] lines) {
            using (var s = File.AppendText()) {
                foreach (var line in lines) {
                    await s.WriteLineAsync(line);
                }
            }
        }

        public void Dispose()
        {
            File.Delete();
        }
    }
        
}
