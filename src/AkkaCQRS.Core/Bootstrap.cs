using System;
using Akka.Actor;

namespace AkkaCQRS.Core
{
    public static class Bootstrap
    {
        private static ActorSystem _system;

        public static ActorSystem System
        {
            get
            {
                if(_system == null) throw new InvalidOperationException("Actor system is not initialized");
                return _system;
            }
        }

        public static void Initialize()
        {
            _system = ActorSystem.Create("akka-cqrs");

            // we hide all application specific actor system 
            // initializers under the cover of actor system extensions
            CqrsExtensionProvider.Instance.Apply(_system);
        }

        public static void Shutdown()
        {
            if (_system != null)
            {
                _system.Shutdown();
                _system.AwaitTermination(TimeSpan.FromMinutes(1));

                _system = null;
            }
        }
    }
}