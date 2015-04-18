using System;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using AkkaCQRS.Core.Accounting;
using DevOne.Security.Cryptography.BCrypt;

namespace AkkaCQRS.Core.Users
{
    public class UserEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public ISet<Guid> AccountsIds { get; set; }

        public UserEntity()
        {
            AccountsIds = new HashSet<Guid>();
        }

        public UserEntity(Guid id, string firstName, string lastName, string email, string passwordHash)
        {
            AccountsIds = new HashSet<Guid>();

            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PasswordHash = passwordHash;
        }
    }

    public class User : AggregateRoot<UserEntity>
    {
        private readonly Guid _id;

        public User(Guid id)
            : base("user-" + id.ToString("N"))
        {
            _id = id;
            
            // start in uninitialized state
            Context.Become(Uninitialized);
        }

        /// <summary>
        /// We immediately switch to <see cref="Uninitialized"/> at the beginning, so this method is empty.
        /// </summary>
        protected override bool OnCommand(object message) { return false; }

        protected override void UpdateState(IEvent domainEvent, IActorRef sender)
        {
            domainEvent.Match()
                .With<UserEvents.UserRegistered>(e =>
                {
                    State = new UserEntity(e.Id, e.FirstName, e.LastName, e.Email, e.PasswordHash);
                    Context.Become(Initialized);
                    if (sender != null) sender.Tell(State);

                    Log.Info("Registered user {0} {1} ({2}) with PersistenceId: {3}", e.FirstName, e.LastName, e.Email, e.Id);
                })
                .With<UserEvents.PasswordChanged>(e => State.PasswordHash = e.NewPasswordHash)
                .With<UserEvents.PasswordReset>(e => State.PasswordHash = e.NewPasswordHash)
                .With<UserEvents.UserSignedIn>(e =>
                {
                    Context.Become(SignedIn);
                    if (sender != null) sender.Tell(State);

                    Log.Info("User with PersistenceId {0} has signed in", e.Id);
                })
                .With<UserEvents.UserSignedOut>(e =>
                {
                    Context.Become(Initialized);
                })
                .With<UserEvents.AccountCreated>(e =>
                {
                    State.AccountsIds.Add(e.Id);

                    Log.Info("User with id {0} created an account with id {1} and initial balance {2}", e.OwnerId, e.Id, e.InitialBalance);
                });
        }

        /// <summary>
        /// Uninitialized actor represents the one which has no related entity instantiated yet.
        /// At this state the only allowed action is user registration.
        /// </summary>
        private bool Uninitialized(object message)
        {
            var cmd = message as UserCommands.RegisterUser;
            if (cmd != null)
            {
                var passwordHash = HashPassword(cmd.Password);
                Persist(new UserEvents.UserRegistered(_id, cmd.FirstName, cmd.LastName, cmd.Email, passwordHash), Sender);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Initialized state represents actor which has entity already initialized, 
        /// but following user has not logged in to his/her personal account.
        /// 
        /// At this state allowed actions are sign in, resetting user password. User sign out is ignored.
        /// </summary>
        private bool Initialized(object message)
        {
            return base.ReceiveCommand(message) || message.Match()
                .With<UserCommands.SignInUser>(signIn =>
                {
                    if (string.Equals(State.Email, signIn.Email, StringComparison.InvariantCultureIgnoreCase) && ValidatePassword(State, signIn.Password))
                        Persist(new UserEvents.UserSignedIn(_id), Sender);
                    else
                    {
                        Log.Error("Unauthorized user sign in. User id: {0}, email: {1}", _id, signIn.Email);
                        Sender.Tell(Unauthorized.Message(signIn));
                    }
                })
                .With<UserCommands.ResetPassword>(reset =>
                {
                    //TODO: user reset password and mail sent
                })
                .With<UserCommands.SignOutUser>(_ => { /* ignore */})
                .WasHandled;
        }

        /// <summary>
        /// Signed in user may perform all basic operations related with his/her accounts.
        /// </summary>
        private bool SignedIn(object message)
        {
            return base.ReceiveCommand(message) || message.Match()
                .With<UserCommands.ChangePassword>(change =>
                {
                    if (ValidatePassword(State, change.OldPassword))
                    {
                        var passwordHash = HashPassword(change.NewPassword);
                        Persist(new UserEvents.PasswordChanged(_id, passwordHash));
                    }
                    else
                    {
                        Log.Error("Unauthorized user sign in. User id: {0}", _id);
                        Sender.Tell(Unauthorized.Message(change));
                    }
                })
                .WasHandled;
        }

        private static bool ValidatePassword(UserEntity user, string password)
        {
            return BCryptHelper.CheckPassword(password, user.PasswordHash);
        }

        private static string HashPassword(string password)
        {
            return BCryptHelper.HashPassword(password, BCryptHelper.GenerateSalt());
        }
    }
}