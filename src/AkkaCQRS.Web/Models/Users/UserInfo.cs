using System;

namespace AkkaCQRS.Web.Models.Users
{
    public class UserInfo
    {
        public readonly Guid UserId;
        public readonly string FirstName;
        public readonly string LastName;
        public readonly string Email;
        public readonly Guid[] AccountsIds;

        public UserInfo(Guid userId, string firstName, string lastName, string email, Guid[] accountsIds)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            AccountsIds = accountsIds;
        }
    }
}