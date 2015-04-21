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
using Newtonsoft.Json.Serialization;
using Owin;
using Serilog;

[assembly: OwinStartup(typeof(Startup))]

namespace AkkaCQRS.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Bootstrap.Initialize();

            ConfigureLogger(app);
            ConfigureStaticFiles(app);
            ConfigureWebApi(app);
        }

        private void ConfigureLogger(IAppBuilder app)
        {
            var logger = new LoggerConfiguration().WriteTo.ColoredConsole().MinimumLevel.Debug().CreateLogger();
            Serilog.Log.Logger = logger;
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
            httpConfig.MapHttpAttributeRoutes();
            httpConfig.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            var jsonSettings = httpConfig.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            app.UseWebApi(httpConfig);
        }
    }
}
