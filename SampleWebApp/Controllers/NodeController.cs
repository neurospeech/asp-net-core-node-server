using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace SampleWebApp.Controllers
{
    [Route("n-api/{*path}")]
    public class NodeController: Controller
    {

        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public async Task<IActionResult> Run(
            [FromRoute] string path,
            [FromServices] NodeServer.NodeServer nodeServer)
        {
            var p = await nodeServer.GetInstalledPackageAsync(path);
            string body = null;
            if (Request.ContentLength > 0)
            {
                using(var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }
            }
            var q = new JObject();
            foreach(var item in Request.Query)
            {
                string s = string.Join(" ",item.Value);
                q.Add(item.Key, JValue.CreateString(s));
            }
            string result = await p.NodeServices.InvokeAsync<string>(
                p.Path.Path,
                JsonConvert.SerializeObject(new {
                    body = body,
                    query = q
                }));
            return Content(result, "application/json");
        }

    }
}
