using System;
using Akka.Actor;

namespace AkkaCQRS.Core.Accounting
{
    public class AccountCoordinator : AggregateCoordinator
    {
        public AccountCoordinator() : base("account")
        {
        }

        public override Props GetProps(Guid id)
        {
            return Props.Create(() => new Account(id));
        }

        protected override bool Receive(object message)
        {
            var handled = base.Receive(message);
            if (!handled)
            {
                if (message is AccountCommands.CreateAccount)
                {
                    var userId = Guid.NewGuid();
                    ForwardCommand(userId, message as AccountCommands.CreateAccount);
                }
                else if (message is GetState)
                {
                    var getState = message as GetState;
                    ForwardCommand(getState.Id, getState);
                }
                else if (message is IAddressed && message is IAccountCommand)
                {
                    var addressed = message as IAddressed;
                    ForwardCommand(addressed.RecipientId, message as IAccountCommand);
                }
                else return false;
                return true;
            }

            return true;
        }
    }
}