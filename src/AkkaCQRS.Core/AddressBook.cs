using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace AkkaCQRS.Core
{
    /// <summary>
    /// Address book serves as a central register of other actors (mostly coordinators and routers).
    /// </summary>
    public class AddressBook : ReceiveActor
    {
        #region messages

        public class Register
        {
            public readonly Type ActorType;
            public readonly ActorPath Path;

            public Register(Type actorType, ActorPath path)
            {
                ActorType = actorType;
                Path = path;
            }
        }

        public sealed class Register<TActor> : Register where TActor : ActorBase
        {
            public Register(ActorPath path)
                : base(typeof(TActor), path)
            {
            }
        }

        public class Unregister
        {
            public readonly Type ActorType;

            public Unregister(Type actorType)
            {
                ActorType = actorType;
            }
        }

        public sealed class Unregister<TActor> : Unregister where TActor : ActorBase
        {
            public Unregister() : base(typeof(TActor)) { }
        }

        public class Get
        {
            public readonly Type ActorType;

            public Get(Type actorType)
            {
                ActorType = actorType;
            }
        }

        public sealed class Get<TActor> : Get where TActor : ActorBase
        {
            public Get() : base(typeof(TActor)) { }
        }

        public class Found
        {
            public readonly Type ActorType;
            public readonly ActorPath Path;

            public Found(Type actorType, ActorPath path)
            {
                ActorType = actorType;
                Path = path;
            }
        }

        public class NotFound
        {
            public readonly Type ActorType;

            public NotFound(Type actorType)
            {
                ActorType = actorType;
            }
        }

        #endregion

        public const string Name = "address-book";
        private readonly IDictionary<Type, ActorPath> _addresses;

        public AddressBook(IEnumerable<KeyValuePair<Type, ActorPath>> entries = null)
        {
            _addresses = new ConcurrentDictionary<Type, ActorPath>(entries ?? Enumerable.Empty<KeyValuePair<Type, ActorPath>>());

            Receive<Register>(register => _addresses.Add(register.ActorType, register.Path));
            Receive<Unregister>(unregister => _addresses.Remove(unregister.ActorType));
            Receive<Get>(get =>
            {
                ActorPath path;
                var reply = _addresses.TryGetValue(get.ActorType, out path)
                    ? (object)new Found(get.ActorType, path)
                    : new NotFound(get.ActorType);

                Sender.Tell(reply);
            });
        }
    }

    public static class AddressBookExtensions
    {
        /// <summary>
        /// Returns a reference to address book.
        /// </summary>
        public static ICanTell GetAddressBook(this ActorSystem system)
        {
            return system.ActorSelection("/" + AddressBook.Name);
        }
    }
}