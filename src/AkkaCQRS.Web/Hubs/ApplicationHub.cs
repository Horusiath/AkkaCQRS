using Akka.Actor;
using AkkaCQRS.Core;
using Microsoft.AspNet.SignalR;

namespace AkkaCQRS.Web.Hubs
{
    public class ApplicationHub : Hub
    {
        public static readonly ActorSystem System = Bootstrap.System;

    }
}