using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Http;
using AkkaCQRS.Core;
using AkkaCQRS.Web;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.Infrastructure;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace AkkaCQRS.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Bootstrap.Initialize();

            ConfigureStaticFiles(app);
            ConfigureWebApi(app);
        }

        private void ConfigureStaticFiles(IAppBuilder app)
        {
            var publicPath = Path.Combine(HttpContext.Current.Server.MapPath("~/"), "public");
            app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem(publicPath)
            });
        }

        private static void ConfigureWebApi(IAppBuilder app)
        {
            var httpConfig = new HttpConfiguration();
            httpConfig.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional});
            app.UseWebApi(httpConfig);
        }
    }
}
