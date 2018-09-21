using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry.Controllers
{


    [Route("npm")]
    public class NpmController: Controller
    {
        [HttpGet("tar/{package}/{file}")]
        public async Task<IActionResult> Tar(
            [FromServices] UIProxyConfig config,
            [FromServices] IGitService git,
            [FromRoute] string package
            ) {

            PackagePath pp = new PackagePath(config, package);

            await git.BuildTag(pp);

            // TarGZTask task = new TarGZTask(pp.CachePath, pp.TarFile, pp.TagFolder);

            // await task.CreateAsync();

            return File(System.IO.File.OpenRead(pp.TarFile), MimeKit.MimeTypes.GetMimeType(pp.TarFile));

        }

        [HttpGet("build/{package}")]
        public async Task<IActionResult> Build(
            [FromServices] UIProxyConfig config,
            [FromServices] IGitService git,
            [FromRoute] string package
            ) {

            PackagePath pp = new PackagePath(config, package);

            await git.BuildTag(pp);

            return Ok();
        }

        [HttpGet("package/{package}/{*path}")]
        public async Task<IActionResult> Get(
            [FromServices] UIProxyConfig config,
            [FromServices] IGitService git,
            [FromRoute] string package,
            [FromRoute] string path
            ) {

            PackagePath pp = new PackagePath(config, package);

            await git.BuildTag(pp);

            // deliver file...

            string filePath = pp.TagFolder + "\\" + path;

            return File(System.IO.File.OpenRead(filePath), MimeKit.MimeTypes.GetMimeType(filePath));
        }

    }
}
