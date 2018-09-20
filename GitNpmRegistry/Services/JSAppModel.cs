using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitNpmRegistry
{

    public class JSAppModel
    {

        public string Title { get; set; }

        public List<Meta> Meta { get; }
            = new List<Meta>(){
                new Meta{ Name = "viewport", Content = "width=device-width" }
            };

        public string UMDScriptSrc { get; set; }

        public string SystemJSSrc { get; set; }

        public Dictionary<string, string> Modules { get; }
            = new Dictionary<string, string>();

        public string StartScript { get; set; }
        public UIProxyConfig.ProxyConfig Package { get; set; }
        public string Platform { get; set; }

        public JSAppModel Clone()
        {
            var copy = new JSAppModel
            {
                Title = Title,
                UMDScriptSrc = UMDScriptSrc,
                SystemJSSrc = SystemJSSrc,
                Package = Package,
                StartScript = StartScript
            };
            foreach (var item in Modules)
            {
                copy.Modules[item.Key] = item.Value;
            }
            copy.Meta.AddRange(Meta);
            return copy;
        }
    }

    public class Meta
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string HttpEquiv { get; set; }

        public string Property { get; set; }

        public override string ToString()
        {
            var c = Content != null ? $"content=\"{Content}\"" : "";
            var n = Name != null ? $"name=\"{Name}\"" : "";
            var h = HttpEquiv != null ? $"http-equiv=\"{HttpEquiv}\"" : "";
            var p = Property != null ? $"property=\"{Property}\"" : "";
            return $"<meta {n} {h} {p} {c}/>";
        }
    }

    public class JSAppResult : IActionResult
    {


        private string package;

        private string path;

        public JSAppResult(string path)
        {

            var tokens = path.Split('/', 2);
            if (tokens.Length > 1)
            {
                this.path = tokens[1];
                this.package = tokens[0];
            }
            else
            {
                throw new ArgumentException($"Invalid {path}, path does not include package");
            }

        }



        public async Task ExecuteResultAsync(ActionContext context)
        {

            var services = context.HttpContext.RequestServices;

            var proxyService = services.GetRequiredService<IUIProxyService>();

            if (!package.Contains("@"))
            {
                var c = proxyService.GetConfig(package);
                package = $"{package}@{c.Version}";
            }


            var content = "<!DOCTYPE html><html lang=\"en\">" +
                "<head>" +
                    "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">" +
                    "<title>Gmail</title>" +
                    "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">" +
                    // "<meta name=\"google\" value=\"notranslate\">" +
                    "<meta name=\"application-name\" content=\"800 Casting\">" +
                    "<meta name=\"description\" content=\"Enterprise Resource Planning\">" +
                "</head>" +
                "<body>" +
                "<div style=\"position: absolute; left: 0; right: 0; bottom: 0; top: 0; margin: auto\" >Loading Application ... </div>" +
                $"<script type=\"text/javascript\" src=\"/ui/{package}/{path}.js\"></script>" +
                "</body>";

            var buffer = System.Text.Encoding.UTF8.GetBytes(content);

            await context.HttpContext.Response.Body.WriteAsync(buffer, 0, buffer.Length);

        }


    }
}
