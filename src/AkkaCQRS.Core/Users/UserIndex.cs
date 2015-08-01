using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using Akka.Persistence;
using Akka.Util.Internal;

namespace AkkaCQRS.Core.Users
{
    /// <summary>
    /// User index reacts to the incoming user registration events and stores them on the separate 
    /// key-value store map (or in SQL database) for read model. It's also able to return an user 
    /// data given the user email. This is usefull in scenarios of eg. SQL reports about users has to be made.
    /// </summary>
    public class UserIndex : PersistentActor
    {
        #region messages

        [Serializable]
        public sealed class GetUserByEmail
        {
            public readonly string Email;

            public GetUserByEmail(string email)
            {
                Email = email;
            }
        }

        public interface IReply { }

        [Serializable]
        public sealed class UserFound : IReply
        {
            public readonly string Email;
            public readonly Guid UserId;

            public UserFound(string email, Guid userId)
            {
                Email = email;
                UserId = userId;
            }
        }

        [Serializable]
        public sealed class UserNotFound : IReply
        {
            public readonly string Email;

            public UserNotFound(string email)
            {
                Email = email;
            }
        }

        [Serializable]
        public sealed class UserIndexed
        {
            public readonly string Email;
            public readonly Guid UserId;

            public UserIndexed(string email, Guid userId)
            {
                Email = email;
                UserId = userId;
            }
        }

        #endregion

        // lets start with in memory K-V map
        private IDictionary<string, Guid> _usersIdsByEmail = new Dictionary<string, Guid>();
        private int _counter = 0;

        public UserIndex()
        {
            Context.System.EventStream.Subscribe(Self, typeof (UserEvents.UserRegistered));
        }

        protected override bool ReceiveRecover(object message)
        {
            return message.Match()
                .With<UserIndexed>(e =>
                {
                    _usersIdsByEmail.AddOrSet(e.Email.ToLowerInvariant(), e.UserId);
                })
                .With<SnapshotOffer>(offer =>
                {
                    _usersIdsByEmail = (Dictionary<string, Guid>)offer.Snapshot;
                })
                .WasHandled;
        }

        protected override bool ReceiveCommand(object message)
        {
            return message.Match()
                .With<UserEvents.UserRegistered>(registered =>
                {
                    Persist(new UserIndexed(registered.Email, registered.Id), e =>
                    {
                        _usersIdsByEmail.AddOrSet(e.Email.ToLowerInvariant(), e.UserId);

                        _counter++;
                        if (_counter%10 == 0)
                        {
                            SaveSnapshot(_usersIdsByEmail);
                            _counter = 0;
                        }
                    });
                })
                .With<GetUserByEmail>(request =>
                {
                    Guid userId;
                    var reply = _usersIdsByEmail.TryGetValue(request.Email.ToLowerInvariant(), out userId)
                        ? (IReply)new UserFound(request.Email, userId)
                        : new UserNotFound(request.Email);

                    Sender.Tell(reply);
                })
                .WasHandled;
        }

        public override string PersistenceId { get { return "users-index"; } }
    }
}