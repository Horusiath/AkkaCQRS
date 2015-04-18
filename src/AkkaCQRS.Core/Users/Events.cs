using System;

namespace AkkaCQRS.Core.Users
{
    public interface IUserEvent : IEvent
    {
    }

    public static class UserEvents
    {
        public sealed class AccountCreated : IUserEvent
        {
            public readonly Guid Id;
            public readonly Guid OwnerId;
            public decimal InitialBalance;

            public AccountCreated(Guid id, Guid ownerId, decimal initialBalance)
            {
                Id = id;
                OwnerId = ownerId;
                InitialBalance = initialBalance;
            }
        }

        public sealed class UserRegistered : IUserEvent
        {
            public readonly Guid Id;
            public readonly string FirstName;
            public readonly string LastName;
            public readonly string Email;
            public readonly string PasswordHash;

            public UserRegistered(Guid id, string firstName, string lastName, string email, string passwordHash)
            {
                Id = id;
                FirstName = firstName;
                LastName = lastName;
                Email = email;
                PasswordHash = passwordHash;
            }
        }

        public sealed class UserSignedIn : IUserEvent
        {
            public readonly Guid Id;

            public UserSignedIn(Guid id)
            {
                Id = id;
            }
        }

        public sealed class UserSignedOut : IUserEvent
        {
            public readonly Guid Id;

            public UserSignedOut(Guid id)
            {
                Id = id;
            }
        }

        public sealed class PasswordChanged : IUserEvent
        {
            public readonly Guid Id;
            public readonly string NewPasswordHash;

            public PasswordChanged(Guid id, string newPasswordHash)
            {
                Id = id;
                NewPasswordHash = newPasswordHash;
            }
        }

        public sealed class PasswordReset : IUserEvent
        {
            public readonly Guid Id;
            public readonly string NewPasswordHash;

            public PasswordReset(Guid id, string newPasswordHash)
            {
                Id = id;
                NewPasswordHash = newPasswordHash;
            }
        }
    }
}