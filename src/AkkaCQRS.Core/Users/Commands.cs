using System;

namespace AkkaCQRS.Core.Users
{
    public interface IUserCommand : ICommand { }

    public static class UserCommands
    {
        public sealed class RegisterUser : IUserCommand
        {
            public readonly string FirstName;
            public readonly string LastName;
            public readonly string Email;
            public readonly string Password;

            public RegisterUser(string firstName, string lastName, string email, string password)
            {
                FirstName = firstName;
                LastName = lastName;
                Email = email;
                Password = password;
            }
        }

        public sealed class ChangePassword : IUserCommand, IAddressed
        {
            public readonly Guid UserId;
            public readonly string OldPassword;
            public readonly string NewPassword;

            public ChangePassword(Guid userId, string oldPassword, string newPassword)
            {
                UserId = userId;
                NewPassword = newPassword;
                OldPassword = oldPassword;
            }

            public Guid RecipientId
            {
                get { return UserId; }
            }
        }

        public sealed class ResetPassword : IUserCommand
        {
            public readonly string Email;

            public ResetPassword(string email)
            {
                Email = email;
            }
        }

        public sealed class SignInUser : IUserCommand
        {
            public readonly string Email;
            public readonly string Password;

            public SignInUser(string email, string password)
            {
                Email = email;
                Password = password;
            }
        }

        public sealed class SignOutUser : IUserCommand, IAddressed
        {
            public readonly Guid UserId;

            public SignOutUser(Guid userId)
            {
                UserId = userId;
            }

            public Guid RecipientId
            {
                get { return UserId; }
            }
        }
    }
}