using System;
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

    public class Account : AggregateRoot<AccountEntity>
    {
        /// <summary>
        /// Custom notion of date time provider, easy to replace.
        /// </summary>
        public static readonly Clock Clock = () => DateTime.UtcNow;

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
                .With<AccountCommands.DeactivateAccount>(deactivate =>
                {
                    Persist(new AccountEvents.AccountDeactivated(deactivate.AccountId, Clock()));
                })
                .With<AccountCommands.Deposit>(deposit =>
                {
                    if (deposit.Amount > 0)
                    {
                        Persist(new AccountEvents.Deposited(_id, deposit.Amount, Clock()));
                    }
                    else
                    {
                        Log.Error("Cannot perform deposit on account {0}: money amount is not positive value", _id);
                    }
                })
                .With<AccountCommands.Withdraw>(withdraw =>
                {
                    var sender = Sender;
                    var withdrawal = new AccountEvents.Withdrawal(_id, withdraw.Amount, Clock());

                    // Use defer to await to proceed command until all account events have been
                    // persisted and handled. This is done mostly, because we don't want to perform
                    // negative account check while there may be still account balance modifying events
                    // waiting in mailbox.
                    Defer(withdrawal, e =>
                    {
                        if (withdraw.Amount > 0 && withdraw.Amount <= State.Balance)
                        {
                            Persist(e, sender);
                        }
                        else
                        {
                            Log.Error("Cannot perform withdrawal from account {0}, because it has a negative balance", _id);
                            sender.Tell(new NotEnoughtFunds(_id));
                        }
                    });

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