using System;
using Akka;
using Akka.Actor;
using Akkme.Shared.Domain.Accounts.TransferProtocol;
using Akkme.Shared.Domain.Events.V1;
using Akkme.Shared.Infrastructure.Domain;
using Akkme.Shared.Infrastructure.Serializers;
using Bond;

namespace Akkme.Shared.Domain.Accounts
{
    [Schema]
    public sealed class AccountModel : IBond
    {
        [Id(0), Required]
        public string AccountNr { get; private set; }

        [Id(1), Required]
        public decimal Balance { get; set; }

        public AccountModel(string accountNr, decimal initialBalance)
        {
            Balance = initialBalance;
            AccountNr = accountNr;
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

            // if account balance is sufficient, proceed with transfer by creating transfer saga actor responsible to transfering the money
            Command<Transfer>(transfer =>
            {
                var transactionId = Guid.NewGuid().ToString("N");
                var transferSaga = Context.ActorOf(Props.Create(() => new TransferSaga(transactionId)), "transaction-" + transactionId);
                transferSaga.Forward(transfer);
            },
            shouldHandle: transfer => transfer.Amount <= State.Balance);

            Command<Withdraw>(withdraw =>
            {
                var transferSaga = Sender;
                var transactionId = withdraw.TransactionId;
                Emit(new ModifiedBalance(transactionId, -withdraw.Amount), e =>
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

            Command<Deposit>(deposit =>
            {
                var transferSaga = Sender;
                var transactionId = deposit.TransactionId;
                Emit(new ModifiedBalance(transactionId, +deposit.Amount), e => transferSaga.Tell(new DepositSucceed(transactionId)));
            });
        }

        public override void UpdateState(IDomainEvent domainEvent)
        {
            domainEvent.Match()
                .With<ModifiedBalance>(modified => State.Balance += modified.Delta);
        }
    }
}
