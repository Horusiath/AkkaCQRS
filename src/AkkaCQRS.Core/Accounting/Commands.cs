using System;

namespace AkkaCQRS.Core.Accounting
{
    public interface IAccountCommand : ICommand { }

    public static class AccountCommands
    {
        public sealed class CreateAccount : IAccountCommand, IAddressed
        {
            public readonly Guid AccountId;
            public readonly Guid OwnerId;
            public readonly decimal Balance;

            public CreateAccount(Guid accountId, Guid ownerId, decimal balance)
            {
                AccountId = accountId;
                OwnerId = ownerId;
                Balance = balance;
            }

            public Guid RecipientId
            {
                get { return AccountId; }
            }
        }
        public sealed class DeactivateAccount : IAccountCommand, IAddressed
        {
            public readonly Guid AccountId;

            public DeactivateAccount(Guid accountId)
            {
                AccountId = accountId;
            }

            public Guid RecipientId
            {
                get { return AccountId; }
            }
        }

        public sealed class Deposit : IAccountCommand, IAddressed
        {
            public readonly Guid AccountId;
            public readonly decimal Amount;

            public Deposit(Guid accountId, decimal amount)
            {
                AccountId = accountId;
                Amount = amount;
            }

            public Guid RecipientId
            {
                get { return AccountId; }
            }
        }

        public sealed class Transfer : IAccountCommand
        {
            public readonly Guid FromAccountId;
            public readonly Guid ToAccountId;
            public readonly decimal Amount;

            public Transfer(Guid fromAccountId, Guid toAccountId, decimal amount)
            {
                FromAccountId = fromAccountId;
                ToAccountId = toAccountId;
                Amount = amount;
            }
        }

        public sealed class Withdraw : IAccountCommand, IAddressed
        {
            public readonly Guid AccountId;
            public readonly decimal Amount;

            public Withdraw(Guid accountId, decimal amount)
            {
                AccountId = accountId;
                Amount = amount;
            }

            public Guid RecipientId
            {
                get { return AccountId; }
            }
        }
    }
}