using System.IO;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AkkaCQRS.Core;
using AkkaCQRS.Web;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
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
            ConfigureMvc(RouteTable.Routes);
            ConfigureWebApi(app);
            ConfigureSignalR(app);
            ConfigureBundles(BundleTable.Bundles);
        }

        private void ConfigureBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/js/app")
                .IncludeDirectory("~/Scripts/app/", "*.js", true));

            bundles.Add(new ScriptBundle("~/bundles/js/app/init")
                .Include("~/Scripts/Main.js"));
        }

        private void ConfigureSignalR(IAppBuilder app)
        {
            app.MapSignalR();
        }

        private void ConfigureLogger(IAppBuilder app)
        {
            var logger = new LoggerConfiguration().WriteTo.ColoredConsole().MinimumLevel.Debug().CreateLogger();
            Log.Logger = logger;
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

        private static void ConfigureMvc(RouteCollection routes)
        {
            AreaRegistration.RegisterAllAreas();
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "Default", 
                url: "{controller}/{action}/{id}", 
                defaults: new {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional
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
