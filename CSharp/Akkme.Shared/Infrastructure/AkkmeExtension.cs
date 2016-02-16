using System;
using Akka.Actor;

namespace Akkme.Shared.Infrastructure
{
    /// <summary>
    /// We use this object as plugin to Akka.NET. This way we can easily integrate all of our domain logic with Akka.NET itself.
    /// </summary>
    public class Akkme : IExtension
    {
        public static Akkme Get(ActorSystem system)
        {
            return system.WithExtension<Akkme, AkkmeExtensionProvider>();
        }

        public IActorRef Accounts { get; }

        public Akkme(ExtendedActorSystem system)
        {
            throw new NotImplementedException();
        }
    }

    public class AkkmeExtensionProvider : ExtensionIdProvider<Akkme>
    {
        public override Akkme CreateExtension(ExtendedActorSystem system)
        {
            return new Akkme(system);
        }
    }
}