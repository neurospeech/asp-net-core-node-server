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
            [FromServices] string package,
            [FromServices] string path
            ) {

            var tokens = package.Split('@');
            if (tokens.Length == 1) {
                return new StatusCodeResult(402) {  };
            }
            var version = tokens[1];
            package = tokens[0];



        }

    }
}
