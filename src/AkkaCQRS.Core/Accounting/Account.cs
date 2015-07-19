using System;
using System.Collections.Generic;
using System.Linq;
using Akka;
using Akka.Actor;

namespace AkkaCQRS.Core.Accounting
{
    public delegate DateTime Clock();

    public class AccountEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }

        public AccountEntity(Guid id, Guid ownerId, bool isActive, decimal balance)
        {
            Id = id;
            OwnerId = ownerId;
            IsActive = isActive;
            Balance = balance;
        }
    }

    public class PendingTransfer
    {
        public readonly Guid TransactionId;
        public readonly IActorRef Sender;
        public readonly IActorRef Recipient;
        public readonly decimal Amount;

        public PendingTransfer(Guid transactionId, IActorRef sender, IActorRef recipient, decimal amount)
        {
            TransactionId = transactionId;
            Sender = sender;
            Recipient = recipient;
            Amount = amount;
        }
    }

    public class Account : AggregateRoot<AccountEntity>
    {
        /// <summary>
        /// Custom notion of date time provider, easy to replace.
        /// </summary>
        public static readonly Clock Clock = () => DateTime.UtcNow;

        private readonly Guid _id;

        private readonly ICollection<PendingTransfer> _pendingTransactions;

        public Account(Guid id)
            : base("account-" + id.ToString("N"))
        {
            _id = id;
            _pendingTransactions = new List<PendingTransfer>();
            Context.Become(Uninitialized);
        }

        /// <summary>
        /// Gets total amount of funds reserved on pending transactions authentication.
        /// </summary>
        public decimal ReservedFunds
        {
            get { return _pendingTransactions.Aggregate(0M, (acc, transaction) => acc + transaction.Amount); }
        }


        protected override bool OnCommand(object message)
        {
            return false;
        }

        protected override void OnReplaySuccess()
        {
            if (State == null) Become(Uninitialized);
            else if (State.IsActive) Become(Active);
            else Become(Deactivated);
        }

        protected override void UpdateState(IEvent domainEvent, IActorRef sender)
        {
            domainEvent.Match()
                .With<AccountEvents.Withdrawal>(e => State.Balance -= e.Amount)
                .With<AccountEvents.Deposited>(e => State.Balance += e.Amount)
                .With<AccountEvents.TransferedWithdrawal>(e => State.Balance -= e.Amount)
                .With<AccountEvents.TransferedDeposit>(e => State.Balance += e.Amount)
                .With<AccountEvents.AccountCreated>(e =>
                {
                    State = new AccountEntity(e.Id, e.OwnerId, true, e.Balance);
                    Context.Become(Active);
                    if (sender != null) sender.Tell(State);

                    Log.Info("Account with id {0} and balance {1} has been created", e.Id, e.Balance);
                })
                .With<AccountEvents.AccountDeactivated>(e =>
                {
                    State.IsActive = false;
                    Context.Become(Deactivated);

                    Log.Info("Account with id {0} has been deactivated", e.Id);
                });
        }

        private bool Uninitialized(object message)
        {
            return message.Match()
                .With<AccountCommands.CreateAccount>(create =>
                {
                    Persist(new AccountEvents.AccountCreated(_id, create.OwnerId, create.Balance, Clock()), Sender);
                })
                .WasHandled;
        }

        private bool Active(object message)
        {
            return base.ReceiveCommand(message) || message.Match()
                .With<TransactionCoordinator.BeginTransaction>(EstablishTransferTransaction)
                .With<TransactionCoordinator.Commit>(CommitTransfer)
                .With<TransactionCoordinator.Rollback>(AbortTransfer)
                .With<AccountCommands.DeactivateAccount>(Deactivate)
                .With<AccountCommands.Deposit>(Deposit)
                .With<AccountCommands.Withdraw>(Withdraw)
                .WasHandled;
        }

        private void Deactivate(AccountCommands.DeactivateAccount deactivate)
        {
            Persist(new AccountEvents.AccountDeactivated(deactivate.AccountId, Clock()));
        }

        private void Deposit(AccountCommands.Deposit deposit)
        {
            if (deposit.Amount > 0)
            {
                Persist(new AccountEvents.Deposited(_id, deposit.Amount, Clock()));
            }
            else
            {
                Log.Error("Cannot perform deposit on account {0}: money amount is not positive value", _id);
            }
        }

        private void Withdraw(AccountCommands.Withdraw withdraw)
        {
            var sender = Sender;
            var withdrawal = new AccountEvents.Withdrawal(_id, withdraw.Amount, Clock());

            // Use defer to await to proceed command until all account events have been
            // persisted and handled. This is done mostly, because we don't want to perform
            // negative account check while there may be still account balance modifying events
            // waiting in mailbox.
            Defer(withdrawal, e =>
            {
                if (withdraw.Amount > 0 && withdraw.Amount <= (State.Balance - ReservedFunds))
                {
                    Persist(e, sender);
                }
                else
                {
                    Log.Error("Cannot perform withdrawal from account {0}, because it has a negative balance", _id);
                    sender.Tell(new NotEnoughtFunds(_id));
                }
            });
        }

        /// <summary>
        /// Aborts related transaction and sends <see cref="TransactionCoordinator.Ack"/> back to transaction coordinator.
        /// </summary>
        private void AbortTransfer(TransactionCoordinator.Rollback e)
        {
            var abortedTransaction = _pendingTransactions.FirstOrDefault(tx => tx.TransactionId == e.TransactionId);
            if (abortedTransaction != null)
            {
                _pendingTransactions.Remove(abortedTransaction);
                Sender.Tell(new TransactionCoordinator.Ack(e.TransactionId));
            }
            else Unhandled(e);
        }

        /// <summary>
        /// Commits target transaction, persisting a transaction event and sending <see cref="TransactionCoordinator.Ack"/> to transaction coordinator.
        /// </summary>
        private void CommitTransfer(TransactionCoordinator.Commit e)
        {
            var transaction = _pendingTransactions.SingleOrDefault(tx => tx.TransactionId == e.TransactionId);
            if (transaction != null)
            {
                // apply pending transaction and confirm operation
                var transfered = Self.Equals(transaction.Sender)
                    ? (IAccountEvent)
                        new AccountEvents.TransferedWithdrawal(State.Id, transaction.TransactionId, transaction.Amount, Clock())
                    : new AccountEvents.TransferedDeposit(State.Id, transaction.TransactionId, transaction.Amount, Clock());

                Persist(transfered);

                // don't send ACK until you're sure, that event has been stored
                Defer(transfered, _ =>
                {
                    _pendingTransactions.Remove(transaction);
                    Sender.Tell(new TransactionCoordinator.Ack(transaction.TransactionId));
                });
            }
            else Unhandled(e);
        }

        /// <summary>
        /// Establishes first phase of the two-phase commit transaction. Current account funds are being verified. If transfer can be proceed,
        /// transaction goes onto pending transactions list nad <see cref="TransactionCoordinator.Commit"/> message is sent to transaction coordinator.
        /// Otherwise transaction is aborted.
        /// </summary>
        private void EstablishTransferTransaction(TransactionCoordinator.BeginTransaction e)
        {
            var pendingTransaction = e.Payload as PendingTransfer;
            if (pendingTransaction != null)
            {
                if (Self.Equals(pendingTransaction.Sender))
                {
                    // if current actor is account sender, 
                    var unreserved = State.Balance - ReservedFunds;
                    if (pendingTransaction.Amount > 0 && pendingTransaction.Amount <= unreserved)
                    {
                        _pendingTransactions.Add(pendingTransaction);
                        Sender.Tell(new TransactionCoordinator.Continue(pendingTransaction.TransactionId));
                    }
                    else
                    {
                        Sender.Tell(new TransactionCoordinator.Abort(pendingTransaction.TransactionId,
                            new Exception(string.Format("Account {0} has insufficient funds. Unreserved balance {1}, requested {2}", State.Id, unreserved, pendingTransaction.Amount))));
                    }
                }
                else if (Self.Equals(pendingTransaction.Recipient))
                {
                    // recipient's account doesn't need to check if it has enough funds
                    _pendingTransactions.Add(pendingTransaction);
                    Sender.Tell(new TransactionCoordinator.Commit(pendingTransaction.TransactionId));
                }
                else
                {
                    Sender.Tell(new TransactionCoordinator.Abort(e.TransactionId,
                        new Exception(string.Format(
                            "Transaction {0} was addressed to {1}, who is neither sender nor recipient", e.TransactionId, Self))));
                    Unhandled(e);
                }
            }
        }

        private bool Deactivated(object message)
        {
            return base.ReceiveCommand(message) || message.Match()
                .With<AccountCommands.DeactivateAccount>(_ => { /* ignore */ })
                .WasHandled;
        }
    }
}