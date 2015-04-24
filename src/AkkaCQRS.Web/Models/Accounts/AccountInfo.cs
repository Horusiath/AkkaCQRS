using System;

namespace AkkaCQRS.Web.Models.Accounts
{
    public class AccountInfo
    {
        public readonly Guid AccountId;
        public readonly Guid OwnerId;
        public readonly decimal Balance;
        public readonly bool IsActive;

        public AccountInfo(Guid accountId, Guid ownerId, decimal balance, bool isActive)
        {
            AccountId = accountId;
            OwnerId = ownerId;
            Balance = balance;
            IsActive = isActive;
        }
    }
}