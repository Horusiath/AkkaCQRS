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
            public readonly ICanTell Ref;

            public Register(Type actorType, ICanTell @ref)
            {
                ActorType = actorType;
                Ref = @ref;
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

        public class Get
        {
            public readonly Type ActorType;

            public Get(Type actorType)
            {
                ActorType = actorType;
            }
        }

        public class Found
        {
            public readonly Type ActorType;
            public readonly ICanTell Ref;

            public Found(Type actorType, ICanTell @ref)
            {
                ActorType = actorType;
                Ref = @ref;
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
        private readonly IDictionary<Type, ICanTell> _addresses;

        public AddressBook(IEnumerable<KeyValuePair<Type, ICanTell>> entries = null)
        {
            _addresses = new ConcurrentDictionary<Type, ICanTell>(entries ?? Enumerable.Empty<KeyValuePair<Type, ICanTell>>());

            Receive<Register>(register => _addresses.Add(register.ActorType, register.Ref));
            Receive<Unregister>(unregister => _addresses.Remove(unregister.ActorType));
            Receive<Get>(get =>
            {
                ICanTell path;
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