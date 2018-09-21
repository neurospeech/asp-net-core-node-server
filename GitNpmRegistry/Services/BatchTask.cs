using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{
    public struct BuildResult {
        public bool Success;
        public string Log;
        public string Error;
    }

    public class BuildTask
    {

        public async Task<BuildResult> RunAsync(string path) {

            System.Threading.CancellationTokenSource ct = new System.Threading.CancellationTokenSource();

            using (var tmp = new TemporaryFile("bat"))
            {

                await tmp.AppendLines($"npm install",
                "tsc tsconfig.config");

                var processTask = new ProcessTask(tmp.File.FullName, ct.Token);

                var result = await processTask.RunAsync();

                return new BuildResult {
                    Success = result == 0,
                    Log = processTask.Log,
                    Error = processTask.Error
                };
                
            }

        }

    }
}
