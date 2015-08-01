using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Event;
using AkkaCQRS.Web.Hubs;

namespace AkkaCQRS.Web.Actors
{
    public class BrowserLoggerException : Exception
    {
        public BrowserLoggerException(Exception innerException) : base(string.Empty, innerException)
        {
        }

        protected BrowserLoggerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class BrowserLogger : ReceiveActor
    {
        #region Messages

        [Serializable]
        public sealed class Register
        {
            public readonly LoggingHub Hub;

            public Register(LoggingHub hub)
            {
                Hub = hub;
            }
        }

        [Serializable]
        public sealed class Unregister
        {
            public readonly LoggingHub Hub;

            public Unregister(LoggingHub hub)
            {
                Hub = hub;
            }
        }

        #endregion

        private readonly ISet<LoggingHub> _hubs = new HashSet<LoggingHub>();
        private ILoggingAdapter _log;

        protected ILoggingAdapter Logger
        {
            get { return _log ?? (_log = Context.GetLogger()); }
        }

        public BrowserLogger()
        {
            Receive<Debug>(e => Log(LogLevel.DebugLevel, e.ToString()));
            Receive<Info>(e => Log(LogLevel.InfoLevel, e.ToString()));
            Receive<Warning>(e => Log(LogLevel.WarningLevel, e.ToString()));
            Receive<Error>(e =>
            {
                // we omit handling BrowserLoggerException to avoid cascading failures
                if (!(e.Cause is BrowserLoggerException))
                {
                    Log(LogLevel.ErrorLevel, e.ToString());
                }
            });

            Receive<Register>(register => _hubs.Add(register.Hub));
            Receive<Unregister>(register => _hubs.Remove(register.Hub));
        }

        private void Log(LogLevel level, string message)
        {
            foreach (var hub in _hubs)
            {
                try
                {
                    hub.SendLog(level, message);
                }
                catch (Exception exception)
                {
                    Logger.Error(new BrowserLoggerException(exception), "Internal browser logger exception");
                }
            }
        }
    }
}