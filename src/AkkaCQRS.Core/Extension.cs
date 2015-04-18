using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Routing;
using AkkaCQRS.Core.Users;

namespace AkkaCQRS.Core
{
    public class CqrsExtension : IExtension
    {
        public CqrsExtension(ExtendedActorSystem system)
        {
            var usersCoordinator = system.ActorOf(Props.Create(() => new UserCoordinator()), "users");
            var usersIndex = system.ActorOf(Props.Create(() => new UserIndex()).WithRouter(new RoundRobinPool(4)), "users-index");

            system.ActorOf(Props.Create(() => new AddressBook(new Dictionary<Type, ICanTell>
            {
                {typeof(UserCoordinator), usersCoordinator},
                {typeof(UserIndex), usersIndex},
            })));
        }
    }

    public class CqrsExtensionProvider : ExtensionIdProvider<CqrsExtension>
    {
        public static readonly CqrsExtensionProvider Instance = new CqrsExtensionProvider();

        private CqrsExtensionProvider() { }

        public override CqrsExtension CreateExtension(ExtendedActorSystem system)
        {
            return new CqrsExtension(system);
        }
    }
}