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

        [HttpGet("{package}/{path*}")]
        public async Task<IActionResult> Get(
            [FromServices] UIProxyConfig config,
            [FromServices] IGitService git,
            [FromQuery] string package,
            [FromQuery] string path
            ) {

            var tokens = package.Split('@');
            if (tokens.Length == 1) {
                return new StatusCodeResult(402) {  };
            }
            var version = tokens[1];

            var up = config.Get(package);
            if (up == null) {
                throw new HttpStatusException(404, $"No package found {package}");
            }

            package = tokens[0];

            string tag = version;
            if (!tag.StartsWith("v"))
            {
                tag = "v" + tag;
            }


            await git.BuildTag(up, tag);

            return Ok();
        }

    }
}
