using System;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using Akkme.Shared.Domain.Accounts.TransferProtocol;
using Akkme.Shared.Infrastructure.Domain;
using Akkme.Shared.Infrastructure.Serializers;
using Bond;
using V1 = Akkme.Shared.Domain.Events.V1;
using V2 = Akkme.Shared.Domain.Events.V2;

namespace Akkme.Shared.Domain.Accounts
{
    [Schema]
    public sealed class AccountModel : IBond
    {
        [Id(0), Required] public string AccountNr { get; private set; }

        [Id(1), Required] public decimal Balance { get; set; }

        [Id(2), Required] public ICollection<string> PendingTransactions { get; set; }

        public AccountModel(string accountNr, decimal initialBalance)
        {
            Balance = initialBalance;
            AccountNr = accountNr;
            PendingTransactions = new HashSet<string>();
        }
    }

    public class Account : AggregateRoot<AccountModel>
    {
        public Account()
        {
            State = new AccountModel(PersistenceId, 0M);

            // try fail fast if account has not sufficient funds to realize transfer
            Command<Transfer>(transfer =>
            {
                Sender.Tell(new TransferFailed(transfer.TransactionId,
                    new Exception($"Insufficient funds. Transfer requested {transfer.Amount}, while current account balance is {State.Balance}")));
            },
            shouldHandle: transfer => transfer.Amount > State.Balance);

            // if account balance is sufficient, proceed with transfer by creating transfer saga actor responsible for transfering the money
            Command<Transfer>(transfer =>
            {
                var transactionId = Guid.NewGuid().ToString("N");
                Emit(new V1.TransferCreated(transactionId), e =>
                {
                    var transferSaga = CreateTransfer(e.TransferId);
                    transferSaga.Forward(transfer);
                });
            },
            shouldHandle: transfer => transfer.Amount <= State.Balance);

            Command<Withdraw>(withdraw =>
            {
                var transferSaga = Sender;
                var transactionId = withdraw.TransactionId;
                Emit(new V2.ModifiedBalance(transactionId, -withdraw.Amount, DateTime.UtcNow), e =>
                {
                    transferSaga.Tell(new WithdrawSucceed(transactionId));
                    if (State.Balance < 0)
                    {
                        // in case when there are a lot of long-running, concurrent transactions, 
                        // there is a risk of reaching negative balance, in that case business 
                        // must think about making a compensating action
                        Log.Warning("Transaction succeed, but you've reached negative balance {0}. A compensating action will be necessary.", State.Balance);
                    }
                });
            });

            Command<TransferFailed>(failed => Emit(new V1.TransferFinished(failed.TransactionId, failed.Reason)));
            Command<TransferSucceed>(succeed => Emit(new V1.TransferFinished(succeed.TransactionId)));

            Command<Deposit>(deposit =>
            {
                var transferSaga = Sender;
                var transactionId = deposit.TransactionId;
                Emit(new V2.ModifiedBalance(transactionId, +deposit.Amount, DateTime.UtcNow), e => transferSaga.Tell(new DepositSucceed(transactionId)));
            });
        }

        private IActorRef CreateTransfer(string transactionId)
        {
            return Context.ActorOf(Props.Create(() => new TransferSaga(transactionId, Plugin.Accounts)), transactionId);
        }

        public override void UpdateState(IDomainEvent domainEvent)
        {
            domainEvent.Match()
                .With<V2.ModifiedBalance>(modified => State.Balance += modified.Delta)
                .With<V1.TransferCreated>(created => State.PendingTransactions.Add(created.TransferId))
                .With<V1.TransferFinished>(created => State.PendingTransactions.Remove(created.TransferId));
        }

        protected override void OnReplaySuccess()
        {
            base.OnReplaySuccess();

            // after recovery phase has finished, we want to recreate all pending transfers in order to complete them
            foreach (var pendingTransaction in State.PendingTransactions)
            {
                CreateTransfer(pendingTransaction);
            }
        }
    }
}
