using Akka.Event;
using AkkaCQRS.Web.Actors;
using Microsoft.AspNet.SignalR;

namespace AkkaCQRS.Web.Hubs
{
    public class LoggingHub : ApplicationHub
    {
        public LoggingHub()
        {
            Register();
        }

        public void SendLog(LogLevel level, string message)
        {
            Clients.All.log(level, message);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Unregister();
        }

        private void Register()
        {
            var loggers = System.ActorSelection("/system/log*");
            loggers.Tell(new BrowserLogger.Register(this));
        }

        private void Unregister()
        {
            var loggers = System.ActorSelection("/system/log*");
            loggers.Tell(new BrowserLogger.Unregister(this));
        }
    }
}