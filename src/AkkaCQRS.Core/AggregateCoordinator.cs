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

        protected sealed class PendingCommand
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

        #endregion

        /// <summary>
        /// Default value of maximum number of child aggregates to be kept in memory at once.
        /// </summary>
        public const int DefaultMaxChildrenCount = 64;

        /// <summary>
        /// Maximum number of child aggregates to be kept in memory at once.
        /// </summary>
        public virtual int MaxChildrenCount { get { return DefaultMaxChildrenCount; } }

        /// <summary>
        ///  Default number of children to be killed at once on kill request.
        /// </summary>
        public const int DefaultChildrenToKillCount = 32;

        /// <summary>
        /// Number of children to be killed at once on kill request.
        /// </summary>
        public virtual int ChildrenToKillCount { get { return DefaultChildrenToKillCount; } }

        private ILoggingAdapter _log;
        protected ILoggingAdapter Log { get { return _log ?? (_log = Context.GetLogger()); } }

        protected readonly string ChildPrefix;
        private readonly ISet<IActorRef> _terminatingChildren = new HashSet<IActorRef>();
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
                // if Terminated message was received, remove terminated actor from terminating children list
                _terminatingChildren.ExceptWith(new[] { terminated.ActorRef });

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
            else return false;
        }

        /// <summary>
        /// Generates a persistence id for an entity with provided aggregate identifier.
        /// </summary>
        protected string GetPersistenceId(Guid id)
        {
            return ChildPrefix + id.ToString("N");
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
            if (child != null)
            {
                if (_terminatingChildren.Contains(child))
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
        /// Gets an child actor identified by <paramref name="pid"/> or creates it.
        /// </summary>
        private IActorRef Recreate(Guid id, string pid)
        {
            return Context.Child(pid) ?? Create(id, pid);
        }

        private IActorRef Create(Guid id, string pid)
        {
            HarvestChildren();

            var aggregateRef = Context.ActorOf(GetProps(id), pid);
            Context.Watch(aggregateRef);
            return aggregateRef;
        }

        /// <summary>
        /// Checks if children count doesn't expanded over specified boundaries
        /// and removes overflowing ones from the buffer.
        /// </summary>
        private void HarvestChildren()
        {
            var children = Context.GetChildren().ToArray();
            if (children.Length - _terminatingChildren.Count >= MaxChildrenCount)
            {
                Log.Debug("Max children count exceeded. Killing {0} children", ChildrenToKillCount);

                // get all non-terminating children, kill the [ChildrenToKillCount] of them
                // and put onto terminating children list
                var notTerminating = children.Except(_terminatingChildren);
                var childrenToKill = notTerminating.Take(ChildrenToKillCount);
                foreach (var childRef in childrenToKill)
                {
                    childRef.Tell(Kill.Instance);
                    _terminatingChildren.Add(childRef);
                }
            }
        }
    }
}