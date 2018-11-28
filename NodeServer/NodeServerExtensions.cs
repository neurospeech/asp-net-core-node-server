using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace NodeServer
{
    public static class NodeServerExtensions
    {

        public static void AddNodeServer(this IServiceCollection services,
            NodeServerOptions options)
        {
            services.AddSingleton(sp => new NodeServer(sp, options));
        }

        public static IApplicationBuilder UseUIViews(this IApplicationBuilder app, string route = "uiv/")
        {

            app.Use(async (context, next) =>
            {

                HttpRequest request = context.Request;
                if (!request.Method.EqualsIgnoreCase("GET"))
                {
                    await next();
                    return;
                }

                PathString path = request.Path;
                if (!path.HasValue || !path.Value.StartsWithIgnoreCase(route))
                {
                    await next();
                    return;
                }

                IHeaderDictionary headers = context.Response.Headers;
                headers.Add("access-control-allow-origin", "*");
                headers.Add("access-control-expose-headers", "*");
                headers.Add("access-control-allow-methods", "*");
                headers.Add("access-control-allow-headers", "*");
                headers.Add("access-control-max-age", TimeSpan.FromDays(30).TotalSeconds.ToString());

                var nodeServer = context.RequestServices.GetService<NodeServer>();

                string sp = path.Value.Substring(4);

                PackagePath packagePath = nodeServer.ParsePath(sp);

                await nodeServer.DownloadAsync(packagePath);
                
                // get file content...
            });

            return app;
        }

    }
}
