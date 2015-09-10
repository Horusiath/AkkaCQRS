using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;

namespace AkkaCQRS.Core
{

    /// <summary>
    /// Common class for all actors used to instantiate aggregate roots and manage access to them.
    /// </summary>
    public abstract class AggregateCoordinator : ActorBase
    {
        #region messages

        [Serializable]
        public sealed class PendingCommand
        {
            public readonly IActorRef Sender;
            public readonly Guid AggregateId;
            public readonly string PersistenceId;
            public ICommand Command;

            public PendingCommand(IActorRef sender, Guid aggregateId, string persistenceId, ICommand command)
            {
                Sender = sender;
                AggregateId = aggregateId;
                Command = command;
                PersistenceId = persistenceId;
            }
        }

        /// <summary>
        /// Message used in passivation mechanism. In this case each aggregate root is requesting coordinator
        /// to stop it after it didn't received any message for some time.  Before it do so, it sends a <see cref="Passivate"/> 
        /// message to coordinator. 
        /// 
        /// When aggregate coordinator receives passivation requests it starts buffering all messages incoming 
        /// to passivate requester. Then it sends a <see cref="PoisonPill"/> to an actor, stopping it in result.
        /// 
        /// In case when there are some buffered commands pending, coordinator recreates an aggregates and sends
        /// all pending messages to it.
        /// </summary>
        [Serializable]
        public sealed class Passivate
        {
            public static readonly Passivate Instance = new Passivate();

            private Passivate()
            {
            }
        }

        #endregion
        
        private ILoggingAdapter _log;
        protected ILoggingAdapter Log { get { return _log ?? (_log = Context.GetLogger()); } }

        protected readonly string ChildPrefix;

        // set of child actors, that send passivate message, but are not yet dead
        private readonly ISet<IActorRef> _passivating = new HashSet<IActorRef>();
        private ICollection<PendingCommand> _pendingCommands = new List<PendingCommand>(0);

        protected AggregateCoordinator(string childPrefix)
        {
            ChildPrefix = childPrefix;
        }

        /// <summary>
        /// By default aggregate coordinator watches over it's children. If any terminated
        /// message was received while some commands to terminated actor were pending, it 
        /// recreates an actor and resends messages to it.
        /// </summary>
        protected override bool Receive(object message)
        {
            var terminated = message as Terminated;
            if (terminated != null)
            {
                // if Terminated message was received, remove passivating actor from passivating children list
                _passivating.ExceptWith(new[] { terminated.ActorRef });

                // if there were pending commands waiting to be sent to terminated actor, recreate it
                var groups = _pendingCommands.GroupBy(cmd => cmd.PersistenceId == terminated.ActorRef.Path.Name).ToArray();
                _pendingCommands = groups.First(x => !x.Key).ToList();
                var commands = groups.First(x => x.Key);
                foreach (var pendingCommand in commands)
                {
                    var child = Recreate(pendingCommand.AggregateId, pendingCommand.PersistenceId);
                    child.Tell(pendingCommand.Command, pendingCommand.Sender);
                }

                return true;
            }
            else if (message is Passivate)
            {
                var child = Sender;
                _passivating.Add(child);
                child.Tell(PoisonPill.Instance);

                return true;
            }
            else return false;
        }

        /// <summary>
        /// Generates a persistence id for an entity with provided aggregate identifier.
        /// </summary>
        protected string GetPersistenceId(Guid id)
        {
            return ChildPrefix + "-" + id.ToString("N");
        }

        /// <summary>
        /// Forwards an addresses command message to recipient aggregate described in command.
        /// </summary>
        protected void ForwardCommand<TCommand>(TCommand command) where TCommand : IAddressed, ICommand
        {
            ForwardCommand(command.RecipientId, command);
        }

        /// <summary>
        /// Forwards a command to the child aggregate. Recreates an aggregate if it has not been incarnated yet.
        /// </summary>
        protected void ForwardCommand(Guid id, ICommand command)
        {
            var pid = GetPersistenceId(id);
            var child = Context.Child(pid);
            if (!child.Equals(ActorRefs.Nobody))
            {
                if (_passivating.Contains(child))
                {
                    // add command to cache
                    _pendingCommands.Add(new PendingCommand(Sender, id, pid, command));
                    return;
                }
            }
            else
            {
                child = Create(id, pid);
            }

            child.Forward(command);
        }

        /// <summary>
        /// Props to be used for child aggregates creation.
        /// </summary>
        public abstract Props GetProps(Guid id);

        /// <summary>
        /// Retrieves an actor with provided <paramref name="id"/>. Respawns actor if necessary.
        /// </summary>
        protected IActorRef Retrieve(Guid id)
        {
            return Recreate(id, GetPersistenceId(id));
        }

        /// <summary>
        /// Gets an child actor identified by <paramref name="pid"/> or creates it.
        /// </summary>
        private IActorRef Recreate(Guid id, string pid)
        {
            return Context.Child(pid) ?? Create(id, pid);
        }

        private IActorRef Create(Guid id, string pid)
        {
            var aggregateRef = Context.ActorOf(GetProps(id), pid);
            Context.Watch(aggregateRef);
            return aggregateRef;
        }
    }
}