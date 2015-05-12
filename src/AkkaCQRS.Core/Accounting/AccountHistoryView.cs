using System;
using System.Collections.Generic;
using System.Linq;
using Akka;
using Akka.Persistence;

namespace AkkaCQRS.Core.Accounting
{
    /// <summary>
    /// Account log represents single transaction made from particular account.
    /// </summary>
    [Serializable]
    public sealed class AccountLog
    {
        internal class AccountLogTimestampComparer : IComparer<AccountLog>
        {
            /// <summary>
            /// Sort account logs in by timestamp from latests to oldest.
            /// </summary>
            public int Compare(AccountLog x, AccountLog y)
            {
                return y.Timestamp.CompareTo(x);
            }
        }

        /// <summary>
        /// Default comparer sorting account logs by their timestamp in descending order.
        /// </summary>
        public static readonly IComparer<AccountLog> TimestampComparer = new AccountLogTimestampComparer();

        /// <summary>
        /// Transaction date.
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Transaction ammount. Positive value determines deposit, otherwise withdrawal.
        /// </summary>
        public readonly decimal Amount;

        /// <summary>
        /// Total account balance after transaction has been made.
        /// </summary>
        public readonly decimal Balance;

        /// <summary>
        /// Account, which current log is referring to.
        /// </summary>
        public readonly Guid AccountId;

        public AccountLog(DateTime timestamp, decimal amount, decimal balance, Guid accountId)
        {
            Timestamp = timestamp;
            Amount = amount;
            Balance = balance;
            AccountId = accountId;
        }
    }

    /// <summary>
    /// Account history view represents self-updating read model for account history log page.
    /// It's related with provided id parameter determining, which account aggregate root should it relate to.
    /// </summary>
    public class AccountHistoryView : PersistentView
    {
        #region messages

        /// <summary>
        /// Request to receive a single page of account logs.
        /// </summary>
        public sealed class GetHistoryPage
        {
            public readonly int Skip;
            public readonly int Take;

            public GetHistoryPage(int skip, int take)
            {
                Skip = skip;
                Take = take;
            }
        }

        #endregion

        private readonly Guid _id;

        // you would probably like to record this directly in database
        protected readonly ISet<AccountLog> History;
        protected decimal CurrentBalance;

        public AccountHistoryView(Guid id)
        {
            _id = id;
            History = new SortedSet<AccountLog>(AccountLog.TimestampComparer);
        }

        /// <summary>
        /// View id is unique identifier of current view. It's used i.e. for persistent view state snapshotting.
        /// </summary>
        public override string ViewId { get { return "account-history-" + _id.ToString("N"); } }

        /// <summary>
        /// Persistence id is used to identify aggregate root (<see cref="Account"/> instance in 
        /// this case) which will be used as an event source for current persistent view.
        /// </summary>
        public override string PersistenceId { get { return "account-" + _id.ToString("N"); } }

        protected override bool Receive(object message)
        {
            return message.Match()
                .With<GetHistoryPage>(page =>
                {
                    Sender.Tell(History.Skip(page.Skip).Take(page.Take).ToArray(), Self);
                })
                .With<AccountEvents.TransferedWithdrawal>(e =>
                {
                    RecordWithdrawal(e.FromId, e.Amount, e.Timestamp);
                })
                .With<AccountEvents.TransferedDeposit>(e =>
                {
                    RecordDeposit(e.ToId, e.Amount, e.Timestamp);
                })
                .With<AccountEvents.Withdrawal>(e =>
                {
                    if (e.Id == _id)
                    {
                        RecordWithdrawal(e.Id, e.Amount, e.Timestamp);
                    }
                })
                .With<AccountEvents.Deposited>(e =>
                {
                    if (e.Id == _id)
                    {
                        RecordDeposit(e.Id, e.Amount, e.Timestamp);
                    }
                })
                .With<AccountEvents.AccountCreated>(e =>
                {
                    CurrentBalance = 0.0M;
                })
                .With<AccountEvents.AccountDeactivated>(e => { /* ignore */ })
                .WasHandled;
        }

        private void RecordWithdrawal(Guid accountId, decimal amount, DateTime timestamp)
        {
            CurrentBalance -= amount;
            var record = new AccountLog(timestamp, -amount, CurrentBalance, accountId);
            History.Add(record);
        }

        private void RecordDeposit(Guid recipientId, decimal amount, DateTime timestamp)
        {
            CurrentBalance += amount;
            var record = new AccountLog(timestamp, amount, CurrentBalance, recipientId);
            History.Add(record);
        }
    }
}