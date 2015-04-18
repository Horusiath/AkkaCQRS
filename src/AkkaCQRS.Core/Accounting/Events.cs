using System;

namespace AkkaCQRS.Core.Accounting
{
    public interface IAccountEvent : IEvent { }


    public sealed class NotEnoughtFunds : IMessage
    {
        public readonly Guid AccountId;
        public NotEnoughtFunds(Guid accountId)
        {
            AccountId = accountId;
        }
    }

    public static class AccountEvents
    {
        public sealed class AccountCreated : IAccountEvent
        {
            public readonly Guid Id;
            public readonly Guid OwnerId;
            public readonly decimal Balance;

            public AccountCreated(Guid id, Guid ownerId, decimal balance)
            {
                Id = id;
                OwnerId = ownerId;
                Balance = balance;
            }
        }

        public sealed class AccountDeactivated : IAccountEvent
        {
            public readonly Guid Id;

            public AccountDeactivated(Guid id)
            {
                Id = id;
            }
        }

        public sealed class Deposited : IAccountEvent
        {
            public readonly Guid Id;
            public readonly decimal Amount;

            public Deposited(Guid id, decimal amount)
            {
                Id = id;
                Amount = amount;
            }
        }

        public sealed class Transfered : IAccountEvent
        {
            public readonly Guid FromId;
            public readonly Guid ToId;
            public readonly decimal Amount;

            public Transfered(Guid fromId, Guid toId, decimal amount)
            {
                FromId = fromId;
                ToId = toId;
                Amount = amount;
            }
        }

        public sealed class Withdrawal : IAccountEvent
        {
            public readonly Guid Id;
            public readonly decimal Amount;

            public Withdrawal(Guid id, decimal amount)
            {
                Id = id;
                Amount = amount;
            }
        }
    }
}