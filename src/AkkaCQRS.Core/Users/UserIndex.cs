using System;
using System.Collections.Concurrent;
using Akka.Actor;

namespace AkkaCQRS.Core.Users
{
    /// <summary>
    /// User index reacts to the incoming user registration events and stores them on the separate 
    /// key-value store map (or in SQL database) for read model. It's also able to return an user 
    /// data given the user email. This is usefull in scenarios of eg. SQL reports about users has to be made.
    /// </summary>
    public class UserIndex : ReceiveActor
    {
        #region messages

        public sealed class GetUserByEmail
        {
            public readonly string Email;

            public GetUserByEmail(string email)
            {
                Email = email;
            }
        }

        public interface IReply { }

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

        public sealed class UserNotFound : IReply
        {
            public readonly string Email;

            public UserNotFound(string email)
            {
                Email = email;
            }
        }

        #endregion

        // lets start with in memory K-V map
        private static readonly ConcurrentDictionary<string, Guid> UsersIdsByEmail = new ConcurrentDictionary<string, Guid>();

        public UserIndex()
        {
            Receive<UserEvents.UserRegistered>(registered => UsersIdsByEmail.TryAdd(registered.Email, registered.Id));

            Receive<GetUserByEmail>(request =>
            {
                Guid userId;
                var reply = UsersIdsByEmail.TryGetValue(request.Email, out userId)
                    ? (IReply)new UserFound(request.Email, userId)
                    : new UserNotFound(request.Email);

                Sender.Tell(reply);
            });
        }
    }
}