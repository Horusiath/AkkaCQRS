using System;
using System.Web.Http;
using Akka.Actor;
using AkkaCQRS.Core;

namespace AkkaCQRS.Web.Controllers
{
    public abstract class ApplicationController : ApiController
    {
        public static readonly ActorSystem System = Bootstrap.System;
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    }
}