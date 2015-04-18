using System;
using Akka;
using Akka.Actor;

namespace AkkaCQRS.Core.Users
{
    public class UserCoordinator : AggregateCoordinator
    {
        public UserCoordinator() : base("user")
        {
        }

        public override Props GetProps(Guid id)
        {
            return Props.Create(() => new User(id));
        }

        protected override bool Receive(object message)
        {
            var handled = base.Receive(message);
            if (!handled)
            {
                if (message is UserCommands.RegisterUser)
                {
                    var userId = Guid.NewGuid();
                    ForwardCommand(userId, message as UserCommands.RegisterUser);
                }
                else if (message is IAddressed && message is IUserCommand)
                {
                    var addressed = message as IAddressed;
                    ForwardCommand(addressed.RecipientId, message as IUserCommand);
                }
                else return false;
                return true;
            }

            return true;
        }
    }
}