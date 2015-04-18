using System;
using Akka;
using Akka.Actor;

namespace AkkaCQRS.Core.Accounting
{

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

    public class Account : AggregateRoot<AccountEntity>
    {
        private readonly Guid _id;

        public Account(Guid id)
            : base("account-" + id.ToString("N"))
        {
            _id = id;
            Context.Become(Uninitialized);
        }


        protected override bool OnCommand(object message)
        {
            return false;
        }

        protected override void UpdateState(IEvent domainEvent, IActorRef sender)
        {
            domainEvent.Match()
                .With<AccountEvents.Transfered>(e =>
                {
                    //TODO:
                })
                .With<AccountEvents.Withdrawal>(e =>
                {
                    State.Balance -= e.Amount;
                })
                .With<AccountEvents.Deposited>(e =>
                {
                    State.Balance += e.Amount;
                })
                .With<AccountEvents.AccountCreated>(e =>
                {
                    State = new AccountEntity(e.Id, e.OwnerId, true, e.Balance);
                    Context.Become(Active);

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
                    Persist(new AccountEvents.AccountCreated(_id, create.OwnerId, create.Balance));
                })
                .WasHandled;
        }

        private bool Active(object message)
        {
            return base.ReceiveCommand(message) || message.Match()
                .With<AccountCommands.DeactivateAccount>(deactivate =>
                {
                    Persist(new AccountEvents.AccountDeactivated(deactivate.AccountId));
                })
                .With<AccountCommands.Deposit>(deposit =>
                {
                    if (deposit.Amount > 0)
                    {
                        Persist(new AccountEvents.Deposited(_id, deposit.Amount));
                    }
                    else
                    {
                        //TODO: ammount must be a positive number
                    }
                })
                .With<AccountCommands.Withdraw>(withdraw =>
                {
                    if (withdraw.Amount > 0 && withdraw.Amount <= State.Balance)
                    {
                        Persist(new AccountEvents.Withdrawal(_id, withdraw.Amount));
                    }
                    else
                    {
                        //TODO: ammount must be a positive number and les than actual balance
                    }
                })
                .With<AccountCommands.Transfer>(transfer =>
                {
                    //TODO: transfer
                })
                .WasHandled;
        }

        private bool Deactivated(object message)
        {
            return base.ReceiveCommand(message) || message.Match()
                .With<AccountCommands.DeactivateAccount>(_ => { /* ignore */ })
                .WasHandled;
        }
    }
}