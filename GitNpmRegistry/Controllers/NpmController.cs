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

        [HttpGet("uiv/{package}/{*path}")]
        public async Task<IActionResult> GetUIView(
            [FromServices] UIProxyConfig config,
            [FromServices] IGitService git,
            [FromServices] IUIProxyService proxyService,
            [FromRoute] string package,
            [FromRoute] string path,
            [FromQuery] bool designMode = false,
            [FromQuery] string platform = "web"
            ) {
            if (string.IsNullOrWhiteSpace(path))
            {
                return NotFound();
            }

            var packageConfig = await proxyService.GetPackageConfigAsync(package);

            if (!path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                path = $"{path}.js";
            }

            packageConfig.StartScript = $"UMD.loadView(\"{packageConfig.Package.Package}/{path}\", {(designMode ? "true" : "false")})";
            packageConfig.Platform = platform;
            string isWeb = (string.IsNullOrWhiteSpace(platform) || platform.EqualsIgnoreCase("web")) ? "Web" : "";
            return View($"~/Views/Npm/{isWeb}JSApplication.cshtml", packageConfig);
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
