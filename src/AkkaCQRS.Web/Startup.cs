using System.Web.Http;
using AkkaCQRS.Core;
using AkkaCQRS.Web;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace AkkaCQRS.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Bootstrap.Initialize();
            app.UseWebApi(new HttpConfiguration(new HttpRouteCollection("~/api")));
        }
    }
}
