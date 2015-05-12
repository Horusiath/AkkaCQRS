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
            public readonly DateTime Timestamp;

            public AccountCreated(Guid id, Guid ownerId, decimal balance, DateTime timestamp)
            {
                Id = id;
                OwnerId = ownerId;
                Balance = balance;
                Timestamp = timestamp;
            }
        }

        public sealed class AccountDeactivated : IAccountEvent
        {
            public readonly Guid Id;
            public readonly DateTime Timestamp;

            public AccountDeactivated(Guid id, DateTime timestamp)
            {
                Id = id;
                Timestamp = timestamp;
            }
        }

        public sealed class Deposited : IAccountEvent
        {
            public readonly Guid Id;
            public readonly decimal Amount;
            public readonly DateTime Timestamp;

            public Deposited(Guid id, decimal amount, DateTime timestamp)
            {
                Id = id;
                Amount = amount;
                Timestamp = timestamp;
            }
        }

        public sealed class TransferedWithdrawal : IAccountEvent
        {
            public readonly Guid FromId;
            public readonly Guid TransactionId;
            public readonly decimal Amount;
            public readonly DateTime Timestamp;

            public TransferedWithdrawal(Guid fromId, Guid transactionId, decimal amount, DateTime timestamp)
            {
                FromId = fromId;
                TransactionId = transactionId;
                Amount = amount;
                Timestamp = timestamp;
            }
        }
        public sealed class TransferedDeposit : IAccountEvent
        {
            public readonly Guid ToId;
            public readonly Guid TransactionId;
            public readonly decimal Amount;
            public readonly DateTime Timestamp;

            public TransferedDeposit(Guid toId, Guid transactionId, decimal amount, DateTime timestamp)
            {
                ToId = toId;
                TransactionId = transactionId;
                Amount = amount;
                Timestamp = timestamp;
            }
        }

        public sealed class Withdrawal : IAccountEvent
        {
            public readonly Guid Id;
            public readonly decimal Amount;
            public readonly DateTime Timestamp;

            public Withdrawal(Guid id, decimal amount, DateTime timestamp)
            {
                Id = id;
                Amount = amount;
                Timestamp = timestamp;
            }
        }
    }
}