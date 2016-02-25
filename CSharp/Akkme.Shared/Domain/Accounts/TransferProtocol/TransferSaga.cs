using System;
using Akka.Actor;
using Akka.Persistence;
using Akkme.Shared.Domain.Events.V1;
using Akkme.Shared.Infrastructure.Domain;
using Akkme.Shared.Infrastructure.Utils;
using Bond;

namespace Akkme.Shared.Domain.Accounts.TransferProtocol
{
    /// <summary>
    /// Transfer saga is an actor dedicated to manage the funds transfer process between two actors. 
    /// It's always created by actor initializing transfer process (which makes it <see cref="IActorContext.Parent"/> in this case).
    /// 
    /// Since transfer and one of it's parties (money sender) lies on the same machine, communication safety on this direction 
    /// is always guaranteed. To make it safe on the second end, we're using at-least-once delivery semantics 
    /// combined with idempotency of <see cref="Account"/> aggregate.
    /// </summary>
    public sealed class TransferSaga : ReceivePersistentActor
    {
        private decimal amount;
        private string toAccount;

        // used for trackings steps to make on rollback compensation
        private ITransferEvent lastEvent;
        private IActorRef replyTo;
        private readonly IActorRef accountShardRegion;

        public TransferSaga(string transactionId, IActorRef accountShardRegion)
        {
            this.accountShardRegion = accountShardRegion;
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentNullException(nameof(transactionId), $"{GetType()} requires {nameof(transactionId)} to be provided");

            PersistenceId = transactionId;

            // we give each operation up to one minute to finish
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(60));

            Recover((Action<TransferStarted>)OnTransferStarted);
            Recover((Action<Withdrawn>)OnMoneyWithdrawn);
            Recover((Action<Deposited>)OnMoneyDeposited);
            Recover((Action<Rollback>)OnRollback);

            Waiting();
        }

        public override string PersistenceId { get; }

        /// <summary>
        /// Method used for validation if passed transfer protocol message is correlated with current saga. Just in case.
        /// </summary>
        private bool ConcernsCurrentTransfer<TMessage>(TMessage message)
            where TMessage : ITransactional => message.TransactionId == PersistenceId;

        #region Actor behaviors

        /// <summary>
        /// Initial state, reacts only on <see cref="Transfer"/> meesage.
        /// </summary>
        private void Waiting()
        {
            Command<Transfer>(ConcernsCurrentTransfer, start =>
            {
                replyTo = Sender;
                // as mentioned since TransferSaga is created by an Account sending a money,
                // fromId allways is based on the parent
                var fromId = replyTo.AggregateId();
                var toId = start.ToAccountNr;
                Persist(new TransferStarted(start.Amount, fromId, toId), OnTransferStarted);
            });
        }

        /// <summary>
        /// Once <see cref="Withdraw"/> request has been sent to parent, current <see cref="TransferSaga"/> 
        /// awaits for confirmation, then sends <see cref="Deposit"/> request to transfer recipient. 
        /// </summary>
        private void AwaitWithdrawn()
        {
            Command<WithdrawSucceed>(ConcernsCurrentTransfer, success => Persist(new Withdrawn(PersistenceId, amount), OnMoneyWithdrawn));
            Command<WithdrawFailed>(ConcernsCurrentTransfer, failed => Persist(Rollback.Instance, OnRollback));
            Command<ReceiveTimeout>(failed => Persist(Rollback.Instance, OnRollback));
        }

        /// <summary>
        /// Once <see cref="Deposit"/> request has been sent to receiver, current <see cref="TransferSaga"/>
        /// needs only to await for confirmation to finish it self, or failure to start rollback.
        /// </summary>
        private void AwaitDeposited()
        {
            Command<DepositSucceed>(ConcernsCurrentTransfer, success => Persist(new Deposited(PersistenceId, amount), OnMoneyDeposited));
            Command<DepositFailed>(ConcernsCurrentTransfer, failed => Persist(Rollback.Instance, OnRollback));
            Command<ReceiveTimeout>(failed => Persist(Rollback.Instance, OnRollback));
        }

        #endregion

        #region Domain event handlers

        private void OnMoneyDeposited(Deposited e)
        {
            replyTo.Tell(new TransferSucceed(PersistenceId));
            Context.Stop(Self);
        }

        private void OnMoneyWithdrawn(Withdrawn e)
        {
            lastEvent = e;
            accountShardRegion.Tell(new ShardEnvelope<Deposit>(toAccount, new Deposit(PersistenceId, amount)));
            Become(AwaitDeposited);
        }

        private void OnTransferStarted(TransferStarted e)
        {
            lastEvent = e;
            amount = e.Amount;
            toAccount = e.ToAccountNr;
            replyTo.Tell(new Withdraw(PersistenceId, amount));
            Become(AwaitWithdrawn);
        }

        private void OnRollback(Rollback _)
        {
            if (lastEvent is TransferStarted)
            {
                // compensating action - we ordered withdraw already, now order a deposit back withdrawn amount
                replyTo.Tell(new Deposit(PersistenceId, amount));
            }
            if (lastEvent is Withdrawn)
            {
                // compensating action - we ordered withdraw and deposit already, 
                // now we need to rollback both of these operations
                accountShardRegion.Tell(new ShardEnvelope<Withdraw>(toAccount, new Withdraw(PersistenceId, amount)));
                replyTo.Tell(new Deposit(PersistenceId, amount));
            }

            replyTo.Tell(new TransferFailed(PersistenceId, new Exception("Couldn't finish transfer transaction, rollback has been called")));
            Context.Stop(Self);
        }

        #endregion
    }
}
