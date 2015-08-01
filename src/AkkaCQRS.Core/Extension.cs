using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Monitoring;
using Akka.Monitoring.StatsD;
using Akka.Routing;
using AkkaCQRS.Core.Accounting;
using AkkaCQRS.Core.Users;

namespace AkkaCQRS.Core
{
    public class CqrsExtension : IExtension
    {
        public CqrsExtension(ExtendedActorSystem system)
        {
            var config = ConfigurationFactory.FromResource<CqrsExtension>("AkkaCQRS.Core.akka-cqrs.conf");
            system.Settings.InjectTopLevelFallback(config);

            ActorMonitoringExtension.RegisterMonitor(system, new ActorStatsDMonitor());

            var usersCoordinator = system.ActorOf(Props.Create(() => new UserCoordinator()), "users");
            var accountCoordinator = system.ActorOf(Props.Create(() => new AccountCoordinator()), "accounts");
            var usersIndex = system.ActorOf(Props.Create(() => new UserIndex()), "users-index");

            var addressBook = system.ActorOf(Props.Create(() => new AddressBook(new Dictionary<Type, ICanTell>
            {
                {typeof(UserCoordinator), usersCoordinator},
                {typeof(AccountCoordinator), accountCoordinator},
                {typeof(UserIndex), usersIndex},
            })), AddressBook.Name);
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